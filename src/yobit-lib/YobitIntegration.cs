using config_connection_string_lib;
using kucoin_lib;
using kucoin_lib.Res;
using log_lib;
using mongo_lib;
using MongoDB.Driver;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using res_util_lib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using trade_lib;
using cache_lib.Models;
using trade_model;
using trade_res;
using web_util;
using yobit_lib.Models;
using cache_lib;
using yobit_lib.Client;

namespace yobit_lib
{
    // https://yobit.net/en/api/
    public class YobitIntegration : IYobitIntegration
    {
        private static ThrottleContext ThrottleContext = new ThrottleContext
        {
            Locker = new object(),
            ThrottleThreshold = TimeSpan.FromSeconds(1.25)
        };

        private static TimeSpan OrderBookThreshold = TimeSpan.FromMinutes(15);
        private static TimeSpan YobitInfoThreshold = TimeSpan.FromMinutes(30);

        private const string DatabaseName = "yobit";

        public string Name => "Yobit";
        public Guid Id => new Guid("C77F9631-DD55-4853-A7D0-7F9396736193");

        private readonly IYobitClient _yobitClient;
        private readonly IWebUtil _webUtil;
        private readonly IMongoCollection<WebRequestEventContainer> _infoCollection;
        private readonly ILogRepo _log;

        private readonly IGetConnectionString _getConnectionString;
        private readonly CacheUtil _cacheUtil;

        private readonly YobitMap _yobitMap = new YobitMap();

        public YobitIntegration(
            IYobitClient yobitClient,
            IGetConnectionString getConnectionString,
            IWebUtil webUtil,
            ILogRepo log)
        {
            _yobitClient = yobitClient;
            _getConnectionString = getConnectionString;
            _webUtil = webUtil;

            _cacheUtil = new CacheUtil();

            _infoCollection = new MongoCollectionContext(DbContext, "yobit-info").GetCollection<WebRequestEventContainer>();
            _log = log;
        }

        public List<CommodityForExchange> GetCommodities(CachePolicy cachePolicy)
        {
            var nativeCoins = GetNativeCoins();
            return nativeCoins.Values.Select(item =>
            {
                var nativeSymbol = item.Symbol;
                var nativeName = item.FullName;
                var canon = _yobitMap.GetCanon(item.Symbol);
                var name = !string.IsNullOrWhiteSpace(canon?.Name) ? canon.Name : nativeName;
                var symbol = !string.IsNullOrWhiteSpace(canon?.Symbol) ? canon.Symbol : nativeSymbol;

                return new CommodityForExchange
                {
                    CanonicalId = canon?.Id,
                    Symbol = symbol,
                    NativeSymbol = nativeSymbol,
                    Name = name,
                    NativeName = item.FullName
                };
            }).ToList();
        }

        private static Dictionary<string, YobitCoin> _nativeCoins = null;
        private static object NativeCoinLocker = new object();

        public Dictionary<string, YobitCoin> GetNativeCoins()
        {
            if (_nativeCoins != null) { return _nativeCoins; }
            lock (NativeCoinLocker)
            {
                if (_nativeCoins != null) { return _nativeCoins; }
                
                var nativeCoinList = ResUtil.Get<List<YobitCoin>>("yobit-coins.json", GetType().Assembly);
                // _nativeCoins = new Dictionary<string, YobitCoin>(StringComparer.InvariantCultureIgnoreCase);
                _nativeCoins = new Dictionary<string, YobitCoin>();
                foreach (var nativeCoin in nativeCoinList)
                {
                    _nativeCoins[nativeCoin.Symbol] = nativeCoin;
                }
            }

            return _nativeCoins;
        }

        public YobitCoin GetNativeCoin(string symbol)
        {
            if (string.IsNullOrWhiteSpace(symbol)) { throw new ArgumentNullException(nameof(symbol)); }

            var effectiveSymbol = symbol.Trim().ToUpper();

            var nativeCoins = GetNativeCoins();
            return nativeCoins.ContainsKey(effectiveSymbol) ? nativeCoins[effectiveSymbol] : null;
        }

        public DepositAddress GetDepositAddress(string symbol, CachePolicy cachePolicy)
        {
            return null;
        }

        public List<DepositAddressWithSymbol> GetDepositAddresses(CachePolicy cachePolicy)
        {
            return null;
        }

        public HoldingInfo GetHoldings(CachePolicy cachePolicy)
        {
            return null;
        }

        public List<HistoricalTrade> GetUserTradeHistory()
        {
            return null;
        }

