using hitbtc_lib.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using trade_model;
using cache_lib.Models;
using System.IO;
using web_util;
using mongo_lib;
using trade_node_integration;
using log_lib;
using trade_res;
using HtmlAgilityPack;
using System.Web;
using hitbtc_lib.res;
using cache_lib;
using MongoDB.Bson;
using trade_lib;
using commodity_map;
using config_client_lib;
using browser_automation_client_lib;
using hitbtc_lib.Client;
using object_extensions_lib;
using hitbtc_lib.Client.ClientModels;

namespace hitbtc_lib
{
    public class HitBtcIntegration :
        OrderBookIntegration,
        IHitBtcIntegration
    {
        private const string CcxtIntegrationName = "hitbtc";
        private const string DatabaseName = "hitbtc";

        private static readonly TimeSpan MarketThreshold = TimeSpan.FromMinutes(20);
        private static readonly TimeSpan OrderBookThreshold = TimeSpan.FromMinutes(20);
        private static readonly TimeSpan MarketCacheThreshold = TimeSpan.FromMinutes(17.5);
        private static readonly TimeSpan ThrottleThresh = TimeSpan.FromSeconds(0.75);
        private static readonly TimeSpan SymbolThreshold = TimeSpan.FromMinutes(20);
        private static readonly TimeSpan OpenOrdersThreshold = TimeSpan.FromMinutes(2);
        private static readonly TimeSpan HistoryThreshold = TimeSpan.FromMinutes(20);

        private static readonly ThrottleContext _throttleContext = new ThrottleContext
        {
            Locker = new object(),
            ThrottleThreshold = ThrottleThresh
        };

        private static Random _random = new Random();
        private readonly IWebUtil _webUtil;
        private readonly CacheUtil _cacheUtil;
        private readonly ITradeNodeUtil _tradeNodeUtil;
        private readonly ILogRepo _log;
        private readonly IConfigClient _configClient;
        private readonly IBrowserAutomationClient _browserAutomationClient;
        private readonly IHitBtcClient _hitBtcClient;

        private readonly HitBtcMap _hitBtcMap = new HitBtcMap();

        protected override IMongoDatabaseContext DbContext
        {
            get { return new MongoDatabaseContext(_configClient.GetConnectionString(), DatabaseName); }
        }

        public HitBtcIntegration(
            IWebUtil webUtil,
            IConfigClient configClient,
            IHitBtcClient hitBtcClient,
            ITradeNodeUtil tradeNodeUtil,
            IBrowserAutomationClient browserAutomationClient,
            ILogRepo logRepo)
        {
            _webUtil = webUtil;
            _configClient = configClient;
            _hitBtcClient = hitBtcClient;       
            _tradeNodeUtil = tradeNodeUtil;
            _browserAutomationClient = browserAutomationClient;
            _log = logRepo;

            _cacheUtil = new CacheUtil();
        }

        public HoldingInfo GetHoldingsOld(CachePolicy cachePolicy)
        {
            var native = GetNativeHoldings(cachePolicy);

            var holdingInfo = new HoldingInfo
            {
                TimeStampUtc = DateTime.UtcNow,
                Holdings = native.Select(item => new Holding
                {
                    Symbol = item.Currency,
                    Available = item.Available,
                    InOrders = item.Reserved,
                    Total = item.Available + item.Reserved
                }).ToList()
            };

            return holdingInfo;
        }

        public HoldingInfo GetHoldings(CachePolicy cachePolicy)
        {
            var native = GetCcxtHoldings(cachePolicy);

            var holdingInfo = new HoldingInfo
            {
                TimeStampUtc = DateTime.UtcNow,
                Holdings = new List<Holding>()
            };

            foreach (var item in native.TradingAccount)
            {
                var existingHolding = holdingInfo.Holdings.SingleOrDefault(queryHolding =>
                    string.Equals(queryHolding.Symbol, item.Symbol, StringComparison.InvariantCultureIgnoreCase));

                if (existingHolding != null)
                {
                    existingHolding.Available = item.Free ?? 0;
                    existingHolding.InOrders = item.Used ?? 0;
                    existingHolding.Total += item.Total ?? 0;
                    continue;
                }

                holdingInfo.Holdings.Add(new Holding
                {
                    Symbol = item.Symbol,
                    // AccountType = "Trading",
                    Available = item.Free ?? 0,
                    InOrders = item.Used ?? 0,
                    Total = item.Total ?? 0
                });
            }

            foreach (var item in native.MainAccount)
            {
                var existingHolding = holdingInfo.Holdings.SingleOrDefault(queryHolding =>
                    string.Equals(queryHolding.Symbol, item.Symbol, StringComparison.InvariantCultureIgnoreCase));

                if (existingHolding != null)
                {
                    existingHolding.Total += item.Total ?? 0;
                    if (item.Total.HasValue && item.Total > 0)
                    {
                        existingHolding.AdditionalHoldings = existingHolding.AdditionalHoldings ?? new Dictionary<string, decimal>();
                        existingHolding.AdditionalHoldings["Main"] = item.Total.Value;
                    }

                    continue;
                }

                holdingInfo.Holdings.Add(new Holding
                {
                    Symbol = item.Symbol,
                    // AccountType = "Main",
                    Total = item.Total ?? 0
                });
            }

            //    Holdings = native.Select(item => new Holding
            //    {
            //        Asset = item.Currency,
            //        Available = item.Available,
            //        InOrders = item.Reserved,
            //        Total = item.Available + item.Reserved
            //    }).ToList()
            //};

            return holdingInfo;
        }

        public HitBtcCcxtAggregateBalance GetCcxtHoldings(CachePolicy cachePolicy)
        {
            // var results = _tradeNodeUtil.FetchBalance(CcxtIntegrationName);
            // return results;

            var retriever = new Func<string>(() => _tradeNodeUtil.FetchBalance(CcxtIntegrationName));
            var translator = new Func<string, HitBtcCcxtAggregateBalance>(text =>
            {
                if (string.IsNullOrWhiteSpace(text)) { return null; }
                var outerDictionary = JsonConvert.DeserializeObject<Dictionary<string, object>>(text);
                if (outerDictionary == null) { return null; }

                var tradingAccountBalanceItems = new List<HitBtcCcxtBalanceItem>();
                var mainAccountBalanceItems = new List<HitBtcCcxtBalanceItem>();
                if (outerDictionary.ContainsKey("trading"))
                {
                    var tradingContents = outerDictionary["trading"].ToString();
                    if (!string.IsNullOrWhiteSpace(tradingContents))
                    {
                        var tradingDictionary = JsonConvert.DeserializeObject<Dictionary<string, object>>(tradingContents);
                        if (tradingDictionary != null)
                        {
                            foreach (var key in tradingDictionary.Keys)
                            {
                                if (string.Equals(key, "info", StringComparison.InvariantCultureIgnoreCase)) { continue; }
                                var value = tradingDictionary[key];
                                if (value == null) { continue; }
                                var valueText = value.ToString();
                                var balanceItem = JsonConvert.DeserializeObject<HitBtcCcxtBalanceItem>(valueText);
                                if (balanceItem.Free == 0 && balanceItem.Used == 0 && balanceItem.Total == 0) { continue; }

                                balanceItem.Symbol = key;

                                tradingAccountBalanceItems.Add(balanceItem);
                            }
                        }
                    }
                }

                if (outerDictionary.ContainsKey("account"))
                {
                    var accountContents = outerDictionary["account"].ToString();
                    if (!string.IsNullOrWhiteSpace(accountContents))
                    {
                        var accountDictionary = JsonConvert.DeserializeObject<Dictionary<string, object>>(accountContents);
                        if (accountDictionary != null)
                        {
                            foreach (var key in accountDictionary.Keys)
                            {
                                if (string.Equals(key, "info", StringComparison.InvariantCultureIgnoreCase)) { continue; }
                                var value = accountDictionary[key];
                                if (value == null) { continue; }
                                var valueText = value.ToString();
                                var balanceItem = JsonConvert.DeserializeObject<HitBtcCcxtBalanceItem>(valueText);
                                if (balanceItem.Free == 0 && balanceItem.Used == 0 && balanceItem.Total == 0) { continue; }

                                balanceItem.Symbol = key;

                                mainAccountBalanceItems.Add(balanceItem);
                            }
                        }
                    }
                }

                return new HitBtcCcxtAggregateBalance
                {
                    TradingAccount = tradingAccountBalanceItems,
                    MainAccount = mainAccountBalanceItems
                };
            });

            var validator = new Func<string, bool>(text =>
            {
                if (string.IsNullOrWhiteSpace(text)) { return false; }
                try
                {
                    var translated = translator(text);
                    return translated != null;
                }
                catch
                {
                    return false;
                }
            });

            var context = new MongoCollectionContext(DbContext, "hitbtc--get-ccxt-aggregate-balance");

            var threshold = TimeSpan.FromMinutes(5);
            var cacheResult = _cacheUtil.GetCacheableEx(ThrottleContext, retriever, context, threshold, cachePolicy, validator);
            return translator(cacheResult?.Contents);
        }

        public List<HitBtcBalanceItem> GetNativeHoldings(CachePolicy cachePolicy)
        {
            var retriever = new Func<string>(() =>
            {
                var apiKey = _configClient.GetHitBtcApiKey();
                return _hitBtcClient.AuthenticatedRequest(apiKey, "https://api.hitbtc.com/api/2/trading/balance");
            });

            var translator = new Func<string, List<HitBtcBalanceItem>>(text => 
                !string.IsNullOrWhiteSpace(text)
                ? JsonConvert.DeserializeObject<List<HitBtcBalanceItem>>(text)
                : null);

            var validator = new Func<string, bool>(text =>
            {
                if (string.IsNullOrWhiteSpace(text)) { return false; }
                try
                {
                    var translated = translator(text);
                    return translated != null;
                }
                catch
                {
                    return false;
                }
            });

            var context = new MongoCollectionContext(DbContext, "hitbtc--get-trading-balance");

            var threshold = TimeSpan.FromMinutes(5);
            var cacheResult = _cacheUtil.GetCacheableEx(ThrottleContext, retriever, context, threshold, cachePolicy, validator);
            return translator(cacheResult?.Contents);
        }

        private static readonly object Locker = new object();
        private static DateTime? LastThrottleTime;        

        public List<CommodityForExchange> GetCommodities(CachePolicy cachePolicy)
        {
            var native = GetNativeCurrencies(cachePolicy);
            // var health = GetHealth(cachePolicy);

            var healthCachePolicy = cachePolicy == CachePolicy.AllowCache ? CachePolicy.OnlyUseCacheUnlessEmpty : cachePolicy;
            var health = GetHealth(healthCachePolicy);

            var healthDictionary = new Dictionary<string, HitBtcHealthStatusItem>();
            foreach(var healthItem in health)
            {
                if(string.IsNullOrWhiteSpace(healthItem.Symbol)) { continue; }
                healthDictionary[healthItem.Symbol.Trim().ToUpper()] = healthItem;
            }

            return native.Select(nativeCurrency =>
            {
                var nativeSymbol = nativeCurrency.Id;

                var canon = _hitBtcMap.GetCanon(nativeSymbol);

                var commodityForExchange = new CommodityForExchange
                {
                    CanonicalId = canon?.Id,
                    Symbol = !string.IsNullOrWhiteSpace(canon?.Symbol) ? canon.Symbol : nativeSymbol,
                    NativeSymbol = nativeSymbol,
                    Name = !string.IsNullOrWhiteSpace(canon?.Name) ? canon.Name : nativeCurrency.Fullname,
                    NativeName = nativeCurrency.Fullname,
                    CanDeposit = nativeCurrency.PayinEnabled,
                    CanWithdraw = nativeCurrency.PayoutEnabled,
                    WithdrawalFee = nativeCurrency.PayoutFee,
                    CustomValues = new Dictionary<string, string>()
                };

                var healthItem = healthDictionary.ContainsKey(nativeSymbol.ToUpper()) ? healthDictionary[nativeSymbol.ToUpper()] : null;

                if (healthItem != null)
                {
                    // When trading acount <- -> main account transfers are disabled in either direction,
                    // We're picking up the text "To trading"
                    // We really should be picking up two rows "To trading" and "To main"
                    //   with one of the red and one of them green.
                    // For now, if we pick up the text "To trading",
                    // and CanDeposit is enabled, then mark CanDeposit as unknown.
                    // If CanWithdraw is enabled, then mark CanWithdraw as unknown.
                    if (string.Equals(healthItem.TransfersStatusText, "To trading", StringComparison.InvariantCultureIgnoreCase))
                    {
                        if (commodityForExchange.CanDeposit.HasValue && commodityForExchange.CanDeposit.Value)
                        {
                            commodityForExchange.CanDeposit = null;
                        }

                        if (commodityForExchange.CanWithdraw.HasValue && commodityForExchange.CanWithdraw.Value)
                        {
                            commodityForExchange.CanWithdraw = null;
                        }
                    }

                    // When transfers are offline, it's *sort* of still possible to withdraw and deposit.
                    // However, when the user can't transfer between the main account and the trading account,
                    // There's now way to deposit and then trade the funds
                    // and there's no way to move the funds from the trading account to another exchange.
                    // 
                    // For now, mark desposits and withdrawals as disabled.
                    if (string.Equals(healthItem.TransfersStatusText, "Offline", StringComparison.InvariantCultureIgnoreCase))
                    {
                        commodityForExchange.CanDeposit = false;
                        commodityForExchange.CanWithdraw = false;
                    }



                    if (!string.IsNullOrWhiteSpace(healthItem.DepositStatusText) && healthItem.DepositStatusText.ToUpper().Contains("Offline".ToUpper()))
                    {
                        commodityForExchange.CanDeposit = false;
                    }

                    if (!string.IsNullOrWhiteSpace(healthItem.WithdrawalStatusText) && healthItem.WithdrawalStatusText.ToUpper().Contains("Offline".ToUpper()))
                    {
                        commodityForExchange.CanWithdraw = false;
                    }

                    if (!string.IsNullOrWhiteSpace(healthItem.ProcessingTimeHighText))
                    {
                        commodityForExchange.CustomValues["Processing Time High"] = healthItem.ProcessingTimeHighText;
                    }

                    if (!string.IsNullOrWhiteSpace(healthItem.ProcessingTimeAverageText))
                    {
                        commodityForExchange.CustomValues["Processing Time Average"] = healthItem.ProcessingTimeAverageText;
                    }

                    if (!string.IsNullOrWhiteSpace(healthItem.ProcessingTimeLowText))
                    {
                        commodityForExchange.CustomValues["Processing Time Low"] = healthItem.ProcessingTimeLowText;
                    }

                    if (!string.IsNullOrWhiteSpace(healthItem.PendingDepositsText))
                    {
                        commodityForExchange.CustomValues["Pending Deposits"] = healthItem.PendingDepositsText;
                    }

                    if (!string.IsNullOrWhiteSpace(healthItem.PendingWithdrawalsText))
                    {
                        commodityForExchange.CustomValues["Pending Withdrawals"] = healthItem.PendingWithdrawalsText;
                    }
                }

                return commodityForExchange;
            }).ToList();
        }
        
        private CacheResult GetMarketInfoCache(TradingPair tradingPair, CachePolicy cachePolicy)
        {
            var tradingPairs = GetTradingPairs(cachePolicy);
            if (!tradingPairs.Any(tp => tp.Equals(tradingPair)))
            {
                throw new ApplicationException($"HitBTC does not support order books for trading pair {tradingPair}.");
            }

            if (tradingPair == null) { throw new ArgumentNullException(nameof(tradingPair)); }
            if (string.IsNullOrWhiteSpace(tradingPair.Symbol)) { throw new ArgumentNullException($"{tradingPair}.{nameof(tradingPair.Symbol)}"); }
            if (string.IsNullOrWhiteSpace(tradingPair.BaseSymbol)) { throw new ArgumentNullException($"{tradingPair}.{nameof(tradingPair.BaseSymbol)}"); }

            // https://api.hitbtc.com/#rest-api-reference
            // https://api.hitbtc.com/api/2/public/orderbook/ETHBTC
            var effectiveSymbol = tradingPair.Symbol.Trim().ToUpper();
            var effectiveBaseSymbol = tradingPair.BaseSymbol.Trim().ToUpper();
            var url = $"https://api.hitbtc.com/api/2/public/orderbook/{effectiveSymbol}{effectiveBaseSymbol}";

            var retriever = new Func<string>(() => _webUtil.Get(url));
            var validator = new Func<string, bool>(text => !string.IsNullOrWhiteSpace(text));
            var collectionContext = new MongoCollectionContext(DbContext, $"hitbtc-orderebook-{effectiveSymbol}-{effectiveBaseSymbol}");

            return _cacheUtil.GetCacheableEx(ThrottleContext, retriever, collectionContext, MarketThreshold, cachePolicy, validator);
        }

        public RefreshCacheResult RefreshOrderBook(TradingPair tradingPair)
        {
            // Refresh before it expires. Throw in some randomness to ensure that everything doesn't need to be refreshed at the same time.
            var threshold = _random.Next() % 2 == 0 ? MarketCacheThreshold : MarketThreshold;

            var cacheResult = GetMarketInfoCache(tradingPair, CachePolicy.AllowCache);
            return new RefreshCacheResult
            {
                AsOf = cacheResult.AsOf,
                CacheAge = cacheResult.CacheAge,
                WasRefreshed = !cacheResult.WasFromCache
            };
        }

        private List<HitBtcCcxtMarket> GetNativeMarkets(CachePolicy cachePolicy)
        {
            var validator = new Func<string, bool>(text => !string.IsNullOrWhiteSpace(text));

            var retriever = new Func<string>(() =>
            {
                try
                {
                    var result = _tradeNodeUtil.FetchMarkets(CcxtIntegrationName);
                    if (!validator(result))
                    {
                        _log.Error("Validation failed when attempting to get HitBtc native markets.");
                    }

                    return result;
                }
                catch (Exception exception)
                {                    
                    _log.Error(exception);
                    throw;
                }
            });
            
            var translator = new Func<string, List<HitBtcCcxtMarket>>(text => JsonConvert.DeserializeObject<List<HitBtcCcxtMarket>>(text));
            var context = new MongoCollectionContext(DbContext, "hitbtc--fetch-markets");
            var threshold = TimeSpan.FromHours(2);

            var cacheResult = _cacheUtil.GetCacheableEx(ThrottleContext, retriever, context, threshold, cachePolicy, validator);
            return translator(cacheResult.Contents);
        }

        private AsOfWrapper<List<HitBtcSymbol>> GetNativeSymbols(CachePolicy cachePolicy)
        {
            var translator = new Func<string, List<HitBtcSymbol>>(text =>
            {
                var nativeSymbols = !string.IsNullOrWhiteSpace(text)
                    ? JsonConvert.DeserializeObject<List<HitBtcSymbol>>(text)
                    : null;

                return nativeSymbols;
            });

            var validator = new Func<string, bool>(text =>
            {
                if (string.IsNullOrWhiteSpace(text))
                {
                    throw new ApplicationException($"Received a null or whitespace response from {Name} when requesting symbols.");
                }

                translator(text);

                return true;
            });

            var retriever = new Func<string>(() =>
            {
                try
                {
                    var text = _hitBtcClient.GetSymbols();
                    if (!validator(text))
                    {
                        throw new ApplicationException("Get HitBtc symbols response failed validation.");
                    }

                    return text;
                }
                catch (Exception exception)
                {
                    _log.Error($"An exception was thrown when attempting to retrieve symbols from HitBtc.{Environment.NewLine}{exception.Message}");
                    _log.Error(exception);
                    throw;
                }
            });

            var collectionContext = new MongoCollectionContext(DbContext, "hitbtc--get-symbols");

            var cacheResult = _cacheUtil.GetCacheableEx(ThrottleContext, retriever, collectionContext, SymbolThreshold, cachePolicy, validator);

            var translated = translator(cacheResult?.Contents);

            return new AsOfWrapper<List<HitBtcSymbol>>
            {
                Data = translated,
                AsOfUtc = cacheResult.AsOf
            };
        }

        public List<TradingPair> GetTradingPairs(CachePolicy cachePolicy)
        {
            return GetTradingPairsWithAsOf(cachePolicy)?.TradingPairs;
        }
   
        public Dictionary<string, decimal> GetWithdrawalFees(CachePolicy cachePolicy)
        {
            var native = GetNativeCurrencies(cachePolicy);
            var dict = new Dictionary<string, decimal>();

            foreach (var item in native)
            {
                dict[item.Id.ToUpper()] = item.PayoutFee;
            }

            return dict;
        }

        private List<HitBtcCurrency> GetNativeCurrencies(CachePolicy cachePolicy)
        {
            var translator = new Func<string, List<HitBtcCurrency>>(text =>
            {
                if (string.IsNullOrWhiteSpace(text)) { return null; }
                var nativeCurrencies = JsonConvert.DeserializeObject<List<HitBtcCurrency>>(text);

                return nativeCurrencies;
            });

            var validator = new Func<string, bool>(text => !string.IsNullOrWhiteSpace(text));
            var retriever = new Func<string>(() =>
            {
                return _hitBtcClient.GetCurrenciesRaw();
            });

            var context = new MongoCollectionContext(DbContext, "hitbtc--native-currencies");
            var threshold = TimeSpan.FromHours(2);
            var cacheResult = _cacheUtil.GetCacheableEx(ThrottleContext, retriever, context, threshold, cachePolicy, validator);

            return translator(cacheResult.Contents);
        }

        public decimal? GetWithdrawalFee(string symbol, CachePolicy cachePolicy)
        {
            var fees = GetWithdrawalFees(cachePolicy);
            return fees.ContainsKey(symbol) ? fees[symbol] : (decimal?)null;
        }

        private const string HitBtcDepositAddressesFileName = @"C:\trade\config\hitbtc-deposit-addresses.json";

        public string Name => "HitBTC";
        public Guid Id => new Guid("F2E6F7D9-A47E-4BC8-A124-C28CA10B997D");

        protected override ILogRepo Log => _log;

        protected override string CollectionPrefix => "hitbtc";

        protected override CommodityMap Map => _hitBtcMap;

        protected override ThrottleContext ThrottleContext => _throttleContext;

        public List<DepositAddressWithSymbol> GetDepositAddresses(CachePolicy cachePolicy)
        {
            var contents = File.Exists(HitBtcDepositAddressesFileName)
                ? File.ReadAllText(HitBtcDepositAddressesFileName)
                : null;

            return !string.IsNullOrWhiteSpace(contents)
                ? JsonConvert.DeserializeObject<List<DepositAddressWithSymbol>>(contents)
                : new List<DepositAddressWithSymbol>();
        }

        public DepositAddress GetDepositAddress(string symbol, CachePolicy cachePolicy)
        {
            var native = GetNativeDepositAddress(symbol, cachePolicy);

            if (native == null ||  string.IsNullOrWhiteSpace(native.Address)) { return null; }

            return new DepositAddress
            {
                Address = native.Address,
                Memo = native.PaymentId
            };
        }

        public HitBtcDepositAddress GetNativeDepositAddress(string nativeSymbol, CachePolicy cachePolicy)
        {
            if (string.IsNullOrWhiteSpace(nativeSymbol)) { throw new ArgumentNullException(nameof(nativeSymbol)); }
            var effectiveSymbol = nativeSymbol.Trim().ToUpper();

            var translator = new Func<string, HitBtcDepositAddress>(text =>
                !string.IsNullOrWhiteSpace(text)
                ? JsonConvert.DeserializeObject<HitBtcDepositAddress>(text)
                : null);

            var validator = new Func<string, bool>(text =>
            {
                if (string.IsNullOrWhiteSpace(text)) { throw new ApplicationException("Address must not be null or whitespace."); }
                return translator(text) != null;
            });

            var retriever = new Func<string>(() =>
            {
                try
                {
                    var apiKey = _configClient.GetHitBtcApiKey();
                    var result = _hitBtcClient.GetDepositAddress(apiKey, nativeSymbol);
                    if (!validator(result)) { throw new ApplicationException("HitBTC deposit address response did not pass validation."); }
                    return result;
                }
                catch (Exception exception)
                {
                    _log.Error($"Failed to get HitBtc deposit address for {nativeSymbol}.{Environment.NewLine}{exception.Message}");
                    _log.Error(exception);
                    throw;
                }
            });

            var key = nativeSymbol.ToUpper();
            var context = new MongoCollectionContext(DbContext, $"hitbtc--deposit-address");
            var threshold = TimeSpan.FromDays(2);
            var cacheResult = _cacheUtil.GetCacheableEx(ThrottleContext, retriever, context, threshold, cachePolicy, validator, null, key);

            return translator(cacheResult.Contents);
        }

        private void WriteDepositAddresses(List<DepositAddressWithSymbol> depositAddresses)
        {
            var contents = depositAddresses != null
                ? JsonConvert.SerializeObject(depositAddresses)
                : null;

            File.WriteAllText(HitBtcDepositAddressesFileName, contents);
        }

        public void SetDepositAddress(DepositAddressWithSymbol depositAddress)
        {
            if (depositAddress == null) { throw new ArgumentNullException(nameof(depositAddress)); }

            var addresses = GetDepositAddresses(CachePolicy.ForceRefresh);
            var existing = addresses.FirstOrDefault(item => string.Equals(item.Symbol.Trim().ToUpper(), depositAddress.Symbol));

            if (existing != null)
            {
                existing.Address = depositAddress.Address;                
            }
            else
            {
                addresses.Add(depositAddress);
            }

            WriteDepositAddresses(addresses);
        }

        public List<HistoricalTrade> GetUserTradeHistory()
        {
            throw new NotImplementedException();
        }

        public Holding GetHolding(string symbol)
        {
            throw new NotImplementedException();
        }
        
        public bool BuyMarket(TradingPair tradingPair, decimal quantity)
        {
            throw new NotImplementedException();
        }

        public bool SellMarket(TradingPair tradingPair, decimal quantity)
        {
            throw new NotImplementedException();
        }

        private static TimeSpan HealthCacheTimeSpan = TimeSpan.FromMinutes(10);

        private List<HitBtcHealthStatusItem> ParseHealth(string contents)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(contents);
            var table = doc.DocumentNode.Descendants("table").Single();
            var tableBody = table.Descendants("tbody").Single();
            var rows = tableBody.Descendants("tr").ToList();
            var healthItems = new List<HitBtcHealthStatusItem>();
            foreach (var row in rows)
            {
                // BTC	Online	count: 10 
                // oldest: 2018-07-15 15:05	2018-07-15 15:05	Online	Online	Online	count: 0	count: 1 
                // oldest: 2018-07-15 15:11	2018-07-15 15:14	low: 3 min 
                // high: 32 min 
                // avg: 11 min 
                // —

                var cells = row.Descendants("td").ToList();
                var contentsForCells = cells.Select(item =>
                {
                    var innerText = item.InnerText;
                    var children = item.ChildNodes.ToList();

                    return item?.InnerText != null
                    ? HttpUtility.HtmlDecode(item.InnerText.Trim())
                    : null;
                }).ToList();

                var megas = cells.Select(item =>
                {
                    return item.ChildNodes.Select(subCell =>
                        HttpUtility.HtmlDecode(subCell.InnerText ?? string.Empty)
                        .Replace("\r", string.Empty)
                        .Trim()
                    )
                    .Where(sub => !string.IsNullOrWhiteSpace(sub))
                    .ToList();
                }).ToList();


                var healthItem = new HitBtcHealthStatusItem(megas);
                healthItems.Add(healthItem);
            }

            return healthItems;
        }

