using livecoin_lib.Models;
using log_lib;
using mongo_lib;
using MongoDB.Driver;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using trade_lib;
using cache_lib.Models;
using trade_model;
using trade_node_integration;
using trade_res;
using web_util;
using cache_lib;
using config_client_lib;
using MongoDB.Bson;
using cache_lib.Models.Snapshots;
using livecoin_lib.Client;
using date_time_lib;

namespace livecoin_lib
{
    // https://www.livecoin.net/api/public
    public class LivecoinIntegration : ILivecoinIntegration
    {
        private const string CcxtIntegrationName = "livecoin";
        private const string DatabaseName = "livecoin";

        private static readonly Random _random = new Random();

        private static readonly TimeSpan MarketThreshold = TimeSpan.FromMinutes(20);        
        private static readonly TimeSpan MarketCacheThreshold = TimeSpan.FromMinutes(17.5);

        private static readonly TimeSpan BalanceThreshold = TimeSpan.FromMinutes(20);
        private static readonly TimeSpan UserHistoryThreshold = TimeSpan.FromMinutes(20);

        private static readonly TimeSpan OpenOrdersThreshold = TimeSpan.FromMinutes(20);

        private static readonly TimeSpan CoinInfoThreshold = TimeSpan.FromMinutes(30);
        private static readonly TimeSpan TickerThreshold = TimeSpan.FromMinutes(30);

        private static readonly TimeSpan ThrottleThresh = TimeSpan.FromSeconds(5);        

        private static readonly ThrottleContext ThrottleContext = new ThrottleContext
        {
            Locker = new object(),
            ThrottleThreshold = ThrottleThresh
        };

        private readonly ILivecoinClient _livecoinClient;
        private readonly ITradeNodeUtil _tradeNodeUtil;
        private readonly IWebUtil _webUtil;
        private readonly CacheUtil _cacheUtil;
        private readonly ILogRepo _log;
        private readonly IConfigClient _configClient;

        private readonly IMongoCollection<WebRequestEventContainer> _getCoinInfoCollection;

        private readonly LivecoinMap _livecoinMap = new LivecoinMap();

        public LivecoinIntegration(
            ILivecoinClient livecoinClient,
            ITradeNodeUtil tradeNodeUtil,
            IWebUtil webUtil,
            IConfigClient configClient,
            ILogRepo log)
        {
            _livecoinClient = livecoinClient;
            _tradeNodeUtil = tradeNodeUtil;
            _webUtil = webUtil;
            _configClient = configClient;
            _log = log;

            _cacheUtil = new CacheUtil();

            _getCoinInfoCollection = new MongoCollectionContext(DbContext, "livecoin-get-coin-info")
                .GetCollection<WebRequestEventContainer>();
        }

        public string Name => "Livecoin";
        public Guid Id => new Guid("FEEFA3F0-AF3E-40E1-A0CD-AA8ACF490036");       

        public List<CommodityForExchange> GetCommoditiesOld(CachePolicy cachePolicy)
        {
            var translator = new Func<string, List<CommodityForExchange>>(text =>
            {
                var dict = JsonConvert.DeserializeObject<Dictionary<string, LivecoinCcxtCommodity>>(text);

                var commodities = new List<CommodityForExchange>();

                var walletStatuses = dict.Values.Where(item => item != null && item.info != null).Select(item => item.info.walletStatus).Distinct().ToList();

                foreach (var nativeCommodity in dict.Values.Where(item => item != null && item.info != null))
                {
                    var canon = _livecoinMap.GetCanon(nativeCommodity.info.symbol);

                    var commodity = new CommodityForExchange
                    {
                        CanonicalId = canon?.Id,
                        Symbol = !string.IsNullOrWhiteSpace(canon?.Symbol) ? canon.Symbol : nativeCommodity.info.symbol,
                        NativeSymbol = nativeCommodity.info.symbol,
                        Name = nativeCommodity.info.name,
                        NativeName = !string.IsNullOrWhiteSpace(canon?.Name) ? canon.Name : nativeCommodity.info.name,
                        WithdrawalFee = (decimal)nativeCommodity.info.withdrawFee,
                        CanDeposit = nativeCommodity.active
                            && (new List<string> { "normal", "closed_cashout" }.Any(nativeText => string.Equals(nativeCommodity.info.walletStatus, nativeText, StringComparison.InvariantCultureIgnoreCase))),
                        CanWithdraw = nativeCommodity.active
                            && (new List<string> { "normal", "closed_cashin" }.Any(nativeText => string.Equals(nativeCommodity.info.walletStatus, nativeText, StringComparison.InvariantCultureIgnoreCase)))
                    };

                    commodities.Add(commodity);
                }

                return commodities;
            });

            var retriever = new Func<string>(() => _tradeNodeUtil.FetchCurrencies(CcxtIntegrationName));
            var validator = new Func<string, bool>(text =>
            {
                var translated = translator(text);
                return translated != null && translated.Any();
            });

            var getCommoditiesThreshold = TimeSpan.FromHours(2);
            var context = new MongoCollectionContext(DbContext, "livecoin--fetch-currencies");
            var cacheResult = _cacheUtil.GetCacheableEx(ThrottleContext, retriever, context, getCommoditiesThreshold, cachePolicy, validator);

            if (string.IsNullOrWhiteSpace(cacheResult.Contents))
            {
                throw new ApplicationException("Failed to retrieve Livecoin commodities.");
            }

            return translator(cacheResult?.Contents);
        }

        public List<CommodityForExchange> GetCommodities(CachePolicy cachePolicy)
        {
            var nativeCoinInfoWithAsOf = GetNativeCoinInfo(cachePolicy);

            var depositableStatuses = new List<string> { "normal", "closed_cashout" };
            var withdrawableStatuses = new List<string> { "normal", "closed_cashin" };

            return nativeCoinInfoWithAsOf?.Data?.Info != null
                ? nativeCoinInfoWithAsOf.Data.Info.Select(item => {
                    var nativeSymbol = item.Symbol;
                    var nativeName = item.Name;
                    var canon = _livecoinMap.GetCanon(nativeSymbol);

                    var canDeposit = depositableStatuses.Any(queryStatus => string.Equals(queryStatus, item.WalletStatus, StringComparison.InvariantCultureIgnoreCase));
                    var canWithdraw = withdrawableStatuses.Any(queryStatus => string.Equals(queryStatus, item.WalletStatus, StringComparison.InvariantCultureIgnoreCase));

                    return new CommodityForExchange
                    {
                        CanonicalId = canon?.Id,
                        Symbol = !string.IsNullOrWhiteSpace(canon?.Symbol) ? canon.Symbol : nativeSymbol,
                        NativeSymbol = nativeSymbol,
                        Name = !string.IsNullOrWhiteSpace(canon?.Name) ? canon.Name : nativeName,
                        NativeName = nativeName,
                        CanDeposit = canDeposit,
                        CanWithdraw = canWithdraw
                    };
                }).ToList()
                : new List<CommodityForExchange>();
        }

