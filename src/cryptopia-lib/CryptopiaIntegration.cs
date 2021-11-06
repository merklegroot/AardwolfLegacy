using config_connection_string_lib;
using cryptopia_lib.Models;
using cryptopia_lib.Res;
using date_time_lib;
using log_lib;
using mongo_lib;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using res_util_lib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Configuration;
using System.Reflection;
using System.Threading;
using cache_lib.Models;
using trade_model;
using trade_node_integration;
using trade_res;
using web_util;
using cache_lib;

namespace cryptopia_lib
{
    // https://www.cryptopia.co.nz/Forum/Thread/255
    public class CryptopiaIntegration : ICryptopiaIntegration
    {
        private static TimeSpan CryptopiaOrderBookThreshold = TimeSpan.FromMinutes(10);

        private static object Locker = new object();

        public string Name => "Cryptopia";
        public Guid Id => new Guid("4EDD89C9-5685-48C7-A877-9CBB1DF01F8B");

        private readonly static TimeSpan NativeTradingPairsThreshold = TimeSpan.FromMinutes(10);
        private readonly static TimeSpan CurrenciesThreshold = TimeSpan.FromMinutes(10);

        private const string CcxtIntegrationName = "cryptopia";
        private const string DatabaseName = "cryptopia";

        private static readonly ThrottleContext ThrottleContext = new ThrottleContext
        {
            Locker = new object(),
            ThrottleThreshold = TimeSpan.FromSeconds(2.5)
        };
        
        private readonly IGetConnectionString _getConnectionString;
        private readonly IWebUtil _webUtil;
        private readonly ITradeNodeUtil _tradeNodeUtil;
        private readonly CacheUtil _cacheUtil;
        private readonly ILogRepo _log;
        public readonly CryptopiaMap _cryptopiaMap = new CryptopiaMap();

        public CryptopiaIntegration(
            IGetConnectionString getConnectionString,
            ITradeNodeUtil tradeNodeUtil,
            IWebUtil webUtil,
            ILogRepo log)
        {            
            _getConnectionString = getConnectionString;
            _webUtil = webUtil;
            _tradeNodeUtil = tradeNodeUtil;
            _log = log;

            _cacheUtil = new CacheUtil();
            
            ToggleAllowUnsafeHeaderParsing(true);
        }

        // ugly static caching, but good enough for now.
        private static Dictionary<string, decimal> _cachedWithdrawalFees;
        public Dictionary<string, decimal> GetWithdrawalFees(CachePolicy cachePolicy)
        {
            if (_cachedWithdrawalFees != null) { return _cachedWithdrawalFees; }
            
            var dict = new Dictionary<string, decimal>();
            var currencies = GetNativeCurrencies(cachePolicy);
            foreach (var currency in currencies.Data)
            {
                var symbol = currency.Symbol.Trim().ToUpper();
                var canonicalSymbol = _cryptopiaMap.ToCanonicalSymbol(symbol);
                dict[canonicalSymbol] = currency.WithdrawFee;
            }

            return _cachedWithdrawalFees = dict;
        }

        private CryptopiaResponseMessage<CryptopiaMarketOrdersPayload> GetNativeMarketOrders(TradingPair nativeTradingPair, CachePolicy cachePolicy)
        {
            // https://www.cryptopia.co.nz/api/GetMarketOrders/DOT_BTC
            var effectiveSymbol = nativeTradingPair.Symbol.Trim().ToUpper();
            var effectiveBaseSymbol = nativeTradingPair.BaseSymbol.Trim().ToUpper();
            var url = $"https://www.cryptopia.co.nz/api/GetMarketOrders/{effectiveSymbol}_{effectiveBaseSymbol}";

            var retriever = new Func<string>(() => _webUtil.Get(url));
            var validator = new Func<string, bool>(text => !string.IsNullOrWhiteSpace(text));
            var context = new MongoCollectionContext(_getConnectionString.GetConnectionString(), DatabaseName, $"cryptopia--get-market-orders--{effectiveSymbol}-{effectiveBaseSymbol}");

            var cacheResult = _cacheUtil.GetCacheableEx(ThrottleContext, retriever, context, CryptopiaOrderBookThreshold, cachePolicy, validator);

            var nativeMarketOrders = JsonConvert.DeserializeObject<CryptopiaResponseMessage<CryptopiaMarketOrdersPayload>>(cacheResult.Contents);
            nativeMarketOrders.AsOf = cacheResult?.AsOf;

            return nativeMarketOrders;
        }