        private class HitBtcHealthCache
        {
            public ObjectId Id { get; set; }
            public List<HitBtcHealthStatusItem> HealthStatusItems { get; set; }
        }

        private static readonly object HealthLocker = new object();
        private static bool _isFreshening = false;
        public void KeepHealthFresh()
        {
            if (_isFreshening) { return; }
            lock (HealthLocker)
            {
                if (_isFreshening) { return; }

                try
                {
                    _isFreshening = true;
                    GetHealth(CachePolicy.PreemptCache);                    
                }
                finally
                {
                    _isFreshening = false;
                }
            }
        }

        private static HitBtcHealthCache _healthCache = null;

        public List<HitBtcHealthStatusItem> GetHealth(CachePolicy cachePolicy)
        {
            var validator = new Func<string, bool>(text =>
            {
                if (string.IsNullOrWhiteSpace(text)) { throw new ApplicationException("Health text must not be empty."); }
                var parsed = ParseHealth(text);
                return parsed != null && parsed.Any();
            });

            var retriever = new Func<string>(() =>
            {
                try
                {
                    var contents = _browserAutomationClient.GetHitBtcHealthStatusContents();
                    if (!validator(contents))
                    {
                        throw new ApplicationException("HitBtc Health text failed validation.");
                    }

                    return contents;
                }
                catch (Exception exception)
                {
                    _log.Error(exception);
                    throw;
                }
            });

            var collectionContext = new MongoCollectionContext(DbContext, "hitbtc--system-health");

            var cacheResult = _cacheUtil.GetCacheableEx(ThrottleContext, retriever, collectionContext, HealthCacheTimeSpan, cachePolicy);

            if (cacheResult != null
                && cacheResult.Id.HasValue
                && _healthCache != null
                && _healthCache.Id == cacheResult.Id.Value)
            {
                return _healthCache.HealthStatusItems;
            }

            var parsedHealth = ParseHealth(cacheResult?.Contents);

            if ((cacheResult?.Id.HasValue ?? false)
                && (parsedHealth?.Any() ?? false))
            {
                _healthCache = new HitBtcHealthCache
                {
                    Id = cacheResult.Id.Value,
                    HealthStatusItems = parsedHealth
                };
            }

            return parsedHealth;
        }