        private AsOfWrapper<LivecoinCoinInfoResult> GetNativeCoinInfo(CachePolicy cachePolicy)
        {
            var translator = new Func<string, LivecoinCoinInfoResult>(text =>
                !string.IsNullOrWhiteSpace(text)
                    ? JsonConvert.DeserializeObject<LivecoinCoinInfoResult>(text)
                    : null);

            var validator = new Func<string, bool>(text =>
            {
                if (string.IsNullOrWhiteSpace(text)) { throw new ApplicationException("Received an null or whitespace response when requesting coin info from Livecoin."); }
                var translated = translator(text);
                if (!translated.Success) { throw new ApplicationException("Response from livecoin when requesting coin info did not indicate success."); }

                return true;
            });

            var retriever = new Func<string>(() =>
            {
                try
                {
                    var contents = _livecoinClient.GetCoinInfoRaw();
                    if (!validator(contents)) { throw new ApplicationException("Response from livecoin when requesting coin info did not pass validation."); }
                    return contents;
                }
                catch (Exception exception)
                {
                    _log.Error($"Failed to retrieve livecoin coin info.{Environment.NewLine}{exception.Message}");
                    throw;
                }
            });

            var collection = new MongoCollectionContext(DbContext, "livecoin--get-coin-info");
            var cacheResult = _cacheUtil.GetCacheableEx(ThrottleContext, retriever, collection, CoinInfoThreshold, cachePolicy, validator);

            return new AsOfWrapper<LivecoinCoinInfoResult>
            {
                AsOfUtc = cacheResult?.AsOf,
                Data = translator(cacheResult?.Contents)
            };
        }

        private static ManualResetEventSlim _getDepositAddressSlim = new ManualResetEventSlim(true);

        private static TimeSpan DepositAddressThreshold = TimeSpan.FromDays(1);
        public DepositAddress GetDepositAddress(string symbol, CachePolicy cachePolicy)
        {
            var depositAddressWithSymbol = GetDepositAddressWithSymbol(symbol, cachePolicy);
            return depositAddressWithSymbol != null
                ? new DepositAddress
                {
                    Address = depositAddressWithSymbol?.Address,
                    Memo = depositAddressWithSymbol?.Memo
                }
                : null;
        }

        private DepositAddressWithSymbol GetDepositAddressWithSymbol(string symbol, CachePolicy cachePolicy)
        {
            DepositAddress cacheOnlyResult = null;
            if (cachePolicy == CachePolicy.AllowCache)
            {
                cacheOnlyResult = GetDepositAddressWithSymbol(symbol, CachePolicy.OnlyUseCache);
            }

            var toNative = new Func<string, LivecoinDepositAddress>(text => JsonConvert.DeserializeObject<LivecoinDepositAddress>(text));
            var validator = new Func<string, bool>(text =>
            {
                if (string.IsNullOrWhiteSpace(text)) { return false; }
                var translated = toNative(text);

                return translated != null;
            });

            var retriever = new Func<string>(() =>
            {
                var nativeSymbol = _livecoinMap.ToNativeSymbol(symbol);
                try
                {
                    var results = _tradeNodeUtil.GetDepositAddress(CcxtIntegrationName, nativeSymbol);
                    if (!validator(results))
                    {
                        throw new ApplicationException($"Livecoin response failed validation.{Environment.NewLine}{results}");
                    }

                    return results;
                }
                catch(Exception exception)
                {
                    _log.Error(exception);
                    throw;
                }
            });

            var collectionContext = new MongoCollectionContext(DbContext, $"livecoin--get-deposit-address-{symbol}");

            try
            {
                CacheResult cacheResult;
                var gotSlim = _getDepositAddressSlim.Wait(TimeSpan.FromMilliseconds(150));
                if (!gotSlim && cacheOnlyResult != null)
                {
                    return new DepositAddressWithSymbol
                    {
                        Symbol = symbol,
                        Address = cacheOnlyResult?.Address,
                        Memo = cacheOnlyResult?.Memo
                    };
                }
                if (!gotSlim) { _getDepositAddressSlim.Wait(); }

                try
                {
                    cacheResult = _cacheUtil.GetCacheableEx(ThrottleContext, retriever, collectionContext, DepositAddressThreshold, cachePolicy, validator);
                }
                finally
                {
                    _getDepositAddressSlim.Set();
                }

                if (cacheResult == null || string.IsNullOrWhiteSpace(cacheResult.Contents)) { return null; }
                var native = toNative(cacheResult.Contents);

                
                return new DepositAddressWithSymbol
                {
                    Symbol = symbol,
                    Address = native.Address,

                    // what about the memo for addresses that have a memo field?
                    Memo = null
                };               
                 
            }
            catch (Exception exception)
            {
                _log.Error(exception);
                if (cachePolicy == CachePolicy.AllowCache && cacheOnlyResult != null)
                {
                    // return cacheOnlyResult;
                    return new DepositAddressWithSymbol
                    {
                        Symbol = symbol,
                        Address = cacheOnlyResult?.Address,
                        Memo = cacheOnlyResult?.Memo                        
                    };
                }

                throw;
            }
        }