        public OrderBook GetOrderBook(TradingPair tradingPair, CachePolicy cachePolicy)
        {
            var nativeSymbol = _yobitMap.ToNativeSymbol(tradingPair.Symbol);
            var nativeBaseSymbol = _yobitMap.ToNativeSymbol(tradingPair.BaseSymbol);

            var wrappedNative = GetNativeOrderBook(nativeSymbol, nativeBaseSymbol, cachePolicy);
            var native = wrappedNative?.Data;

            var orderBook = new OrderBook
            {
                Asks = native?.Asks?.Where(item => item.Count == 2).Select(item => new Order { Price = item[0], Quantity = item[1] }).ToList() ?? new List<Order>(),
                Bids = native?.Bids?.Where(item => item.Count == 2).Select(item => new Order { Price = item[0], Quantity = item[1] }).ToList() ?? new List<Order>(),
                AsOf = wrappedNative.AsOfUtc
            };

            return orderBook;
        }

        private AsOfWrapper<YobitOrderBook> GetNativeOrderBook(string nativeSymbol, string nativeBaseSymbol, CachePolicy cachePolicy)
        {
            if (string.IsNullOrWhiteSpace(nativeSymbol)) { throw new ArgumentNullException(nameof(nativeSymbol)); }
            if (string.IsNullOrWhiteSpace(nativeBaseSymbol)) { throw new ArgumentNullException(nameof(nativeBaseSymbol)); }

            var effectiveNativeSymbol = nativeSymbol.Trim().ToUpper();
            var effectiveNativeBaseSymbol = nativeBaseSymbol.Trim().ToUpper();
            
            var tradingPairForUrl = $"{effectiveNativeSymbol.ToLower()}_{effectiveNativeBaseSymbol.ToLower()}";
            var collectionContext = new MongoCollectionContext(DbContext, $"yobit--get-order-book--{effectiveNativeSymbol}-{effectiveNativeBaseSymbol}");

            var url = $"https://yobit.net/api/2/{tradingPairForUrl}/depth";

            var translator = new Func<string, YobitOrderBook>(text => !string.IsNullOrWhiteSpace(text) ? JsonConvert.DeserializeObject<YobitOrderBook>(text) : null);
            var validator = new Func<string, bool>(text => !string.IsNullOrWhiteSpace(text) && translator(text) != null);
            var retriever = new Func<string>(() =>
            {
                try
                {
                    var text = _webUtil.Get(url);
                    if (!validator(text)) { throw new ApplicationException($"Validator failed when trying to retrieve yobit order book for {effectiveNativeSymbol}-{effectiveNativeBaseSymbol}"); }
                    return text;
                }
                catch (Exception exception)
                {
                    _log.Error($"Failed to retrieve Yobit order book for {nativeSymbol}-{nativeBaseSymbol}.");
                    _log.Error(exception);
                    throw;
                }
            });

            var cacheResult = _cacheUtil.GetCacheableEx(ThrottleContext, retriever, collectionContext, OrderBookThreshold, cachePolicy, validator);
            return new AsOfWrapper<YobitOrderBook>
            {
                AsOfUtc = cacheResult?.AsOf,
                Data = translator(cacheResult?.Contents)
            };
        }

        public List<TradingPair> GetTradingPairs(CachePolicy cachePolicy)
        {
            var nativeCoins = GetNativeCoins();

            var nativeInfo = GetNativeInfo(cachePolicy);

            var tradingPairs = new List<TradingPair>();
            foreach (var key in nativeInfo.Pairs.Keys.ToList())
            {
                var pieces = key.Split('_');
                if (pieces.Length != 2) { continue; }
                var nativeSymbol = pieces[0];
                var nativeBaseSymbol = pieces[1];

                var item = nativeInfo.Pairs[key];                

                var nativeCoin = nativeCoins.ContainsKey(nativeSymbol) ? nativeCoins[nativeSymbol] : null;
                var nativeBaseCoin = nativeCoins.ContainsKey(nativeBaseSymbol) ? nativeCoins[nativeBaseSymbol] : null;

                var canon = _yobitMap.GetCanon(nativeSymbol);
                var baseCanon = _yobitMap.GetCanon(nativeBaseSymbol);

                var tradingPair = new TradingPair
                {
                    CanonicalCommodityId = canon?.Id,
                    CommodityName = !string.IsNullOrWhiteSpace(canon?.Name)
                            ? canon.Name
                            : !string.IsNullOrWhiteSpace(nativeCoin?.FullName) ? nativeCoin.FullName : nativeSymbol.ToUpper(),
                    Symbol = !string.IsNullOrWhiteSpace(canon?.Symbol) ? canon.Symbol : nativeSymbol.ToUpper(),
                    NativeSymbol = nativeSymbol.ToUpper(),

                    CanonicalBaseCommodityId = baseCanon?.Id,
                    BaseCommodityName = !string.IsNullOrWhiteSpace(baseCanon?.Name)
                            ? baseCanon.Name
                            : !string.IsNullOrWhiteSpace(nativeBaseCoin?.FullName) ? nativeBaseCoin.FullName : nativeBaseSymbol.ToUpper(),
                    BaseSymbol = !string.IsNullOrWhiteSpace(baseCanon?.Symbol) ? baseCanon.Symbol : nativeBaseSymbol.ToUpper(),
                    NativeBaseSymbol = nativeBaseSymbol.ToUpper()
                };

                tradingPairs.Add(tradingPair);
            }

            return tradingPairs;
        }