        public bool Withdraw(Commodity commodity, decimal quantity, DepositAddress address)
        {
            var nativeSymbol = _hitBtcMap.ToNativeSymbol(commodity.Symbol);
            var results = _tradeNodeUtil.Withdraw(CcxtIntegrationName, nativeSymbol, quantity, address);
            Console.WriteLine(results);

            throw new NotImplementedException("Need to verify the results.");
        }

        protected override string GetNativeOrderBookContents(string nativeSymbol, string nativeBaseSymbol)
        {
            var nativeSymbols = GetNativeSymbols(CachePolicy.OnlyUseCacheUnlessEmpty);
            var match = nativeSymbols.Data.SingleOrDefault(item =>
            {
                return string.Equals(item.BaseCurrency, nativeSymbol, StringComparison.InvariantCultureIgnoreCase)
                    && string.Equals(item.QuoteCurrency, nativeBaseSymbol, StringComparison.InvariantCultureIgnoreCase);
            });

            if (match == null)
            {
                nativeSymbols = GetNativeSymbols(CachePolicy.AllowCache);
                match = nativeSymbols.Data.SingleOrDefault(item =>
                {
                    return string.Equals(item.BaseCurrency, nativeSymbol, StringComparison.InvariantCultureIgnoreCase)
                        && string.Equals(item.QuoteCurrency, nativeBaseSymbol, StringComparison.InvariantCultureIgnoreCase);
                });
            }

            if (match == null)
            {
                throw new ApplicationException($"Failed to find a matching native symbol for {nativeSymbol}-{nativeBaseSymbol}");
            }

            var url = $"https://api.hitbtc.com/api/2/public/orderbook/{match.Id}";
            return _webUtil.Get(url);
        }