        public List<DepositAddressWithSymbol> GetDepositAddresses(CachePolicy cachePolicy)
        {
            var commodities = GetCommodities(cachePolicy);
            var symbols = commodities.Select(item => item.Symbol).Distinct().ToList();

            var addresses = new List<DepositAddressWithSymbol>();
            foreach (var symbol in symbols)
            {
                var address = GetDepositAddress(symbol, CachePolicy.OnlyUseCache);
                if (address != null)
                {
                    addresses.Add(new DepositAddressWithSymbol
                    {
                        Symbol = symbol,
                        Address = address?.Address,
                        Memo = address?.Memo
                    });
                }
            }

            if (cachePolicy == CachePolicy.AllowCache)
            {
                foreach (var symbol in symbols)
                {
                    if (_getDepositAddressSlim.IsSet)
                    {
                        var task = Task.Run(() => GetDepositAddressWithSymbol(symbol, CachePolicy.AllowCache));
                        if (!task.Wait(TimeSpan.FromSeconds(5)))
                        {
                            break;
                        }

                        var betterAddress = task.Result;
                        var match = addresses.SingleOrDefault(item => string.Equals(item.Symbol, betterAddress.Symbol, StringComparison.InvariantCultureIgnoreCase));
                        if (match != null)
                        {
                            match.Address = betterAddress.Address;
                            match.Memo = betterAddress.Memo;
                        }
                        else
                        {
                            addresses.Add(new DepositAddressWithSymbol
                            {
                                Symbol = symbol,
                                Address = betterAddress?.Address,
                                Memo = betterAddress?.Memo
                            });
                        }
                    }
                }
            }

            return addresses.OrderBy(item => item.Symbol).ToList();
        }

        public void TryToGetDepositAddressesFromTheApi()
        {
            var noxDepositAddress = _tradeNodeUtil.GetDepositAddress(CcxtIntegrationName, "NOX");
            Console.WriteLine(JsonConvert.SerializeObject(noxDepositAddress, Formatting.Indented));
        }

        public HoldingInfo GetHoldings(CachePolicy cachePolicy)
        {
            var native = GetNativeBalance(cachePolicy);
            var currencies = native.Info.Select(item => item.Currency).Distinct().ToList();

            var livecoinHoldings = new List<LivecoinHolding>();
            foreach (var currency in currencies)
            {
                var livecoinHolding = new LivecoinHolding { Currency = currency };
                var matches = native.Info.Where(item => string.Equals(item.Currency, currency)).ToList();
                foreach (var match in matches)
                {
                    var dict = new Dictionary<LivecoinHoldingType, Action<decimal>>
                    {
                        { LivecoinHoldingType.Available, value => livecoinHolding.Available = value },
                        { LivecoinHoldingType.AvailableWithdrawal, value => livecoinHolding.AvailableWithdrawal = value },
                        { LivecoinHoldingType.Total, value => livecoinHolding.Total = value },
                        { LivecoinHoldingType.Trade, value => livecoinHolding.Trade = value }
                    };

                    if (dict.ContainsKey(match.HoldingType)) { dict[match.HoldingType](match.Value); }
                }

                livecoinHoldings.Add(livecoinHolding);
            }

            livecoinHoldings = livecoinHoldings.Where(item => item.Total > 0 || item.Available > 0 || item.AvailableWithdrawal > 0 || item.Trade > 0).ToList();

            return new HoldingInfo
            {
                TimeStampUtc = DateTime.UtcNow,
                Holdings = livecoinHoldings.Select(item =>
                new Holding
                {
                    Symbol = item.Currency,
                    Available = item.Available,
                    Total = item.Total,
                    InOrders = item.Total - item.Available
                }
                ).ToList()
            };
        }
        
        private LivecoinGetBalanceResponse GetNativeBalance(CachePolicy cachePolicy)
        {
            var cacheResult = GetNativeBalanceText(cachePolicy);
            return JsonConvert.DeserializeObject<LivecoinGetBalanceResponse>(cacheResult.Contents);
        }

        private static ManualResetEventSlim GetNativeBalanceSlim = new ManualResetEventSlim();
        private CacheResult GetNativeBalanceText(CachePolicy cachePolicy)
        {
            if (cachePolicy == CachePolicy.AllowCache)
            {
                var gotSlim = GetNativeBalanceSlim.Wait(TimeSpan.FromSeconds(2.5));
                if (!gotSlim)
                {
                    return GetNativeBalanceText(CachePolicy.OnlyUseCache);
                }
            }
            
            try
            {
                var retriever = new Func<string>(() => _tradeNodeUtil.FetchBalance(CcxtIntegrationName));
                var validator = new Func<string, bool>(text => !string.IsNullOrWhiteSpace(text));

                var collectionContext = new MongoCollectionContext(DbContext, "livecoin--get-balance");

                return _cacheUtil.GetCacheableEx(ThrottleContext, retriever, collectionContext, BalanceThreshold, cachePolicy, validator);                
            }
            finally
            {
                GetNativeBalanceSlim.Set();
            }
        }

        public List<HistoricalTrade> GetUserTradeHistory(CachePolicy cachePolicy)
        {
            try
            {
                var historyWithAsOf = GetNativeUserTradeHistoryContents(cachePolicy);
                if (historyWithAsOf?.Data == null) { return new List<HistoricalTrade>(); }

                //[{"datetime":1525989370,"id":486853101,"clientorderid":6322739751,"type":"sell","symbol":"NOX/ETH","price":0.00011029,"quantity":119.84986021,"commission":0.0000238,"bonus":0},
                return historyWithAsOf.Data.Where(item => item != null).Select(item =>
                {
                    var symbol = item.FixedCurrency;
                    var baseSymbol = item.VariableCurrency;

                    var tradingPair = new TradingPair(symbol, baseSymbol);

                    var tradeTypeDictionary = new Dictionary<string, TradeTypeEnum>(StringComparer.InvariantCultureIgnoreCase)
                    {
                        { "BUY", TradeTypeEnum.Buy },
                        { "SELL", TradeTypeEnum.Sell },
                        { "DEPOSIT", TradeTypeEnum.Deposit },
                        { "WITHDRAW", TradeTypeEnum.Withdraw }
                    };

                    var tradeType = tradeTypeDictionary.ContainsKey(item.Type)
                        ? tradeTypeDictionary[item.Type]
                        : TradeTypeEnum.Unknown;

                    var timeStampUtc = DateTimeUtil.UnixTimeStamp13DigitToDateTime(item.Date);

                    var price = item.VariableAmount.HasValue && item.Amount.HasValue 
                            && item.VariableAmount.Value >0 
                            && item.Amount.Value > 0
                        ? (item.VariableAmount.Value / item.Amount.Value)
                        : 0;

                    return new HistoricalTrade
                    {
                        TradingPair = tradingPair,
                        Symbol = symbol,
                        BaseSymbol = baseSymbol,
                        Price = price,
                        Quantity = item.Amount ?? 0,
                        FeeQuantity = item.Fee ?? 0,
                        TradeType = tradeType,
                        TimeStampUtc = timeStampUtc ?? default(DateTime)
                    };
                })
                .ToList();
            }
            catch (Exception exception)
            {
                _log.Error(exception);
                throw;
            }
        }