        public OrderBook GetOrderBook(TradingPair tradingPair, CachePolicy cachePolicy)
        {
            var nativeTradingPair = new TradingPair(_cryptopiaMap.ToNativeSymbol(tradingPair.Symbol), _cryptopiaMap.ToNativeSymbol(tradingPair.BaseSymbol));

            var nativeOrders = GetNativeMarketOrders(nativeTradingPair, cachePolicy);
            if (nativeOrders == null || nativeOrders.Data == null) { return null; }
            var orderBook = new OrderBook
            {
                Bids = nativeOrders.Data.Buy?.Select(item => new Order(item.Price, item.Volume)).ToList() ?? new List<Order>(),
                Asks = nativeOrders.Data.Sell?.Select(item => new Order(item.Price, item.Volume)).ToList() ?? new List<Order>(),
                AsOf = nativeOrders.AsOf
            };

            return orderBook;
        }

        public List<TradingPair> GetTradingPairs(CachePolicy cachePolicy)
        {
            var nativePairs = GetNativeTradingPairs(cachePolicy);
            if (nativePairs == null) { return new List<TradingPair>(); }

            // var coinStatuses = GetNativeCoinStatuses(cachePolicy);
            return nativePairs.Data
                //.Where(nativePair =>
                //{
                //    var matchingCoinStatus = coinStatuses?.aaData?.FirstOrDefault(nativeStatus => string.Equals(nativePair.Symbol, nativeStatus.Symbol, StringComparison.InvariantCultureIgnoreCase));

                //    // For now, if we can't find a matching coin status,
                //    // return the trading pair and let the user decide if they want to use
                //    // it or not.
                //    return (matchingCoinStatus != null
                //        && matchingCoinStatus.Connections >= 1
                //        && matchingCoinStatus.WalletStatus != CryptopiaWalletStatusEnum.Delisting);
                //})
                .Select(nativePair =>
                {
                    var canon = _cryptopiaMap.GetCanon(nativePair.Symbol);
                    var baseCanon = _cryptopiaMap.GetCanon(nativePair.BaseSymbol);
                    // var canonicalSymbol = NativeSymbolToCanonicalSymbol(nativePair.Symbol);
                    return new TradingPair
                    {
                        CanonicalCommodityId = canon?.Id,
                        Symbol = !string.IsNullOrWhiteSpace(canon?.Symbol) ? canon.Symbol : nativePair.Symbol,
                        NativeSymbol = nativePair.Symbol,
                        CommodityName = !string.IsNullOrWhiteSpace(canon?.Name) ? canon.Name : nativePair.Currency,
                        NativeCommodityName = nativePair.Currency,

                        CanonicalBaseCommodityId = baseCanon?.Id,
                        BaseSymbol = !string.IsNullOrWhiteSpace(baseCanon?.Symbol) ? baseCanon.Symbol : nativePair.Symbol,
                        NativeBaseSymbol = nativePair.BaseSymbol,
                        BaseCommodityName = !string.IsNullOrWhiteSpace(baseCanon?.Name) ? baseCanon.Name : nativePair.BaseCurrency,
                        NativeBaseCommodityName = nativePair.BaseCurrency
                    };
                })
                .ToList();
        }

        public CryptopiaResponseMessage<CryptopiaCurrenciesPayload> GetNativeCurrencies(CachePolicy cachePolicy)
        {
            const string Url = "https://www.cryptopia.co.nz/api/GetCurrencies";
            var retriever = new Func<string>(() => _webUtil.Get(Url));
            var translator = new Func<string, CryptopiaResponseMessage<CryptopiaCurrenciesPayload>>(text =>
                JsonConvert.DeserializeObject<CryptopiaResponseMessage<CryptopiaCurrenciesPayload>>(text));

            var validator = new Func<string, bool>(text =>
            {
                if (string.IsNullOrWhiteSpace(text)) { return false; }
                var translated = translator(text);
                if (translated == null || !translated.Success) { return false; }

                return true;
            });

            var context = new MongoCollectionContext(_getConnectionString.GetConnectionString(), DatabaseName, "cryptopia--get-currencies");

            try
            {
                var cacheResult = _cacheUtil.GetCacheableEx(ThrottleContext, retriever, context, CurrenciesThreshold, cachePolicy, validator);
                return translator(cacheResult.Contents);
            }
            catch(Exception exception)
            {
                _log.Error(exception);

                if (cachePolicy == CachePolicy.AllowCache)
                {
                    return GetNativeCurrencies(CachePolicy.OnlyUseCacheUnlessEmpty);
                }

                throw;
            }
        }
                