        protected override OrderBook ToOrderBook(string text, DateTime? asOf)
        {
            var native = JsonConvert.DeserializeObject<HitBtcOrderBook>(text);

            return new OrderBook
            {                
                Asks = (native?.Ask ?? new List<HitBtcOrder>()).Select(item => new Order { Price = item.Price, Quantity = item.Size }).ToList(),
                Bids = (native?.Bid ?? new List<HitBtcOrder>()).Select(item => new Order { Price = item.Price, Quantity = item.Size }).ToList(),
                AsOf = asOf
            };
        }

        public ExchangeTradingPairsWithAsOf GetTradingPairsWithAsOf(CachePolicy cachePolicy)
        {
            var nativeItems = GetNativeSymbols(cachePolicy);
            var lowerCachePolicy = cachePolicy == CachePolicy.ForceRefresh
                ? CachePolicy.AllowCache
                : cachePolicy;

            var nativeCurrencies = GetNativeCurrencies(lowerCachePolicy);
            var nativeCurrencyDictionary = new Dictionary<string, HitBtcCurrency>(StringComparer.InvariantCultureIgnoreCase);
            foreach (var nativeCurency in nativeCurrencies ?? new List<HitBtcCurrency>())
            {
                nativeCurrencyDictionary[nativeCurency.Id] = nativeCurency;
            }

            var health = GetHealth(lowerCachePolicy);
            var healthDictionary = new Dictionary<string, HitBtcHealthStatusItem>(StringComparer.InvariantCultureIgnoreCase);
            foreach (var healthItem in health ?? new List<HitBtcHealthStatusItem>())
            {
                if (string.IsNullOrWhiteSpace(healthItem?.Symbol)) { continue; }
                healthDictionary[healthItem.Symbol] = healthItem;
            }

            var tradingPairs = new List<TradingPair>();
            foreach (var nativeItem in nativeItems?.Data ?? new List<HitBtcSymbol>())
            {
                var nativeSymbol = nativeItem.BaseCurrency;
                var nativeBaseSymbol = nativeItem.QuoteCurrency;

                var nativeCurrency = nativeCurrencyDictionary.ContainsKey(nativeSymbol) ? nativeCurrencyDictionary[nativeSymbol] : null;
                var nativeBaseCurrency = nativeCurrencyDictionary.ContainsKey(nativeBaseSymbol) ? nativeCurrencyDictionary[nativeBaseSymbol] : null;

                var nativeName = !string.IsNullOrWhiteSpace(nativeCurrency?.Fullname) ? nativeCurrency.Fullname : nativeSymbol;
                var nativeBaseName = !string.IsNullOrWhiteSpace(nativeBaseCurrency?.Fullname) ? nativeBaseCurrency.Fullname : nativeBaseSymbol;

                var canon = _hitBtcMap.GetCanon(nativeSymbol);               
                var baseCanon = _hitBtcMap.GetCanon(nativeBaseSymbol);

                var healthItem = healthDictionary.ContainsKey(nativeSymbol) ? healthDictionary[nativeSymbol] : null;

                var tradingPair = new TradingPair
                {
                    CanonicalCommodityId = canon?.Id,
                    Symbol = !string.IsNullOrWhiteSpace(canon?.Symbol) ? canon.Symbol : nativeSymbol,
                    CommodityName = !string.IsNullOrWhiteSpace(canon?.Name) ? canon.Name : nativeName,
                    NativeSymbol = nativeSymbol,
                    NativeCommodityName = nativeName,

                    CanonicalBaseCommodityId = baseCanon?.Id,
                    BaseSymbol = !string.IsNullOrWhiteSpace(baseCanon?.Symbol) ? baseCanon.Symbol : nativeBaseSymbol,
                    BaseCommodityName = !string.IsNullOrWhiteSpace(baseCanon?.Name) ? baseCanon.Name : nativeBaseName,
                    NativeBaseSymbol = nativeBaseSymbol,
                    NativeBaseCommodityName = nativeBaseName,
                    LotSize = nativeItem.TickSize
                };

                // This is hacky.
                // Either pull this data from the response or create a mapping.
                if (string.Equals(tradingPair.Symbol, "ETH", StringComparison.InvariantCultureIgnoreCase)
                    && string.Equals(tradingPair.BaseSymbol, "GUSD", StringComparison.InvariantCultureIgnoreCase))
                {
                    tradingPair.PriceTick = 0.01m;
                }

                if (healthItem != null)
                {
                    tradingPair.CustomCommodityValues = new Dictionary<string, string>();
                    if (!string.IsNullOrWhiteSpace(healthItem.ProcessingTimeHighText))
                    {
                        tradingPair.CustomCommodityValues["Processing Time High"] = healthItem.ProcessingTimeHighText;
                    }

                    if (!string.IsNullOrWhiteSpace(healthItem.ProcessingTimeAverageText))
                    {
                        tradingPair.CustomCommodityValues["Processing Time Average"] = healthItem.ProcessingTimeAverageText;
                    }

                    if (!string.IsNullOrWhiteSpace(healthItem.ProcessingTimeLowText))
                    {
                        tradingPair.CustomCommodityValues["Processing Time Low"] = healthItem.ProcessingTimeLowText;
                    }

                    if (!string.IsNullOrWhiteSpace(healthItem.PendingDepositsText))
                    {
                        tradingPair.CustomCommodityValues["Pending Deposits"] = healthItem.PendingDepositsText;
                    }

                    if (!string.IsNullOrWhiteSpace(healthItem.PendingWithdrawalsText))
                    {
                        tradingPair.CustomCommodityValues["Pending Withdrawals"] = healthItem.PendingWithdrawalsText;
                    }
                }

                tradingPairs.Add(tradingPair);
            }

            return new ExchangeTradingPairsWithAsOf
            {
                AsOfUtc = nativeItems.AsOfUtc,
                TradingPairs = tradingPairs
            };
        }