        private T GetValue<T>(JObject json, string property, T defaultValue)
        {
            var wasSuccessfulProp = (JValue)json[property];
            if (wasSuccessfulProp != null && wasSuccessfulProp.Value != null && wasSuccessfulProp.Value is T)
            {
                return (T)wasSuccessfulProp.Value;
            }

            return defaultValue;
        }

        private bool? GetBool(JObject json, string property)
        {
            return GetValue<bool?>(json, property, (bool?)null);
        }

        private long? GetLong(JObject json, string property)
        {
            return GetValue<long?>(json, property, (long?)null);
        }

        private string GetString(JObject json, string property)
        {
            return GetValue<string>(json, property, (string)null);
        }

        private AsOfWrapper<List<LivecoinHistoryItem>> GetNativeUserTradeHistoryContents(CachePolicy cachePolicy)
        {
            var translator = new Func<string, List<LivecoinHistoryItem>>(text =>
                !string.IsNullOrWhiteSpace(text)
                    ? JsonConvert.DeserializeObject<List<LivecoinHistoryItem>>(text)
                    : null);

            var validator = new Func<string, bool>(text =>
            {
                if (string.IsNullOrWhiteSpace(text)) { throw new ApplicationException("Received a null or whitespace response when requesting livecoin history."); }
                translator(text);
                return true;
            });

            var retriever = new Func<string>(() =>
            {
                try
                {
                    var apiKey = _configClient.GetLivecoinApiKey();
                    var contents = _livecoinClient.GetHistoryRaw(apiKey);
                    if (!validator(contents)) { throw new ApplicationException($"Failed validation on attempting to retrieve livecoin history."); }

                    return contents;
                }
                catch(Exception exception)
                {
                    _log.Error(exception);
                    throw;
                }                
            });

            var collectionContext = new MongoCollectionContext(DbContext, "livecoin-client--get-user-trade-history");
            var cacheResult = _cacheUtil.GetCacheableEx(ThrottleContext, retriever, collectionContext, UserHistoryThreshold, cachePolicy, validator);

            return new AsOfWrapper<List<LivecoinHistoryItem>>
            {
                AsOfUtc = cacheResult?.AsOf,
                Data = translator(cacheResult?.Contents)
            };
        }

        public OrderBook GetOrderBook(TradingPair tradingPair, CachePolicy cachePolicy)
        {
            var nativeSymbol = _livecoinMap.ToNativeSymbol(tradingPair.Symbol);
            var nativeBaseSymbol = _livecoinMap.ToNativeSymbol(tradingPair.BaseSymbol);

            var native = GetNativeOrderBook(nativeSymbol, nativeBaseSymbol, cachePolicy);

            var parseOrder = new Func<List<string>, Order>(nativeOrder =>
                new Order
                {
                    Price = decimal.Parse(nativeOrder[0], NumberStyles.Float),
                    Quantity = decimal.Parse(nativeOrder[1], NumberStyles.Float)
                });

            return native != null
                ? new OrderBook
                {
                    AsOf = native.AsOfUtc,
                    Asks = native?.Data?.Asks?.Select(queryOrder => parseOrder(queryOrder)).ToList() ?? new List<Order>(),
                    Bids = native?.Data?.Bids?.Select(queryOrder => parseOrder(queryOrder)).ToList() ?? new List<Order>()
                }
                : null;
        }

        private AsOfWrapper<LivecoinGetOrderBookResponse> GetNativeOrderBook(string nativeSymbol, string nativeBaseSymbol, CachePolicy cachePolicy)
        {
            var translator = new Func<string, LivecoinGetOrderBookResponse>(text =>
                !string.IsNullOrWhiteSpace(text)
                ? JsonConvert.DeserializeObject<LivecoinGetOrderBookResponse>(text)
                : null);

            var validator = new Func<string, bool>(text =>
            {
                if (string.IsNullOrWhiteSpace(text)) { throw new ApplicationException($"Received a null or whitespace response from {Name} when requesting the order book for {nativeSymbol}-{nativeBaseSymbol}."); }
                var translated = translator(text);
                if (translated.TimeStamp <= 0) { throw new ApplicationException($"Invalid time stamp from Livecoin order book for {nativeSymbol}-{nativeBaseSymbol}."); }

                return true;
            });

            var retriever = new Func<string>(() =>
            {
                try
                {
                    var text = _livecoinClient.GetOrderBook(nativeSymbol, nativeBaseSymbol);

                    if (!validator(text))
                    {
                        throw new ApplicationException($"{Name} get {nativeSymbol}-{nativeBaseSymbol} order book failed validation.");
                    }

                    return text;
                }
                catch (Exception exception)
                {
                    _log.Error($"Failed to retrieve order book for {nativeSymbol}-{nativeBaseSymbol} from {Name}.{Environment.NewLine}{exception.Message}");
                    _log.Error(exception);
                    throw;
                }
            });

            var key = $"{nativeSymbol.ToUpper()}-{nativeBaseSymbol.ToUpper()}";
            var context = GetOrderBookColletionContext();
            var cacheResult = _cacheUtil.GetCacheableEx(ThrottleContext, retriever, context, GetInvidividualOrderBookThreshold, cachePolicy, validator, AfterInsertOrderBook, key);

            return new AsOfWrapper<LivecoinGetOrderBookResponse>
            {
                AsOfUtc = cacheResult?.AsOf,
                Data = translator(cacheResult?.Contents)
            };
        }

        private static TimeSpan GetInvidividualOrderBookThreshold = TimeSpan.FromMinutes(20);
        //private LivecoinOrderBook GetNativeOrderBookOld(string nativeSymbol, string nativeBaseSymbol, CachePolicy cachePolicy)
        //{            
        //    var validator = new Func<string, bool>(text =>
        //    {
        //        if (string.IsNullOrWhiteSpace(text)) { throw new ApplicationException($"Received a null or whitespace response from {Name} when requesting the order book for {nativeSymbol}-{nativeBaseSymbol}."); }
        //        var deserialized = JsonConvert.DeserializeObject<LivecoinOrderBook>(text);
        //        if (deserialized.TimeStamp <= 0) { return false; }

        //        return true;
        //    });

        //    var retriever = new Func<string>(() =>
        //    {
        //        try
        //        {
        //            var text = GetCcxtOrderBookContents(new TradingPair(nativeSymbol, nativeBaseSymbol));
        //            if (!validator(text))
        //            {
        //                throw new ApplicationException($"{Name} get {nativeSymbol}-{nativeBaseSymbol} order book failed validation.");
        //            }