        private CryptopiaResponseMessage<CryptopiaTradingPairPayload> GetNativeTradingPairs(CachePolicy cachePolicy)
        {
            var retriever = new Func<string>(() =>
            {
                const string Url = "https://www.cryptopia.co.nz/api/GetTradePairs";
                return _webUtil.Get(Url);
            });

            var translator = new Func<string, CryptopiaResponseMessage<CryptopiaTradingPairPayload>>(text =>
                !string.IsNullOrWhiteSpace(text)
                ? JsonConvert.DeserializeObject<CryptopiaResponseMessage<CryptopiaTradingPairPayload>>(text)
                : null
            );

            var validator = new Func<string, bool>(text => translator(text)?.Success ?? false);
            var collectionContext = new MongoCollectionContext(_getConnectionString.GetConnectionString(), DatabaseName, "cryptopia--get-trading-pairs");

            var cacheResult = _cacheUtil.GetCacheableEx(ThrottleContext, retriever, collectionContext, NativeTradingPairsThreshold, cachePolicy, validator);

            return translator(cacheResult.Contents);
        }

        // Enable/disable useUnsafeHeaderParsing.
        // See http://o2platform.wordpress.com/2010/10/20/dealing-with-the-server-committed-a-protocol-violation-sectionresponsestatusline/
        private static bool ToggleAllowUnsafeHeaderParsing(bool enable)
        {
            //Get the assembly that contains the internal class
            Assembly assembly = Assembly.GetAssembly(typeof(SettingsSection));
            if (assembly != null)
            {
                //Use the assembly in order to get the internal type for the internal class
                Type settingsSectionType = assembly.GetType("System.Net.Configuration.SettingsSectionInternal");
                if (settingsSectionType != null)
                {
                    //Use the internal static property to get an instance of the internal settings class.
                    //If the static instance isn't created already invoking the property will create it for us.
                    object anInstance = settingsSectionType.InvokeMember("Section",
                    BindingFlags.Static | BindingFlags.GetProperty | BindingFlags.NonPublic, null, null, new object[] { });
                    if (anInstance != null)
                    {
                        //Locate the private bool field that tells the framework if unsafe header parsing is allowed
                        FieldInfo aUseUnsafeHeaderParsing = settingsSectionType.GetField("useUnsafeHeaderParsing", BindingFlags.NonPublic | BindingFlags.Instance);
                        if (aUseUnsafeHeaderParsing != null)
                        {
                            aUseUnsafeHeaderParsing.SetValue(anInstance, enable);
                            return true;
                        }

                    }
                }
            }
            return false;
        }

        public List<CommodityForExchange> GetCommodities(CachePolicy cachePolicy)
        {
            var currencies = GetNativeCurrencies(cachePolicy);

            // Cryptopia added a captcha to their coin statuses page.
            // Disabling this until a workaround can be implemented.
            // var coinStatuses = GetNativeCoinStatuses(cachePolicy);

            return currencies.Data.Select(queryCurrency =>
            {
                var canon = _cryptopiaMap.GetCanon(queryCurrency.Symbol);

                return new CommodityForExchange
                {
                    CanonicalId = canon?.Id,
                    Symbol = !string.IsNullOrWhiteSpace(canon?.Symbol) ? canon.Symbol : queryCurrency.Symbol,
                    NativeSymbol = queryCurrency.Symbol,
                    WithdrawalFee = queryCurrency.WithdrawFee,
                    Name = !string.IsNullOrWhiteSpace(canon?.Name) ? canon.Name : queryCurrency.Name,
                    NativeName = queryCurrency.Name,
                    //CanDeposit = matchingCoinStatus != null ? matchingCoinStatus.Connections > 0 : (bool?)null,
                    //CanWithdraw = matchingCoinStatus != null ? matchingCoinStatus.Connections > 0 : (bool?)null,
                };
            }).ToList();
        }

        public decimal? GetWithdrawalFee(string symbol, CachePolicy cachePolicy)
        {
            var fees = GetWithdrawalFees(cachePolicy);
            return fees.ContainsKey(symbol) ? fees[symbol] : (decimal?)null;
        }