        // https://yobit.net/en/coinsinfo/
        public decimal? GetWithdrawalFee(string symbol, CachePolicy cachePolicy)
        {
            var fees = GetWithdrawalFees(cachePolicy);
            return fees.ContainsKey(symbol) ? fees[symbol] : (decimal?)null;
        }

        public Dictionary<string, decimal> GetWithdrawalFees(CachePolicy cachePolicy)
        {
            return new Dictionary<string, decimal>
            {
                { "CAG", 1 }
            };
        }

        public bool BuyMarket(TradingPair tradingPair, decimal quantity)
        {
            throw new NotImplementedException();
        }

        public bool SellMarket(TradingPair tradingPair, decimal quantity)
        {
            throw new NotImplementedException();
        }

        public void SetDepositAddress(DepositAddress depositAddress)
        {
            throw new NotImplementedException();
        }

        public bool Withdraw(string symbol, decimal quantity, string address)
        {
            throw new NotImplementedException();
        }

        private YobitInfo GetNativeInfo(CachePolicy cachePolicy)
        {            
            var translator = new Func<string, YobitInfo>(text =>
            {
                return !string.IsNullOrWhiteSpace(text) 
                    ? JsonConvert.DeserializeObject<YobitInfo>(text)
                    : null;
            });

            var validator = new Func<string, bool>(text =>
            {
                if (string.IsNullOrWhiteSpace(text)) { throw new ApplicationException("Received null or whitespace response when requesting native info from Yobit."); }
                return true;
            });

            const string Url = "https://yobit.net/api/3/info";
            var retriever = new Func<string>(() =>
            {
                try
                {
                    var text = _webUtil.Get(Url);
                    if (!validator(text))
                    {
                        throw new ApplicationException("Validation failed when attempting to get native info from Yobit.");
                    }
                    return text;
                }
                catch (Exception exception)
                {
                    _log.Error($"An exception was thrown when attempting to get native info from Yobit.{Environment.NewLine}{exception.Message}");
                    _log.Error(exception);

                    throw;
                }
            });

            var collectionContext = new MongoCollectionContext(DbContext, "yobit--get-info");

            var cacheResult = _cacheUtil.GetCacheableEx(ThrottleContext, retriever, collectionContext, YobitInfoThreshold, cachePolicy, validator);
            return translator(cacheResult?.Contents);
        }

        private string GetCacheable(string url, IMongoCollection<WebRequestEventContainer> collection, TimeSpan threshold, CachePolicy cachePolicy = CachePolicy.AllowCache)
        {
            var currentTime = DateTime.UtcNow;

            var databaseResult =
                cachePolicy != CachePolicy.OnlyUseCache
                ? collection.AsQueryable()
                .OrderByDescending(item => item.Id)
                .FirstOrDefault()
                : null;

            if (databaseResult != null && currentTime - databaseResult.StartTimeUtc < threshold && !string.IsNullOrWhiteSpace(databaseResult.Raw))
            {
                return databaseResult.Raw;
            }

            var response = HttpGet(url);
            var ec = new WebRequestEventContainer
            {
                StartTimeUtc = response.startTime,
                EndTimeUtc = response.endTime,
                Raw = response.contents,
                Context = new WebRequestContext
                {
                    Url = url,
                    Verb = "GET"
                }
            };

            collection.InsertOne(ec);

            return response.contents;
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

        private IMongoCollection<WebRequestEventContainer> GetDepthCollection(TradingPair tradingPair)
        {
            return new MongoCollectionContext(DbContext, $"yobit-{tradingPair}").GetCollection<WebRequestEventContainer>();
        }

        //private static Dictionary<string, string> SymbolTranslation = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase)
        //{
        //    { "JNT", "JointCoin" },
        //    { "OMG", "OMGame" },
        //    { "PAY", "EPay" },
        //    { "PLAY", "PlayCoin" },
        //    { "SUB", "Subscripto" },
        //    { "COV", "CovenCoin" },
        //    { "STK", "StakeCoin" },
        //    { "CS", "CryptoSpots" },
        //    { "KNC", "KingN" },
        //    { "BHC", "BlackholeCoin" },
        //    { "POLY", "Polybit" },
        //};

        private IMongoDatabaseContext DbContext
        {
            get { return new MongoDatabaseContext(_getConnectionString.GetConnectionString(), DatabaseName); }
        }
    }
}