        //            return text;
        //        }
        //        catch (Exception exception)
        //        {
        //            _log.Error($"Failed to retrieve order book for {nativeSymbol}-{nativeBaseSymbol} from {Name}.{Environment.NewLine}{exception.Message}");
        //            _log.Error(exception);
        //            throw;
        //        }
        //    });

        //    var key = $"{nativeSymbol.ToUpper()}-{nativeBaseSymbol.ToUpper()}";
        //    var context = GetOrderBookColletionContext();
        //    var cacheResult = _cacheUtil.GetCacheableEx(ThrottleContext, retriever, context, GetInvidividualOrderBookThreshold, cachePolicy, validator, AfterInsertOrderBook, key);

        //    if (string.IsNullOrWhiteSpace(cacheResult.Contents))
        //    {
        //        throw new ApplicationException($"Failed to retrieve order book for trading pair {nativeSymbol.ToUpper()}-{nativeBaseSymbol.ToUpper()}.");
        //    }

        //    var native = JsonConvert.DeserializeObject<LivecoinOrderBook>(cacheResult.Contents);
        //    if(native != null)
        //    {
        //        native.AsOf = cacheResult?.AsOf;
        //    }

        //    return native;
        //}

        private void AfterInsertOrderBook(CacheEventContainer container)
        {
            var itemContext = GetOrderBookColletionContext();
            var snapShotContext = GetOrderBookSnapshotCollectionContext();
            var snapShot = snapShotContext
                .GetLast<AllOrdersSnapshot>();

            List<CacheEventContainer> itemsToApply = null;
            if (snapShot != null)
            {
                itemsToApply =
                    itemContext.GetCollection<CacheEventContainer>()
                    .AsQueryable()
                    .Where(item => item.Id > snapShot.LastId)
                    .OrderBy(item => item.Id)
                    .ToList();
            }
            else
            {
                itemsToApply =
                    itemContext.GetCollection<CacheEventContainer>()
                    .AsQueryable()
                    .OrderBy(item => item.Id)
                    .ToList();
            }

            if (snapShot == null) { snapShot = new AllOrdersSnapshot(); }

            if (itemsToApply == null || !itemsToApply.Any()) { return; }

            foreach (var item in itemsToApply)
            {
                snapShot.ApplyEvent(item);
            }

            snapShot.Id = default(ObjectId);
            snapShotContext.Insert(snapShot);

            var collection = snapShotContext.GetCollection<BsonDocument>();
            var filter = Builders<BsonDocument>.Filter.Lt("_id", snapShot.Id);

            collection.DeleteMany(filter);
        }

        private IMongoCollectionContext GetOrderBookColletionContext() => new MongoCollectionContext(DbContext, $"livecoin--get-order-book");
        private IMongoCollectionContext GetOrderBookSnapshotCollectionContext() => new MongoCollectionContext(DbContext, $"livecoin--order-book-snapshot");

        //private string GetOrderBookContents(string nativeSymbol, string nativeBaseSymbol)
        //{
        //    var nativeTradingPair = new TradingPair(nativeSymbol, nativeBaseSymbol);
        //    var currencyPair = $"{nativeTradingPair.Symbol}/{nativeTradingPair.BaseSymbol}";
        //    var url = $"https://api.livecoin.net/exchange/order_book?currencyPair={currencyPair}";

        //    var contents = _webUtil.Get(url);
        //    return contents;
        //}

        //public string GetCcxtOrderBookContents(TradingPair tradingPair)
        //{
        //    return _tradeNodeUtil.FetchOrderBook(CcxtIntegrationName, tradingPair);
        //}

        public List<TradingPair> GetTradingPairs(CachePolicy cachePolicy)
        {
            const decimal PriceTick = 0.00000001m;
            const decimal LotSize = 0.00000001m;

            var tickerWithAsOf = GetNativeTicker(cachePolicy);
            var coinInfoWithAsOf = GetNativeCoinInfo(cachePolicy);

            return tickerWithAsOf?.Data.Select(item =>
            {
                var combo = item.Symbol;
                var pieces = combo.Split('/');
                if (pieces.Length != 2) { return null; }
                var nativeSymbol = pieces[0].Trim();
                var nativeBaseSymbol = pieces[1].Trim();

                var canon = _livecoinMap.GetCanon(nativeSymbol);
                var baseCanon = _livecoinMap.GetCanon(nativeBaseSymbol);

                var matchingNativeCoin = (coinInfoWithAsOf?.Data?.Info ?? new List<LivecoinCoinInfoItem>())
                    .SingleOrDefault(queryCoinInfoItem =>
                        string.Equals(nativeSymbol, queryCoinInfoItem.Symbol, StringComparison.InvariantCultureIgnoreCase)
                    );

                var matchingNativeBaseCoin = (coinInfoWithAsOf?.Data?.Info ?? new List<LivecoinCoinInfoItem>())
                    .SingleOrDefault(queryCoinInfoItem =>
                        string.Equals(nativeBaseSymbol, queryCoinInfoItem.Symbol, StringComparison.InvariantCultureIgnoreCase)
                    );

                var nativeCommodityName = !string.IsNullOrWhiteSpace(matchingNativeCoin?.Name)
                    ? matchingNativeCoin.Name
                    : nativeSymbol;

                var commodityName = !string.IsNullOrWhiteSpace(canon?.Name)
                    ? canon.Name
                    : nativeCommodityName;

                var nativeBaseCommodtyName = !string.IsNullOrWhiteSpace(matchingNativeBaseCoin?.Name)
                    ? matchingNativeBaseCoin.Name
                    : nativeBaseSymbol;

                var baseCommodityName = !string.IsNullOrWhiteSpace(baseCanon?.Name)
                    ? baseCanon.Name
                    : nativeBaseCommodtyName;

                return new TradingPair
                {
                    CanonicalCommodityId = canon?.Id,
                    Symbol = !string.IsNullOrWhiteSpace(canon?.Symbol) ? canon.Symbol : nativeSymbol,
                    NativeSymbol = nativeSymbol,
                    CommodityName = commodityName,
                    NativeCommodityName = nativeCommodityName,
                    
                    CanonicalBaseCommodityId = baseCanon?.Id,
                    BaseSymbol = !string.IsNullOrWhiteSpace(baseCanon?.Symbol) ? baseCanon.Symbol : nativeBaseSymbol,
                    NativeBaseSymbol = nativeBaseSymbol,
                    BaseCommodityName = baseCommodityName,
                    NativeBaseCommodityName = nativeBaseCommodtyName,

                    PriceTick = PriceTick,
                    LotSize = LotSize
                };
            })
            .Where(item => item != null)
            .ToList();
        }