        private static T Throttle<T>(Func<T> method)
        {
            lock (Locker)
            {
                try
                {
                    return method();
                }
                finally
                {
                    Thread.Sleep(250);
                }
            }
        }

        private static TimeSpan GetHoldingsThreshold = TimeSpan.FromMinutes(5);

        public HoldingInfo GetHoldings(CachePolicy cachePolicy)
        {
            var retriever = new Func<string>(() => _tradeNodeUtil.FetchBalance(CcxtIntegrationName));
            var context = new MongoCollectionContext(_getConnectionString.GetConnectionString(), DatabaseName, "cryptopia--fetch-balance");
            var cacheResult = _cacheUtil.GetCacheableEx(ThrottleContext, retriever, context, GetHoldingsThreshold, cachePolicy);
            var json = (JObject)JsonConvert.DeserializeObject(cacheResult.Contents);

            var holdingInfo = new HoldingInfo
            {
                TimeStampUtc = cacheResult.CacheAge.HasValue ? DateTime.UtcNow-cacheResult.CacheAge.Value : DateTime.UtcNow,
                Holdings = new List<Holding>()
            };

            foreach (JProperty item in json.Children())
            {
                var name = item.Name;

                decimal? free = null;
                decimal? used = null;
                decimal? total = null;

                foreach (var sub in item.Value.Children())
                {
                    if (!(sub is JProperty subItem)) { continue; }
                    var subItemName = subItem.Name;
                    if (string.Equals(subItemName, "free", StringComparison.InvariantCultureIgnoreCase))
                    {
                        var val = subItem.Value;
                        free = val.Value<decimal?>();
                    }
                    else if (string.Equals(subItemName, "used", StringComparison.InvariantCultureIgnoreCase))
                    {
                        var val = subItem.Value;
                        used = val.Value<decimal?>();
                    }
                    else if (string.Equals(subItemName, "total", StringComparison.InvariantCultureIgnoreCase))
                    {
                        var val = subItem.Value;
                        total = val.Value<decimal?>();
                    }
                }

                if (free.HasValue || used.HasValue || total.HasValue)
                {
                    holdingInfo.Holdings.Add(
                        new Holding
                        {
                            Asset = name,
                            Available = free ?? 0,
                            InOrders = used ?? 0,
                            Total = total ?? 0
                        });
                }
            }

            return holdingInfo;
        }       

        public List<DepositAddressWithSymbol> GetDepositAddresses(CachePolicy cachePolicy)
        {
            return new List<DepositAddressWithSymbol>();
        }

        public DepositAddress GetDepositAddress(string symbol, CachePolicy cachePolicy)
        {
            return null;
        }

        public List<HistoricalTrade> GetUserTradeHistory()
        {
            return null;
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

        public CryptopiaGetCoinStatusResponse GetNativeCoinStatuses(CachePolicy cachePolicy)
        {
            var translator = new Func<string, CryptopiaGetCoinStatusResponse>(text =>
                !string.IsNullOrWhiteSpace(text)
                    ? JsonConvert.DeserializeObject<CryptopiaGetCoinStatusResponse>(text)
                    : null
            );

            var validator = new Func<string, bool>(text =>
            {
                if (string.IsNullOrWhiteSpace(text)) { throw new ApplicationException("Must not be null or whitespace"); }
                return translator(text) != null;
            });

            var retriever = new Func<string>(() =>
            {
                try
                {
                    var nonce = DateTimeUtil.GetUnixTimeStamp13Digit();
                    var url = $"https://www.cryptopia.co.nz/CoinInfo/GetCoinInfo?_={nonce}";
                    var response = _webUtil.Get(url);
                    var validationResult = validator(response);
                    if (!validationResult)
                    {
                        throw new ApplicationException("Cryptopia coin info failed validation.");
                    }

                    return response;
                }
                catch(Exception exception)
                {
                    _log.Error(exception);
                    throw;
                }
            });

            var collectionContext = new MongoCollectionContext(_getConnectionString.GetConnectionString(), DatabaseName, "cryptopia--get-coin-info");

            var threshold = TimeSpan.FromMinutes(5);
            var cacheResult = _cacheUtil.GetCacheableEx(ThrottleContext, retriever, collectionContext, threshold, cachePolicy, validator);

            return translator(cacheResult?.Contents);
        }
    }
}