        public List<OpenOrderForTradingPair> GetOpenOrders(string symbol, string baseSymbol, CachePolicy cachePolicy)
        {
            return GetOpenOrders(cachePolicy)
                .Where(item => string.Equals(item.Symbol, symbol, StringComparison.InvariantCultureIgnoreCase) && string.Equals(item.BaseSymbol, baseSymbol, StringComparison.InvariantCultureIgnoreCase))
                .ToList();   
        }

        private static Dictionary<string, OrderType> HitBtcOrderTypeDictionary = new Dictionary<string, OrderType>(StringComparer.InvariantCultureIgnoreCase)
        {
            { "buy", OrderType.Bid },
            { "ask", OrderType.Ask },
        };

        public List<OpenOrderForTradingPair> GetOpenOrders(CachePolicy cachePolicy)
        {
            return GetOpenOrdersWithAsOf(cachePolicy).Data;
        }

        private AsOfWrapper<List<OpenOrderForTradingPair>> GetOpenOrdersWithAsOf(CachePolicy cachePolicy)
        {
            var nativeWithAsOf = GetNativeOpenOrders(cachePolicy);
            if (!nativeWithAsOf.Data.Any())
            {
                return new AsOfWrapper<List<OpenOrderForTradingPair>>
                {
                    AsOfUtc = nativeWithAsOf.AsOfUtc,
                    Data = new List<OpenOrderForTradingPair>()
                };
            }

            var nativeSymbols = GetNativeSymbols(CachePolicy.OnlyUseCacheUnlessEmpty);
            var hasReloadedNativeSymbols = false;

            var translated = nativeWithAsOf.Data.Select(queryNativeOpenOrder =>
            {
                var matchingNativeCombo = nativeSymbols.Data.Where(queryNativeSymbol =>
                    string.Equals(queryNativeSymbol.Id, queryNativeOpenOrder.Symbol, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();

                if (matchingNativeCombo == null && !hasReloadedNativeSymbols)
                {
                    nativeSymbols = GetNativeSymbols(CachePolicy.ForceRefresh);
                    hasReloadedNativeSymbols = true;

                    matchingNativeCombo = nativeSymbols.Data.Where(queryNativeSymbol =>
                    string.Equals(queryNativeSymbol.Id, queryNativeOpenOrder.Symbol, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
                }

                string nativeSymbol = null;
                string nativeBaseSymbol = null;

                if (matchingNativeCombo != null)
                {
                    nativeSymbol = matchingNativeCombo?.BaseCurrency;
                    nativeBaseSymbol = matchingNativeCombo?.QuoteCurrency;
                }
                else
                {
                    var comboText = queryNativeOpenOrder.Symbol;
                    if (comboText.Length > 3)
                    {
                        nativeBaseSymbol = comboText.Substring(comboText.Length - 3);
                        nativeSymbol = comboText.Substring(0, comboText.Length - nativeBaseSymbol.Length);
                    }
                    else
                    {
                        nativeSymbol = comboText;
                        nativeSymbol = comboText;
                    }
                }

                var canonSymbol = _hitBtcMap.ToCanonicalSymbol(nativeSymbol);
                var canonBaseSymbol = _hitBtcMap.ToCanonicalSymbol(nativeBaseSymbol);

                return new OpenOrderForTradingPair
                {
                    OrderId = queryNativeOpenOrder.ClientOrderId,
                    Price = queryNativeOpenOrder.Price,
                    Quantity = queryNativeOpenOrder.Quantity,
                    Symbol = canonSymbol,
                    BaseSymbol = canonBaseSymbol,
                    OrderType = HitBtcOrderTypeDictionary.ContainsKey(queryNativeOpenOrder.Side)
                        ? HitBtcOrderTypeDictionary[queryNativeOpenOrder.Side]
                        : OrderType.Unknown
                };
            })
            .ToList();

            return new AsOfWrapper<List<OpenOrderForTradingPair>>
            {
                AsOfUtc = nativeWithAsOf?.AsOfUtc,
                Data = translated
            };
        }

        private AsOfWrapper<List<HitBtcClientOrder>> GetNativeOpenOrders(CachePolicy cachePolicy)
        {
            var translator = new Func<string, List<HitBtcClientOrder>>(text =>
                !string.IsNullOrWhiteSpace(text)
                    ? JsonConvert.DeserializeObject<List<HitBtcClientOrder>>(text)
                    : new List<HitBtcClientOrder>());

            var validator = new Func<string, bool>(text =>
            {
                if (string.IsNullOrWhiteSpace(text)) { throw new ApplicationException("Hitbtc returned a null or whitespace respojse when requesting open orders."); }
                translator(text);

                return true;
            });

            var retriever = new Func<string>(() =>
            {
                ApiKey apiKey;
                try
                {
                    apiKey = _configClient.GetHitBtcApiKey();
                }
                catch (Exception exception)
                {
                    _log.Error(exception);
                    throw;
                }

                try
                {
                    var contents = _hitBtcClient.GetOpenOrdersRaw(apiKey);
                    if (!validator(contents))
                    {
                        throw new ApplicationException($"Vaidation for {Name} open orders failed.");
                    }
                    return contents;
                }
                catch (Exception exception)
                {
                    _log.Error($"Failed to retrieve {Name} open orders.{Environment.NewLine}{exception.Message}");
                    _log.Error(exception);
                    throw;
                }
            });

            var cacheResult = _cacheUtil.GetCacheableEx(ThrottleContext, retriever, OpenOrdersCollectionContext, OpenOrdersThreshold, cachePolicy, validator);

            return new AsOfWrapper<List<HitBtcClientOrder>>
            {
                AsOfUtc = cacheResult?.AsOf,
                Data = translator(cacheResult?.Contents)
            };
        }

        public void CancelOrder(string orderId)
        {
            var apiKey = _configClient.GetHitBtcApiKey();
            var response = _hitBtcClient.CancelOrderRaw(apiKey, orderId);
            _log.Info($"Response from hitbtc after requesting to cancel order \"{orderId}\".{Environment.NewLine}{response}");
        }

        public LimitOrderResult BuyLimit(string symbol, string baseSymbol, QuantityAndPrice quantityAndPrice)
        {
            if (string.IsNullOrWhiteSpace(symbol)) { throw new ArgumentNullException(nameof(symbol)); }
            if (string.IsNullOrWhiteSpace(baseSymbol)) { throw new ArgumentNullException(nameof(baseSymbol)); }
            if (quantityAndPrice == null) { throw new ArgumentNullException(nameof(quantityAndPrice)); }
            if (quantityAndPrice.Price <= 0) { throw new ArgumentOutOfRangeException(nameof(quantityAndPrice.Price)); }

            var nativeSymbol = _hitBtcMap.ToNativeSymbol(symbol);
            var nativeBaseSymbol = _hitBtcMap.ToNativeSymbol(baseSymbol);

            var nativeTradingPairs = GetNativeSymbols(CachePolicy.OnlyUseCacheUnlessEmpty);
            var match = nativeTradingPairs.Data.SingleOrDefault(item =>
                string.Equals(item.BaseCurrency, nativeSymbol, StringComparison.InvariantCultureIgnoreCase)
                && string.Equals(item.QuoteCurrency, nativeBaseSymbol, StringComparison.InvariantCultureIgnoreCase));

            if (match == null)
            {
                nativeTradingPairs = GetNativeSymbols(CachePolicy.ForceRefresh);
                match = nativeTradingPairs.Data.SingleOrDefault(item =>
                    string.Equals(item.BaseCurrency, nativeSymbol, StringComparison.InvariantCultureIgnoreCase)
                    && string.Equals(item.QuoteCurrency, nativeBaseSymbol, StringComparison.InvariantCultureIgnoreCase));

                if (match == null)
                {
                    throw new ApplicationException($"Failed to find a matching hitbtc trading pair symbol for {nativeSymbol}-{nativeBaseSymbol}.");
                }
            }

            var tradingPairSymbol = match.Id;

            var apiKey = _configClient.GetHitBtcApiKey();
            var response = _hitBtcClient.BuyLimitRaw(apiKey, tradingPairSymbol, quantityAndPrice.Quantity, quantityAndPrice.Price);

            _log.Info($"Response from hitbtc after requesting to buy {quantityAndPrice.Quantity} {nativeSymbol} at {quantityAndPrice.Price} {nativeBaseSymbol}.{Environment.NewLine}{response}");

            return new LimitOrderResult
            {
                WasSuccessful = true
            };
        }

        public LimitOrderResult SellLimit(string symbol, string baseSymbol, QuantityAndPrice quantityAndPrice)
        {
            if (string.IsNullOrWhiteSpace(symbol)) { throw new ArgumentNullException(nameof(symbol)); }
            if (string.IsNullOrWhiteSpace(baseSymbol)) { throw new ArgumentNullException(nameof(baseSymbol)); }
            if (quantityAndPrice == null) { throw new ArgumentNullException(nameof(quantityAndPrice)); }
            if (quantityAndPrice.Price <= 0) { throw new ArgumentOutOfRangeException(nameof(quantityAndPrice.Price)); }

            var nativeSymbol = _hitBtcMap.ToNativeSymbol(symbol);
            var nativeBaseSymbol = _hitBtcMap.ToNativeSymbol(baseSymbol);

            var nativeTradingPairs = GetNativeSymbols(CachePolicy.OnlyUseCacheUnlessEmpty);
            var match = nativeTradingPairs.Data.SingleOrDefault(item =>
                string.Equals(item.BaseCurrency, nativeSymbol, StringComparison.InvariantCultureIgnoreCase)
                && string.Equals(item.QuoteCurrency, nativeBaseSymbol, StringComparison.InvariantCultureIgnoreCase));

            if (match == null)
            {
                nativeTradingPairs = GetNativeSymbols(CachePolicy.ForceRefresh);
                match = nativeTradingPairs.Data.SingleOrDefault(item =>
                    string.Equals(item.BaseCurrency, nativeSymbol, StringComparison.InvariantCultureIgnoreCase)
                    && string.Equals(item.QuoteCurrency, nativeBaseSymbol, StringComparison.InvariantCultureIgnoreCase));

                if (match == null)
                {
                    throw new ApplicationException($"Failed to find a matching hitbtc trading pair symbol for {nativeSymbol}-{nativeBaseSymbol}.");
                }
            }

            var tradingPairSymbol = match.Id;

            var apiKey = _configClient.GetHitBtcApiKey();
            var response = _hitBtcClient.SellLimitRaw(apiKey, tradingPairSymbol, quantityAndPrice.Quantity, quantityAndPrice.Price);

            _log.Info($"Response from hitbtc after requesting to sell {quantityAndPrice.Quantity} {nativeSymbol} at {quantityAndPrice.Price} {nativeBaseSymbol}.{Environment.NewLine}{response}");

            return new LimitOrderResult
            {
                WasSuccessful = true
            };
        }

        public OpenOrdersWithAsOf GetOpenOrdersForTradingPairV2(string symbol, string baseSymbol, CachePolicy cachePolicy)
        {
            var openOrdersWithAsOf = GetOpenOrdersWithAsOf(cachePolicy);
            var openOrders = openOrdersWithAsOf.Data
                .Where(item => string.Equals(item.Symbol, symbol, StringComparison.InvariantCultureIgnoreCase) && string.Equals(item.BaseSymbol, baseSymbol, StringComparison.InvariantCultureIgnoreCase))
                .Select(item => item.CloneAs<OpenOrder>())
                .ToList();

            return new OpenOrdersWithAsOf
            {
                AsOfUtc = openOrdersWithAsOf.AsOfUtc,
                OpenOrders = openOrders
            };
        }

        public HistoryContainer GetUserTradeHistoryV2(CachePolicy cachePolicy)
        {
            var nativeSymbolsWithAsOf = GetNativeSymbols(cachePolicy);
            var nativeHistoryWithAsOf = GetNativeUserTradeHistory(cachePolicy);
            var nativeTransactionHistoryWithAsOf = GetNativeTransactionHistory(cachePolicy);

            var tradeHistory = (nativeHistoryWithAsOf?.Data ?? new List<HitBtcApiTradeHistoryItem>())
                    .OrderByDescending(nativeItem => nativeItem.TimeStamp)
                    .Select(nativeItem => NativeHistoryItemToHistoricalTrade(nativeItem, nativeSymbolsWithAsOf.Data)).ToList();

            var transactionHistory = (nativeTransactionHistoryWithAsOf?.Data ?? new List<HitBtcClientTransactionItem>())
                    .OrderByDescending(nativeItem => nativeItem.CreatedAt)
                    .Select(nativeItem => NativeTransactionItemToHistoricalTrade(nativeItem, nativeSymbolsWithAsOf.Data)).ToList();

            DateTime? earlierAsOfUtc = null;
            if (nativeSymbolsWithAsOf?.AsOfUtc == null && nativeHistoryWithAsOf?.AsOfUtc != null)
            {
                earlierAsOfUtc = nativeHistoryWithAsOf.AsOfUtc;
            }
            else if (nativeSymbolsWithAsOf?.AsOfUtc != null && nativeHistoryWithAsOf?.AsOfUtc == null)
            {
                earlierAsOfUtc = nativeSymbolsWithAsOf.AsOfUtc;
            }
            else
            {
                earlierAsOfUtc = nativeSymbolsWithAsOf.AsOfUtc < nativeHistoryWithAsOf.AsOfUtc
                    ? nativeSymbolsWithAsOf.AsOfUtc
                    : nativeHistoryWithAsOf.AsOfUtc;
            }

            return new HistoryContainer
            {
                AsOfUtc = earlierAsOfUtc,
                History = tradeHistory.Union(transactionHistory)
                    .OrderByDescending(item => item.TimeStampUtc)
                    .ToList()
            };
        }

        private HistoricalTrade NativeHistoryItemToHistoricalTrade(
            HitBtcApiTradeHistoryItem nativeItem,
            List<HitBtcSymbol> nativeSymbols)
        {
            if (nativeItem == null) { return null; }

            var tradeTypeDictionary = new Dictionary<string, TradeTypeEnum>(StringComparer.InvariantCultureIgnoreCase)
            {
                { "buy", TradeTypeEnum.Buy },
                { "sell", TradeTypeEnum.Sell },
            };

            var matchingNativeCombo = nativeSymbols.SingleOrDefault(queryNativeSymbol =>
                string.Equals(queryNativeSymbol.Id, nativeItem.Symbol));

            var nativeSymbol = matchingNativeCombo?.BaseCurrency;
            var nativeBaseSymbol = matchingNativeCombo?.FeeCurrency;

            var symbol = _hitBtcMap.ToCanonicalSymbol(nativeSymbol);
            var baseSymbol = _hitBtcMap.ToCanonicalSymbol(nativeBaseSymbol);

            return new HistoricalTrade
            {
                NativeId = nativeItem.OrderId.ToString(),
                TradeType = tradeTypeDictionary.ContainsKey(nativeItem.Side)
                    ? tradeTypeDictionary[nativeItem.Side]
                    : TradeTypeEnum.Unknown,
                Price = nativeItem.Price ?? 0,
                Quantity = nativeItem.Quantity ?? 0,
                FeeQuantity = nativeItem.Fee ?? 0,
                TimeStampUtc = nativeItem.TimeStamp,

                Symbol = symbol,
                BaseSymbol = baseSymbol
            };
        }

        private HistoricalTrade NativeTransactionItemToHistoricalTrade(
            HitBtcClientTransactionItem nativeItem,
            List<HitBtcSymbol> nativeSymbols)
        {
            if (nativeItem == null) { return null; }

            var tradeTypeDictionary = new Dictionary<string, TradeTypeEnum>(StringComparer.InvariantCultureIgnoreCase)
            {
                { "payout", TradeTypeEnum.Withdraw },
                { "payin", TradeTypeEnum.Deposit },
                { "deposit", TradeTypeEnum.Deposit },
                { "exchangeToBank", TradeTypeEnum.ExchangeToBank },
                { "bankToExchange", TradeTypeEnum.BankToExchange },
            };

            return new HistoricalTrade
            {
                NativeId = nativeItem.Id,
                
                TimeStampUtc = nativeItem.CreatedAt,
                SuccessTimeStampUtc = nativeItem.UpdatedAt,

                Quantity = nativeItem.Amount ?? default(decimal),
                FeeQuantity = nativeItem.Fee ?? default(decimal),
                TradeType = tradeTypeDictionary.ContainsKey(nativeItem.Type)
                    ? tradeTypeDictionary[nativeItem.Type]
                    : TradeTypeEnum.Unknown,
                TransactionHash = nativeItem.Hash,
                Symbol = nativeItem.Currency,
                FeeCommodity = nativeItem.Currency
            };
        }

        private AsOfWrapper<List<HitBtcApiTradeHistoryItem>> GetNativeUserTradeHistory(CachePolicy cachePolicy)
        {
            var translator = new Func<string, List<HitBtcApiTradeHistoryItem>>(text =>
                !string.IsNullOrWhiteSpace(text)
                ? JsonConvert.DeserializeObject<List<HitBtcApiTradeHistoryItem>>(text)
                : new List<HitBtcApiTradeHistoryItem>());

            var validator = new Func<string, bool>(text =>
            {
                if (string.IsNullOrWhiteSpace(text)) { throw new ApplicationException("HitBtc returned a null or whitespace response when requesting history."); }
                var translated = translator(text);

                return true;
            });

            var retriever = new Func<string>(() =>
            {
                try
                {
                    var apiKey = _configClient.GetHitBtcApiKey();
                    var contents = _hitBtcClient.GetTradeHistoryRaw(apiKey);

                    if (!validator(contents)) { throw new ApplicationException("HitBtc's response when requesting history failed validation."); }

                    return contents;
                }
                catch (Exception exception)
                {
                    _log.Error(exception);
                    throw;
                }
            });

            var collectionContext = new MongoCollectionContext(DbContext, "hibtc--get-trade-history");
            var cacheResult = _cacheUtil.GetCacheableEx(ThrottleContext, retriever, collectionContext, HistoryThreshold, cachePolicy, validator);

            return new AsOfWrapper<List<HitBtcApiTradeHistoryItem>>
            {
                AsOfUtc = cacheResult?.AsOf,
                Data = translator(cacheResult?.Contents)
            };
        }

        private AsOfWrapper<List<HitBtcClientTransactionItem>> GetNativeTransactionHistory(CachePolicy cachePolicy)
        {
            var translator = new Func<string, List<HitBtcClientTransactionItem>>(text =>
                !string.IsNullOrWhiteSpace(text)
                ? JsonConvert.DeserializeObject<List<HitBtcClientTransactionItem>>(text)
                : new List<HitBtcClientTransactionItem>());

            var validator = new Func<string, bool>(text =>
            {
                if (string.IsNullOrWhiteSpace(text)) { throw new ApplicationException("HitBtc returned a null or whitespace response when requesting transaction history."); }
                var translated = translator(text);

                return true;
            });

            var retriever = new Func<string>(() =>
            {
                try
                {
                    var apiKey = _configClient.GetHitBtcApiKey();
                    var contents = _hitBtcClient.GetTransactionsHistoryRaw(apiKey);

                    if (!validator(contents)) { throw new ApplicationException("HitBtc's response when requesting transaction history failed validation."); }

                    return contents;
                }
                catch (Exception exception)
                {
                    _log.Error(exception);
                    throw;
                }
            });

            var collectionContext = new MongoCollectionContext(DbContext, "hibtc--get-transaction-history");
            var cacheResult = _cacheUtil.GetCacheableEx(ThrottleContext, retriever, collectionContext, HistoryThreshold, cachePolicy, validator);

            return new AsOfWrapper<List<HitBtcClientTransactionItem>>
            {
                AsOfUtc = cacheResult?.AsOf,
                Data = translator(cacheResult?.Contents)
            };
        }

        private IMongoCollectionContext OpenOrdersCollectionContext => new MongoCollectionContext(DbContext, "hitbtc--open-orders");

        //private string AuthRequest(string resource, Method method = Method.GET)
        //{
        //    var apiKey = _configClient.GetHitBtcApiKey();

        //    var url = "https://api.hitbtc.com/api/2/";
        //    var client = new RestClient(url)
        //    {
        //        Authenticator = new HttpBasicAuthenticator(apiKey.Key, apiKey.Secret)
        //    };

        //    var request = new RestRequest(resource, method, DataFormat.Json);
        //    return client.Execute(request).Content;
        //}
    }
}