        private AsOfWrapper<List<LivecoinTickerItem>> GetNativeTicker(CachePolicy cachePolicy)
        {
            var translator = new Func<string, List<LivecoinTickerItem>>(text =>
                !string.IsNullOrWhiteSpace(text)
                ? JsonConvert.DeserializeObject<List<LivecoinTickerItem>>(text)
                : null);

            var validator = new Func<string, bool>(text =>
            {
                if (string.IsNullOrWhiteSpace(text)) { throw new ApplicationException("Livecoin returned a null or whitespace response when requesting ticker."); }
                translator(text);

                return true;
            });

            var retriever = new Func<string>(() =>
            {
                try
                {
                    var contents = _livecoinClient.GetTickerRaw();
                    if (!validator(contents))
                    {
                        throw new ApplicationException("Livecoin's response when attempting to get ticker failed validation.");
                    }

                    return contents;
                }
                catch (Exception exception)
                {
                    _log.Error($"Failed to get livecoin ticker.{Environment.NewLine}{exception.Message}");
                    _log.Error(exception);
                    throw;
                }
            });

            var collectionContext = new MongoCollectionContext(DbContext, "livecoin--get-ticker");

            var cacheResults = _cacheUtil.GetCacheableEx(ThrottleContext, retriever, collectionContext, TickerThreshold, cachePolicy, validator);
            return new AsOfWrapper<List<LivecoinTickerItem>>
            {
                AsOfUtc = cacheResults.AsOf,
                Data = translator(cacheResults.Contents)
            };
        }

        public List<TradingPair> GetTradingPairsOld(CachePolicy cachePolicy)
        {
            var cacheResult = GetAllOrderBooksContents(cachePolicy);            

            var json = (JObject)JsonConvert.DeserializeObject(cacheResult.Contents);
            var tradingPairs = new List<TradingPair>();
            foreach (var kid in json.Children())
            {
                var pairName = ((JProperty)kid).Name;
                var pieces = pairName.Split('/');
                var nativeSymbol = pieces[0];
                var nativeBaseSymbol = pieces[1];

                var canon = _livecoinMap.GetCanon(nativeSymbol);
                var baseCanon = _livecoinMap.GetCanon(nativeBaseSymbol);

                var tradingPair = new TradingPair
                {
                    CanonicalCommodityId = canon?.Id,
                    Symbol = !string.IsNullOrWhiteSpace(canon?.Symbol) ? canon.Symbol : nativeSymbol,
                    CommodityName = !string.IsNullOrWhiteSpace(canon?.Name) ? canon.Name : nativeSymbol,
                    NativeSymbol = nativeSymbol,
                    NativeCommodityName = nativeSymbol,

                    CanonicalBaseCommodityId = baseCanon?.Id,
                    NativeBaseSymbol = nativeBaseSymbol,
                    NativeBaseCommodityName = nativeBaseSymbol,
                    BaseSymbol = !string.IsNullOrWhiteSpace(baseCanon?.Symbol) ? baseCanon.Symbol : nativeBaseSymbol,
                    BaseCommodityName = !string.IsNullOrWhiteSpace(baseCanon?.Name) ? baseCanon.Name : nativeBaseSymbol                    
                };

                // var tradingPair = new TradingPair(FromNativeSymbol(nativeSymbol), FromNativeSymbol(nativeBaseSymbol));
                tradingPairs.Add(tradingPair);
            }

            return tradingPairs;
        }

        private static object GetAllOrderBooksContentsLocker = new object();
        private CacheResult GetAllOrderBooksContents(CachePolicy cachePolicy)
        {
            lock (GetAllOrderBooksContentsLocker)
            {
                const string Url = "https://api.livecoin.net/exchange/all/order_book";

                var retriever = new Func<string>(() => _webUtil.Get(Url));
                var validator = new Func<string, bool>(text => !string.IsNullOrWhiteSpace(text));

                var collectionContext = new MongoCollectionContext(DbContext, "livecoin--get-all-order-books");

                return _cacheUtil.GetCacheableEx(ThrottleContext, retriever, collectionContext, MarketThreshold, cachePolicy, validator);
            }
        }

        public decimal? GetWithdrawalFee(string symbol, CachePolicy cachePolicy)
        {
            var fees = GetWithdrawalFees(cachePolicy);
            return fees.ContainsKey(symbol) ? fees[symbol] : (decimal?)null;
        }

        //private LivecoinCoinInfoResult GetNativeCoinInfo(CachePolicy cachePolicy)
        //{
        //    const string Url = "https://api.livecoin.net/info/coinInfo";
        //    var contents = GetCacheableWebRequest(_getCoinInfoCollection, cachePolicy, new WebRequestContext { Url = Url, Verb = "GET" });

        //    var native = JsonConvert.DeserializeObject<LivecoinCoinInfoResult>(contents);

        //    return native;
        //}

        public Dictionary<string, decimal> GetWithdrawalFees(CachePolicy cachePolicy)
        {
            var native = GetNativeCoinInfo(cachePolicy);

            var dict = new Dictionary<string, decimal>();
            foreach (var info in native.Data.Info)
            {
                if (info.WithdrawFee.HasValue)
                {
                    var canonicalSymbol = _livecoinMap.ToCanonicalSymbol(info.Symbol);
                    dict[canonicalSymbol] = info.WithdrawFee.Value;
                }
            }

            return dict;
        }

        public void SetDepositAddress(DepositAddress depositAddress)
        {
            throw new NotImplementedException();
        }

        private List<KeyValuePair<string, string>> GetHeaders(string publicKey, string signature)
        {
            var apiKey = _configClient.GetLivecoinApiKey();

            var headers = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("API-Key", publicKey),
                new KeyValuePair<string, string>("API-Sign", signature)
            };

            return headers;
        }

        private static TimeSpan CacheThreshold = TimeSpan.FromMinutes(10);
        private string GetCacheableWebRequest(
            IMongoCollection<WebRequestEventContainer> collection,
            CachePolicy cachePolicy,
            WebRequestContext context,            
            List<KeyValuePair<string, string>> headers = null)
        {
            if (cachePolicy != CachePolicy.ForceRefresh)
            {
                var mostRecent = collection.AsQueryable()
                    .OrderByDescending(item => item.Id)
                    .Where(item => item.Context.SearchableContext == context.SearchableContext)
                    .FirstOrDefault();

                var currentTime = DateTime.UtcNow;
                if (mostRecent != null
                    && (currentTime - mostRecent.StartTimeUtc) < CacheThreshold
                    && !string.IsNullOrWhiteSpace(mostRecent.Raw))
                {
                    return mostRecent.Raw;
                }
            }

            var (startTime, contents, endTime) = string.Equals(context.Verb, "POST")
                ? HttpPost(context.Url, context.Payload, "application/x-www-form-urlencoded", headers)
                : HttpGet(context.Url);

            var ec = new WebRequestEventContainer
            {
                StartTimeUtc = startTime,
                EndTimeUtc = endTime,
                Raw = contents,
                Context = context
            };

            collection.InsertOne(ec);

            return contents;
        }

        private (DateTime startTime, string contents, DateTime endTime) HttpPost(string url, string data = null, string contentType = null, List<KeyValuePair<string, string>> headers = null)
        {
            return Throttle(() =>
            {
                var startTime = DateTime.UtcNow;

                var contents = ManualPost(url, data, contentType, headers);
                //_webUtil.Post(url, data, headers);
                var endTime = DateTime.UtcNow;

                return (startTime, contents, endTime);
            });
        }

        private (DateTime startTime, string contents, DateTime endTime) HttpGet(string url)
        {
            return Throttle(() =>
            {
                var startTime = DateTime.UtcNow;
                var contents = _webUtil.Get(url);
                var endTime = DateTime.UtcNow;

                return (startTime, contents, endTime);
            });
        }

        private static object Locker = new object();
        private static DateTime? LastReadTime;
        private static TimeSpan ThrottleThreshold = TimeSpan.FromMilliseconds(1500);

        private static T Throttle<T>(Func<T> getter)
        {
            lock (Locker)
            {
                if (LastReadTime.HasValue)
                {
                    var remainigTime = ThrottleThreshold - (DateTime.UtcNow - LastReadTime.Value);
                    if (remainigTime > TimeSpan.Zero)
                    {
                        Thread.Sleep(remainigTime);
                    }
                }

                LastReadTime = DateTime.UtcNow;
                return getter();
            }
        }

        private string ManualPost(string url, string payload, string contentType, List<KeyValuePair<string, string>> headers)
        {
            var webRequest = (HttpWebRequest)WebRequest.Create(url);
            webRequest.Method = "POST";

            if (!string.IsNullOrWhiteSpace(contentType))
            {
                webRequest.ContentType = contentType;
            }

            foreach (var header in headers ?? new List<KeyValuePair<string, string>>())
            {
                webRequest.Headers.Add(header.Key, header.Value);
            }

            if (!string.IsNullOrWhiteSpace(payload))
            {
                using (var writer = new StreamWriter(webRequest.GetRequestStream()))
                {
                    writer.Write(payload);
                }
            }

            return Throttle(() =>
            {
                using (var webResponse = webRequest.GetResponse())
                {
                    using (var stream = webResponse.GetResponseStream())
                    {
                        using (var streamReader = new StreamReader(stream))
                        {
                            return streamReader.ReadToEnd();
                        }
                    }
                }
            });
        }

        private static string HashHMAC(string key, string message)
        {
            var encoding = new UTF8Encoding();
            byte[] keyByte = encoding.GetBytes(key);

            var hmacsha256 = new HMACSHA256(keyByte);

            byte[] messageBytes = encoding.GetBytes(message);
            byte[] hashmessage = hmacsha256.ComputeHash(messageBytes);

            return ByteArrayToString(hashmessage);
        }

        private static string ByteArrayToString(byte[] ba)
        {
            var hex = new StringBuilder(ba.Length * 2);
            foreach (byte b in ba) { hex.AppendFormat("{0:x2}", b); }

            return hex.ToString();
        }

        private static string http_build_query(String formdata)
        {
            string str = formdata.Replace("/", "%2F");
            str = str.Replace("@", "%40");
            str = str.Replace(";", "%3B");
            return str;
        }

        public bool BuyMarket(TradingPair tradingPair, decimal quantity)
        {
            throw new NotImplementedException();
        }

        public bool SellMarket(TradingPair tradingPair, decimal quantity)
        {
            throw new NotImplementedException();
        }

        public bool Withdraw(Commodity commodity, decimal quantity, DepositAddress address)
        {
            var apiKey = _configClient.GetLivecoinApiKey();

            const string Url = "https://api.livecoin.net/payment/out/coin";
            var nativeSymbol = _livecoinMap.ToNativeSymbol(commodity.Symbol);

            var queryText = $"amount={quantity}&currency={nativeSymbol}&wallet={address.Address}";

            var param = http_build_query(queryText);
            var signature = HashHMAC(apiKey.Secret, param).ToUpper();
            var bytes = Encoding.UTF8.GetBytes(param);

            var request = (HttpWebRequest)WebRequest.Create(Url);
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = bytes.Length;
            request.Headers["Api-Key"] = apiKey.Key;
            request.Headers["Sign"] = signature;

            string responseText = null;
            using (var requestStream = request.GetRequestStream())
            {
                requestStream.Write(bytes, 0, bytes.Length);
                
                using (var response = request.GetResponse())
                using (var dataStream = response.GetResponseStream())
                using (var streamReader = new StreamReader(dataStream))
                {
                    responseText = streamReader.ReadToEnd();
                    dataStream.Close();
                }
            }

            if (string.IsNullOrWhiteSpace(responseText))
            {
                throw new ApplicationException("Livecoin returned a null or empty response.");
            }

            Console.WriteLine(responseText);

            _log.Info(responseText);
            // var parsedResponse = JsonConvert.DeserializeObject<LivecoinWithdrawalResponse>(responseText);
            
            return true;
        }

        public List<OrderBookAndTradingPair> GetCachedOrderBooks()
        {
            var context = GetOrderBookSnapshotCollectionContext();
            var collection = context.GetCollection<AllOrdersSnapshot>();

            var snapshot = context.GetLast<AllOrdersSnapshot>();
            if (snapshot?.SnapshotItems == null) { return new List<OrderBookAndTradingPair>(); }

            var orders = new List<OrderBookAndTradingPair>();
            foreach (var key in snapshot.SnapshotItems.Keys)
            {
                var cachedOrderBook = snapshot.SnapshotItems[key];
                var orderBookAndTradingPair = new OrderBookAndTradingPair
                {
                    Symbol = cachedOrderBook.Symbol,
                    BaseSymbol = cachedOrderBook.BaseSymbol,
                };

                var native = JsonConvert.DeserializeObject<LivecoinOrderBook>(cachedOrderBook.Raw);
                if (native == null) { return null; }
                var asks = native.Asks != null ? native.Asks.Select(item => new Order { Price = decimal.Parse(item[0], NumberStyles.Float), Quantity = decimal.Parse(item[1], NumberStyles.Float) }).ToList() : new List<Order>();
                var bids = native.Bids != null ? native.Bids.Select(item => new Order { Price = decimal.Parse(item[0], NumberStyles.Float), Quantity = decimal.Parse(item[1], NumberStyles.Float) }).ToList() : new List<Order>();

                orderBookAndTradingPair.Asks = asks;
                orderBookAndTradingPair.Bids = bids;
                orderBookAndTradingPair.AsOf = DateTimeUtil.UnixTimeStampToDateTime(native.TimeStamp / 1000.0m);

                orders.Add(orderBookAndTradingPair);
            }

            return orders;
        }

        public string NativeClientTest()
        {
            return _livecoinClient.GetOrderBook("ETH", "BTC");
        }

        public bool BuyLimit(TradingPair tradingPair, decimal quantity, decimal price)
        {
            var responseContents = _tradeNodeUtil.BuyLimit(CcxtIntegrationName, tradingPair, quantity, price);
            _log.Info($"Response from livecoin from placing buy limit order for {quantity} {tradingPair.Symbol} at {price} {tradingPair.BaseSymbol}.{Environment.NewLine}{responseContents}");

            try
            {
                var response = JsonConvert.DeserializeObject<LivecoinCcxtPlaceLimitOrdersResponse>(responseContents);
                if (response?.Info == null || !response.Info.Success)
                {
                    _log.Error($"Response from livecoin from placing buy limit order for {quantity} {tradingPair.Symbol} at {price} {tradingPair.BaseSymbol} did not indicate success.{Environment.NewLine}{responseContents}");
                    return false;
                }

                return true;
            }
            catch (Exception exception)
            {
                _log.Error($"Encountered an exception from livecoin from placing buy limit order for {quantity} {tradingPair.Symbol} at {price} {tradingPair.BaseSymbol}.{Environment.NewLine}{exception.Message}");
                _log.Error(exception);

                return false;
            }
        }

        public List<OpenOrderForTradingPair> GetOpenOrders(CachePolicy cachePolicy)
        {
            var orderTypeDictionary = new Dictionary<string, OrderType>(StringComparer.InvariantCultureIgnoreCase)
            {
                { "BUY", OrderType.Bid },
                { "SELL", OrderType.Ask }
            };

            var translator = new Func<string, List<OpenOrderForTradingPair>>(text =>
            {
                var response = !string.IsNullOrWhiteSpace(text)
                    ? JsonConvert.DeserializeObject<List<LivecoinCcxtOpenOrder>>(text)
                    : null;

                if (response == null) { return null; }

                return response.Select(item =>
                {
                    if (!orderTypeDictionary.ContainsKey(item.Side))
                    {
                        throw new ApplicationException($"Unexpected order type \"{item.Side}\"");
                    }
                    var orderType = orderTypeDictionary[item.Side];

                    var pieces = item.Symbol.Split('/');
                    var nativeSymbol = pieces[0];
                    var nativeBaseSymbol = pieces[1];

                    var symbol = _livecoinMap.ToCanonicalSymbol(nativeSymbol);
                    var baseSymbol = _livecoinMap.ToCanonicalSymbol(nativeBaseSymbol);

                    var orderIdCombo = new LivecoinOrderIdCombo
                    {
                        Id = item.Id.ToString(),
                        NativeSymbol = nativeSymbol,
                        NativeBaseSymbol = nativeBaseSymbol
                    };

                    var orderIdComboText = JsonConvert.SerializeObject(orderIdCombo);

                    return new OpenOrderForTradingPair
                    {
                        OrderId = orderIdComboText,
                        Price = item.Price,
                        Quantity = item.Amount,
                        Symbol = symbol,
                        BaseSymbol = baseSymbol,
                        OrderType = orderType
                    };
                }).ToList();
            });

            var validator = new Func<string, bool>(text =>
            {
                if (string.IsNullOrWhiteSpace(text)) { throw new ApplicationException($"Received a null or whitespace from {Name} when requesting open orders."); }
                translator(text);
                return true;
            });

            var retriever = new Func<string>(() =>
            {
                try
                {
                    var contents = _tradeNodeUtil.GetNativeOpenOrders(CcxtIntegrationName);
                    return contents;
                }
                catch (Exception exception)
                {
                    _log.Error(exception);
                    throw;
                }
            });

            var collection = new MongoCollectionContext(DbContext, "livecoin--get-open-orders");
            var cacheResult = _cacheUtil.GetCacheableEx(ThrottleContext, retriever, collection, OpenOrdersThreshold, cachePolicy, validator);

            return translator(cacheResult?.Contents);
        }

        public void CancelOrder(string orderId)
        {
            var apiKey = _configClient.GetLivecoinApiKey();
            var combo = JsonConvert.DeserializeObject<LivecoinOrderIdCombo>(orderId);
            var nativeSymbol = combo.NativeSymbol;
            var nativeBaseSymbol = combo.NativeBaseSymbol;

            var responseContents = _livecoinClient.CancelOrderRaw(apiKey, nativeSymbol, nativeBaseSymbol, combo.Id);
            if (string.IsNullOrWhiteSpace(responseContents))
            {
                throw new ApplicationException($"Received a null or whitespace response from Livecoin when attempting to cancel order order {combo.Id} on {nativeSymbol}-{nativeBaseSymbol}.");
            }

            var response = JsonConvert.DeserializeObject<LivecoinCancelOrderResponse>(responseContents);

            if (!response.Success)
            {
                throw new ApplicationException($"Livecoin's response to cancelling order {combo.Id} on {nativeSymbol}-{nativeBaseSymbol} did not indicate success.{Environment.NewLine}{responseContents}");
            }
        }

        private IMongoDatabaseContext DbContext
        {
            get { return new MongoDatabaseContext(_configClient.GetConnectionString(), DatabaseName); }
        }
    }
}
