using bit_z_lib.Models;
using bit_z_lib.res;
using bit_z_model;
using bitz_data_lib;
using config_connection_string_lib;
using log_lib;
using mongo_lib;
using MongoDB.Driver;
using Newtonsoft.Json;
using res_util_lib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using cache_lib.Models;
using trade_model;
using trade_node_integration;
using trade_res;
using web_util;
using cache_lib;
using config_client_lib;
using bit_z_lib.Models.Balance;
using cache_lib.Models.Snapshots;
using MongoDB.Bson;
using trade_constants;

namespace bit_z_lib
{
    // https://apidoc.bit-z.pro/en/
    public class BitzIntegration : IBitzIntegration
    {
        // bit-z.com, bit-z.pro, and bitz.com are the official urls.
        // private const string ApiRoot = "https://api.bit-z.com/";
        // private const string ApiRoot = "https://api.bit-z.pro/";
        // private const string ApiRoot = "https://api.bitz.com/";
        private const string ApiRoot = "https://apiv2.bitz.com";

        private static TimeSpan DataThreshold = TimeSpan.FromMinutes(20);
        private static TimeSpan RefreshThreshold = TimeSpan.FromMinutes(17.5);
        private static TimeSpan BalanceThreshold = TimeSpan.FromMinutes(20);
        private static TimeSpan OpenOrdersThreshold = TimeSpan.FromMinutes(20);
        private static TimeSpan BitzMarketsThreshold = TimeSpan.FromMinutes(20);
        private static TimeSpan BitzCommoditiesThreshold = TimeSpan.FromMinutes(20);

        private const string DatabaseName = "bitz";

        private static ThrottleContext ThrottleContext = new ThrottleContext
        {
            Locker = new object(),
            ThrottleThreshold = TimeSpan.FromSeconds(1)
        };

        private static readonly Random _random = new Random();

        private readonly IBitzClient _bitzClient;
        private readonly IConfigClient _configClient;
        private readonly ITradeNodeUtil _tradeNodeUtil;
        private readonly IWebUtil _webUtil;
        private readonly ICacheUtil _cacheUtil;
        private readonly IBitzFundsRepo _bitzFundsRepo;
        private readonly IGetConnectionString _getConnectionString;
        private readonly ILogRepo _log;

        private BitzMap _bitzMap = new BitzMap();

        public BitzIntegration(
            IBitzClient bitzClient,
            IConfigClient configClient,
            ITradeNodeUtil tradeNodeUtil,
            IWebUtil webUtil,
            IBitzFundsRepo bitzFundsRepo,            
            IGetConnectionString getConnectionString,
            ICacheUtil cacheUtil,
            ILogRepo log)
        {
            _bitzClient = bitzClient;
            _configClient = configClient;

            _tradeNodeUtil = tradeNodeUtil;
            _webUtil = webUtil;
            _getConnectionString = getConnectionString;
            _bitzFundsRepo = bitzFundsRepo;

            _cacheUtil = cacheUtil;
            _log = log;            
        }

        private AsOfWrapper<BitzGetMarketDepthResponse> GetNativeMarketDepth(string nativeSymbol, string nativeBaseSymbol, CachePolicy cachePolicy)
        {
            var effectiveNativeSymbol = nativeSymbol.Trim().ToLower();
            var effectiveNativeBaseSymbol = nativeBaseSymbol.Trim().ToLower();

            var url = $"https://apiv2.bitz.com/Market/depth?symbol={effectiveNativeSymbol}_{effectiveNativeBaseSymbol}";

            var translator = new Func<string, BitzGetMarketDepthResponse>(text =>
            {
                return !string.IsNullOrWhiteSpace(text)
                    ? JsonConvert.DeserializeObject<BitzGetMarketDepthResponse>(text)
                    : null;
            });

            var validator = new Func<string, bool>(text =>
            {
                if (string.IsNullOrWhiteSpace(text)) { throw new ApplicationException($"{Name} responded with null or whitespace text when requesting order book for {nativeSymbol}-{nativeBaseSymbol}"); }
                var item = translator(text);

                const int ExpectedStatus = 200;
                if (item.Status != ExpectedStatus)
                {
                    throw new ApplicationException($"{Name} responded with Status of {item.Status} when requesting order book for {nativeSymbol}-{nativeBaseSymbol}, but we expected the status to be {ExpectedStatus}.");
                }

                return true;
            });

            var retriever = new Func<string>(() =>
            {
                try
                {
                    var text = _webUtil.Get(url);
                    if (!validator(text)) { throw new ApplicationException($"Response validation failed when requesting {nativeSymbol}-{nativeBaseSymbol} order book from {Name}."); }
                    return text;
                }
                catch (Exception exception)
                {
                    _log.Error(exception);
                    throw;
                }                
            });

            var key = $"{effectiveNativeSymbol}-{effectiveNativeBaseSymbol}";
            var collectionContext = GetOrderBookContext();
            var cacheResult = _cacheUtil.GetCacheableEx(ThrottleContext, retriever, collectionContext, BitzMarketsThreshold, cachePolicy, validator, AfterInsertOrderBook, key);

            var translated = translator(cacheResult?.Contents);

            return new AsOfWrapper<BitzGetMarketDepthResponse>
            {
                AsOfUtc = DateTime.UtcNow,
                Data = translated
            };
        }

        public OrderBook GetOrderBook(TradingPair tradingPair, CachePolicy cachePolicy)
        {
            var nativeSymbol = _bitzMap.ToNativeSymbol(tradingPair.Symbol);
            var nativeBaseSymbol = _bitzMap.ToNativeSymbol(tradingPair.BaseSymbol);
            var marketDepthWithAsOf = GetNativeMarketDepth(nativeSymbol, nativeBaseSymbol, cachePolicy);

            //"57.70000000",  //price
            //"1.6783",       //number
            //"96.837910000000"    //Total price

            var asks = marketDepthWithAsOf?.Data?.Data?.Asks != null
                ? marketDepthWithAsOf.Data.Data.Asks.Select(item =>
                {
                    return new Order
                    {
                        Price = item[0],
                        Quantity = item[1]
                    };
                }).ToList()
                : new List<Order>();

            var bids = marketDepthWithAsOf?.Data?.Data?.Bids != null
                ? marketDepthWithAsOf.Data.Data.Bids.Select(item =>
                {
                    return new Order
                    {
                        Price = item[0],
                        Quantity = item[1]
                    };
                }).ToList()
                : new List<Order>();

            return marketDepthWithAsOf != null
                ? new OrderBook
                {
                    AsOf = marketDepthWithAsOf.AsOfUtc,
                    Asks = asks,
                    Bids = bids
                }
                : null;
        }

        public List<OrderBookAndTradingPair> GetCachedOrderBooks()
        {
            var context = GetAllOrderBooksCollectionContext();
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

                var orderBook = ToOrderBook(cachedOrderBook.Raw, cachedOrderBook.AsOfUtc);
                orderBookAndTradingPair.Asks = orderBook.Asks;
                orderBookAndTradingPair.Bids = orderBook.Bids;
                orderBookAndTradingPair.AsOf = orderBook.AsOf;

                orders.Add(orderBookAndTradingPair);
            }

            return orders;
        }

        private MongoCollectionContext GetOrderBookContext() => new MongoCollectionContext(_getConnectionString.GetConnectionString(), DatabaseName, "bitz--get-market-depth");

        private void AfterInsertOrderBook(CacheEventContainer container)
        {
            var itemContext = GetOrderBookContext();
            var snapShotContext = GetAllOrderBooksCollectionContext();
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

        private OrderBook ToOrderBook(string text, DateTime? asOf)
        {
            var native = JsonConvert.DeserializeObject<BitzGetMarketDepthResponse>(text);
            var asks = native.Data?.Asks != null
                ? native.Data.Asks.Select(item =>
                {
                    return new Order
                    {
                        Price = item[0],
                        Quantity = item[1]
                    };
                }).ToList()
                : new List<Order>();

            var bids = native.Data?.Bids != null
                ? native.Data.Bids.Select(item =>
                {
                    return new Order
                    {
                        Price = item[0],
                        Quantity = item[1]
                    };
                }).ToList()
                : new List<Order>();

            return native != null
                ? new OrderBook
                {
                    AsOf = asOf,
                    Asks = asks,
                    Bids = bids
                }
                : null;
        }

        private MongoCollectionContext GetAllOrderBooksCollectionContext()
        {
            return new MongoCollectionContext(DbContext, "bitz--all-order-books");
        }

        public List<BitzMarket> GetNativeMarkets(CachePolicy cachePolicy)
        {
            var translator = new Func<string, List<BitzMarket>>(text =>
                !string.IsNullOrWhiteSpace(text)
                ? JsonConvert.DeserializeObject<List<BitzMarket>>(text)
                : null);

            var validator = new Func<string, bool>(text =>
            {
                if (string.IsNullOrWhiteSpace(text)) { throw new ApplicationException("Bit-Z returned an empty response when requesting markets."); }
                return translator(text) != null;                
            });

            var retriever = new Func<string>(() =>
            {
                try
                {
                    var contents = _tradeNodeUtil.FetchMarkets(CcxtName);
                    if (!validator(contents))
                    {
                        throw new ApplicationException("Bit-Z's response when requesting markets failed validation.");
                    }

                    return contents;
                }
                catch (Exception exception)
                {
                    _log.Error(exception);
                    throw;
                }
            });

            var collectionContext = new MongoCollectionContext(DbContext, "bitz---markets");
            var cacheResult = _cacheUtil.GetCacheableEx(ThrottleContext, retriever, collectionContext, BitzMarketsThreshold, cachePolicy, validator);

            return translator(cacheResult?.Contents);
        }

        public List<TradingPair> GetTradingPairs(CachePolicy cachePolicy)
        {
            var tradingPairsWithAsOf = GetTradingPairsWithAsOf(cachePolicy);
            return tradingPairsWithAsOf?.TradingPairs;
        }

        private bool DoesContentsIndicateMaintenance(string contents)
        {
            const string MaintenanceText = "系统维护中, 请稍后访问...  (:";
            return contents != null && contents.Contains(MaintenanceText);
        }

        // https://www.bit-z.com/about/fee
        public Dictionary<string, decimal> GetWithdrawalFees(CachePolicy cachePolicy)
        {
            return ResUtil.Get<Dictionary<string, decimal>>("withdrawal-fees.json", typeof(BitzResDummy).Assembly);
        }

        public BitzSymbolListResponseWithAsOf GetNativeSymbolListWithAsOf(CachePolicy cachePolicy)
        {
            const string Url = "https://apiv2.bitz.com/Market/symbolList";

            var translator = new Func<string, BitzSymbolListResponse>(text =>
            {
                return !string.IsNullOrWhiteSpace(text)
                    ? JsonConvert.DeserializeObject<BitzSymbolListResponse>(text)
                    : null;
            });

            var validator = new Func<string, bool>(text =>
            {
                if (string.IsNullOrWhiteSpace(text)) { throw new ApplicationException($"Received a null or whitespace response when requesting {Name} commodities."); }

                return true;
            });

            var retriever = new Func<string>(() =>
            {
                try
                {
                    var text = _webUtil.Get(Url);
                    return text;
                }
                catch (Exception exception)
                {
                    _log.Error($"Failed to get {Name} commodities.{Environment.NewLine}{exception.Message}");
                    _log.Error(exception);

                    throw;
                }
            });

            var collectionContext = new MongoCollectionContext(DbContext, "bitz--symbol-list");
            var cacheResult = _cacheUtil.GetCacheableEx(ThrottleContext, retriever, collectionContext, BitzCommoditiesThreshold, cachePolicy, validator);

            return new BitzSymbolListResponseWithAsOf
            {
                SymbolListResponse = translator(cacheResult?.Contents),
                AsOfUtc = cacheResult?.AsOf
            };
        }

        public List<CommodityForExchange> GetCommodities(CachePolicy cachePolicy)
        {
            var commoditiesWithAsOf = GetExchangeCommoditiesWithAsOf(cachePolicy);
            return commoditiesWithAsOf?.Commodities;
        }

        private static BitzCoinListResponse _coinRes = null;
        private static BitzCoinListResponse CachedCoinRes
        {
            get
            {
                if (_coinRes != null) { return _coinRes; }
                var contents = ResUtil.Get("bitz-coin-list.json", typeof(BitzResDummy).Assembly);
                _coinRes = JsonConvert.DeserializeObject<BitzCoinListResponse>(contents);

                return _coinRes;
            }
        }

        private static Dictionary<string, BitzCoin> _bitzCoinDictionaryPropertyInternal = null;
        private static Dictionary<string, BitzCoin> BitzCoinDictionary
        {
            get
            {
                if (_bitzCoinDictionaryPropertyInternal != null) { return _bitzCoinDictionaryPropertyInternal; }
                var dict = new Dictionary<string, BitzCoin>(StringComparer.InvariantCultureIgnoreCase);
                
                foreach (var coin in CachedCoinRes.Data)
                {
                    dict[coin.Name] = coin;
                }

                _bitzCoinDictionaryPropertyInternal = dict;

                return _bitzCoinDictionaryPropertyInternal;
            }
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

        private AsOfWrapper<BitzGetBalancesResponse> GetNativeBalances(CachePolicy cachePolicy)
        {
            var translator = new Func<string, BitzGetBalancesResponse>(text =>
                !string.IsNullOrWhiteSpace(text)
                    ? JsonConvert.DeserializeObject<BitzGetBalancesResponse>(text)
                    : null);

            var validator = new Func<string, bool>(text =>
            {
                if (string.IsNullOrWhiteSpace(text))
                {
                    throw new ApplicationException("Received a null or whitespace response from Bit-Z when requesting balances.");
                }

                var translated = translator(text);
                if (translated == null) { throw new ApplicationException($"{nameof(translated)} should not be null."); }

                if (translated.Status == -111)
                {
                    throw new ApplicationException($"Status {translated.Status} indicates failure.");
                }

                return true;
            });

            var retriever = new Func<string>(() =>
            {
                try
                {
                    var apiKey = _configClient.GetBitzApiKey();
                    var text = _bitzClient.GetBalances(apiKey);
                    if (!validator(text))
                    {
                        throw new ApplicationException("Bitz's response when requesting balances failed validation.");
                    }

                    return text;
                }
                catch(Exception exception)
                {
                    _log.Error(exception);
                    throw;
                }
            });

            var collectionContext = new MongoCollectionContext(DbContext, "bitz--get-balances");

            var cacheResult = _cacheUtil.GetCacheableEx(ThrottleContext, retriever, collectionContext, BalanceThreshold, cachePolicy, validator);

            return new AsOfWrapper<BitzGetBalancesResponse>
            {
                Data = translator(cacheResult?.Contents),
                AsOfUtc = cacheResult?.AsOf
            };
        }

        public HoldingInfo GetHoldings(CachePolicy cachePolicy)
        {
            var nativeBalancesWithAsOf = GetNativeBalances(cachePolicy);
            if (nativeBalancesWithAsOf == null)
            {
                throw new ApplicationException($"{nameof(nativeBalancesWithAsOf)} should not be null.");
            }

            var holdings = nativeBalancesWithAsOf.Data.Data.Info != null
                ? nativeBalancesWithAsOf.Data.Data.Info.Select(item =>
                {
                    var nativeSymbol = item.Name;
                    var symbol = _bitzMap.ToCanonicalSymbol(nativeSymbol).ToUpper();

                    var total = item.Num ?? 0;
                    var inOrders = item.Lock ?? 0;
                    var available = total - inOrders;

                    return new Holding
                    {
                        Symbol = symbol,
                        Total = item.Num ?? 0,
                        InOrders = item.Lock ?? 0,
                        Available = available
                    };
                }).ToList()
                : new List<Holding>();

            return new HoldingInfo
            {
                Holdings = holdings,
                TimeStampUtc = nativeBalancesWithAsOf?.AsOfUtc
            };
        }

        public List<DepositAddressWithSymbol> GetDepositAddresses(CachePolicy cachePolicy)
        {
            var container = _bitzFundsRepo.GetMostRecent();
            if (container == null || container.Funds == null) { return new List<DepositAddressWithSymbol>(); }

            return container.Funds.Where(queryFund => queryFund.DepositAddress != null)
                .Select(item =>
                {
                    return new DepositAddressWithSymbol
                    {
                        Symbol = item.Symbol,
                        Address = item.DepositAddress?.Address,
                        Memo = item.DepositAddress?.Memo
                    };
                }).ToList();
        }

        public DepositAddress GetDepositAddress(string symbol, CachePolicy cachePolicy)
        {
            return _bitzFundsRepo.GetDepositAddress(symbol);
        }

        public string GetCcxtHistory()
        {
            var response = _tradeNodeUtil.GetUserTradeHistory(IntegrationNameRes.Bitz);
            return response;
        }

        public string GetHistoryFromClient(DateTime? startTime = null, DateTime? endTime = null, int? page = null)
        {
            var apiKey = _configClient.GetBitzApiKey();
            return ((BitzClient)_bitzClient).GetHistory(apiKey, startTime, endTime, page);
        }

        public List<HistoricalTrade> GetUserTradeHistory(CachePolicy cachePolicy)
        {
            var collectionContext = new MongoCollectionContext(_getConnectionString.GetConnectionString(), DatabaseName, "bitz--trade-history");
            var collection = collectionContext.GetCollection<BitzTradeHistoryContainer>();

            var historyContainer = collection.AsQueryable()
                .OrderByDescending(item => item.Id)
                .FirstOrDefault();

            if (historyContainer == null || historyContainer.History == null) { return new List<HistoricalTrade>(); }

            var tradeTypeDictionary = new Dictionary<string, TradeTypeEnum>(StringComparer.InvariantCultureIgnoreCase)
            {
                { "Buy", TradeTypeEnum.Buy },
                { "Sell", TradeTypeEnum.Sell },
            };

            var parseTradingPair = new Func<BitzTradeHistoryItem, TradingPair>(item =>
            {
                if (item == null || string.IsNullOrWhiteSpace(item.Market)) { return null; }
                var pieces = item.Market.Trim().Split('/');
                if (pieces.Count() != 2) { return null; }
                var symbol = pieces[0];
                var baseSymbol = pieces[1];
                return new TradingPair(symbol, baseSymbol);
            });
            

            return historyContainer.History.Select(item =>
            {
                return new HistoricalTrade
                {
                    TimeStampUtc = item.TransactionTime,
                    Price = item.Price,
                    Quantity = item.Amount,
                    TradeType = item.Type != null && tradeTypeDictionary.ContainsKey(item.Type.Trim()) ? tradeTypeDictionary[item.Type.Trim()] : TradeTypeEnum.Unknown,
                    TradingPair = parseTradingPair(item),
                    
                    // TODO: still need to write the parser for this.
                    // Fee = parseFee(item)
                };
            }).ToList();
            /*
        public TradingPair TradingPair { get; set; }
        public DateTime TimeStampUtc { get; set; }
        public decimal Price { get; set; }
        public decimal Quantity { get; set; }
        public decimal Fee { get; set; }
        public TradeTypeEnum TradeType { get; set; }
            */
        }

        private static object GetNativeBalanceTextLocker = new object();
        private string GetNativeBalanceText()
        {
            lock (GetNativeBalanceTextLocker)
            {
                return _tradeNodeUtil.FetchBalance("bitz");
            }
        }

        public bool BuyLimit(TradingPair tradingPair, decimal quantity, decimal price)
        {
            var nativeSymbol = _bitzMap.ToNativeSymbol(tradingPair.Symbol);
            var nativeBaseSymbol = _bitzMap.ToNativeSymbol(tradingPair.BaseSymbol);

            var apiKey = _configClient.GetBitzApiKey();
            var tradePassword = _configClient.GetBitzTradePassword();

            _log.Info($"About to place a buy limit order on {Name} for {quantity} {nativeSymbol} at {price} {nativeBaseSymbol}.");
            var responseText = _bitzClient.BuyLimit(apiKey, tradePassword, nativeSymbol, nativeBaseSymbol, quantity, price);

            if (string.IsNullOrWhiteSpace(responseText))
            {
                throw new ApplicationException($"{Name} returned a null or whitespace response when attempting to place a buy limit order for {quantity} {nativeSymbol} at {price} {nativeBaseSymbol}.");
            }
            _log.Info($"Response from placing a buy limit order on {Name} for {quantity} {nativeSymbol} at {price} {nativeBaseSymbol}.{Environment.NewLine}{responseText}");

            var parsedResponse = JsonConvert.DeserializeObject<BitzPlaceOrderResponse>(responseText);
            const int ExpectedStatus = 200;
            if (parsedResponse.Status != ExpectedStatus)
            {
                throw new ApplicationException($"Expected status code {ExpectedStatus} but received status code {parsedResponse.Status} from {Name} when attempting to place a buy limit order for {quantity} {nativeSymbol} at {price} {nativeBaseSymbol}.");
            }

            return true;
        }

        public bool SellLimit(TradingPair tradingPair, decimal quantity, decimal price)
        {
            if (tradingPair == null) { throw new ArgumentNullException(nameof(tradingPair)); }
            if (quantity <= 0) { throw new ArgumentOutOfRangeException(nameof(quantity)); }
            if (price <= 0) { throw new ArgumentOutOfRangeException(nameof(price)); }

            var nativeSymbol = _bitzMap.ToNativeSymbol(tradingPair.Symbol);
            var nativeBaseSymbol = _bitzMap.ToNativeSymbol(tradingPair.BaseSymbol);

            var apiKey = _configClient.GetBitzApiKey();
            var tradePassword = _configClient.GetBitzTradePassword();

            _log.Info($"About to place a sell limit order on {Name} for {quantity} {nativeSymbol} at {price} {nativeBaseSymbol}.");
            var responseText = _bitzClient.SellLimit(apiKey, tradePassword, nativeSymbol, nativeBaseSymbol, quantity, price);

            if (string.IsNullOrWhiteSpace(responseText))
            {
                throw new ApplicationException($"{Name} returned a null or whitespace response when attempting to place a sell limit order for {quantity} {nativeSymbol} at {price} {nativeBaseSymbol}.");
            }
            _log.Info($"Response from placing a sell limit order on {Name} for {quantity} {nativeSymbol} at {price} {nativeBaseSymbol}.{Environment.NewLine}{responseText}");

            var parsedResponse = JsonConvert.DeserializeObject<BitzPlaceOrderResponse>(responseText);
            const int ExpectedStatus = 200;
            if (parsedResponse.Status != ExpectedStatus)
            {
                throw new ApplicationException($"Expected status code {ExpectedStatus} but received status code {parsedResponse.Status} from {Name} when attempting to place a sell limit order for {quantity} {nativeSymbol} at {price} {nativeBaseSymbol}.");
            }

            return true;
        }

        public bool BuyMarket(TradingPair tradingPair, decimal quantity)
        {
            throw new NotImplementedException();
        }

        public bool SellMarket(TradingPair tradingPair, decimal quantity)
        {
            throw new NotImplementedException();
        }

        internal class NativeOpenOrder
        {
            public string Id { get; set; }
            public DateTime DateTime { get; set; }
            public long TimeStamp { get; set; }
            public string Status { get; set; }
            public string Symbol { get; set; }
            public string Type { get; set; }
            public decimal Price { get; set; }
            public decimal Amount { get; set; }

            public NativeOpenOrderInfo Info { get; set; }

            public class NativeOpenOrderInfo
            {
                public string Id { get; set; }
                public decimal Price { get; set; }
                public decimal Number { get; set; }
                public string Flag { get; set; }
                public int Status { get; set; }
                public DateTime DateTime { get; set; }
            }
        }

        private AsOfWrapper<List<NativeOpenOrder>> GetNativeOpenOrders(string nativeSymbol, string nativeBaseSymbol, CachePolicy cachePolicy)
        {
            var translator = new Func<string, List<NativeOpenOrder>>(text =>
            {
                return !string.IsNullOrWhiteSpace(text)
                    ? JsonConvert.DeserializeObject<List<NativeOpenOrder>>(text)
                    : new List<NativeOpenOrder>();
            });

            var validator = new Func<string, bool>(text =>
            {
                if (string.IsNullOrWhiteSpace(text)) { throw new ApplicationException($"Bit-Z returned a null response when requesting open orders for \"{nativeSymbol}-{nativeBaseSymbol}\"."); }
                return translator(text) != null;
            });

            var retriever = new Func<string>(() =>
            {
                try
                {
                    var contents = _tradeNodeUtil.GetNativeOpenOrders("bitz", new TradingPair(nativeSymbol, nativeBaseSymbol));
                    if (!validator(contents))
                    {
                        _log.Error("Bit-Z's response failed validation when requesting open orders for \"{tradingPair}\".");
                        return null;
                    }

                    return contents;
                }
                catch (Exception exception)
                {
                    _log.Error(exception);
                    throw;
                }
            });

            var collectionContext = new MongoCollectionContext(DbContext, $"bitz--fetch-open-orders--{nativeSymbol.ToUpper()}-{nativeBaseSymbol.ToUpper()}");
            
            var cacheResult = _cacheUtil.GetCacheableEx(ThrottleContext, retriever, collectionContext, OpenOrdersThreshold, cachePolicy, validator);

            return new AsOfWrapper<List<NativeOpenOrder>>
            {
                AsOfUtc = cacheResult?.AsOf,
                Data = translator(cacheResult?.Contents)
            };
        }

        public List<OpenOrder> GetOpenOrdersForTradingPair(TradingPair tradingPair, CachePolicy cachePolicy)
        {
            return GetOpenOrdersForTradingPairV2(tradingPair.Symbol, tradingPair.BaseSymbol, cachePolicy)
                ?.OpenOrders
                ?? new List<OpenOrder>();
        }

        public List<OpenOrder> GetOpenOrdersForTradingPairOld(TradingPair tradingPair, CachePolicy cachePolicy)
        {
            var nativeSymbol = _bitzMap.ToNativeSymbol(tradingPair.Symbol);
            var nativeBaseSymbol = _bitzMap.ToNativeSymbol(tradingPair.BaseSymbol);

            var nativeOpenOrders = GetNativeOpenOrders(nativeSymbol, nativeBaseSymbol, cachePolicy)?.Data;

            var canon = (nativeOpenOrders ?? new List<NativeOpenOrder>())
                .Select(native =>
                {
                    return new OpenOrder
                    {
                        OrderId = native.Id,
                        Price = native.Price,
                        Quantity = native.Amount,
                        OrderType = string.Equals(native.Info.Flag, "buy", StringComparison.InvariantCultureIgnoreCase)
                            ? OrderType.Bid : OrderType.Ask
                    };
                }).ToList();

            return canon;
        }

        public void CancelAllOpenOrdersForTradingPair(TradingPair tradingPair)
        {
            var response = _tradeNodeUtil.CancelAllOpenOrdersForTradingPair("bitz", tradingPair);
            Console.WriteLine(response);
        }

        private static object Locker = new object();

        private const string CcxtName = "bitz";
        public string Name => "Bit-Z";
        public Guid Id => new Guid("D38AB174-0541-4151-95BD-B2C4EB15584B");

        private Commodity GetCanonForNativeSymbol(string nativeSymbol, List<CommodityMapItem> commodityMap, List<Commodity> allCanon)
        {
            var mapItem = commodityMap.SingleOrDefault(map => string.Equals(map.NativeSymbol, nativeSymbol));
            var canon = mapItem != null
                ? allCanon.Single(item => item.Id == mapItem.CanonicalId)
                : null;

            return canon;
        }

        // TODO: This needs to be modified to handle multiple pages.
        public AsOfWrapper<List<BitzGetOpenOrdersResponse>> GetClientOpenOrders(CachePolicy cachePolicy)
        {
            var translator = new Func<string, List<BitzGetOpenOrdersResponse>>(text =>
                !string.IsNullOrWhiteSpace(text)
                ? JsonConvert.DeserializeObject<List<BitzGetOpenOrdersResponse>>(text)
                : null);

            var validator = new Func<string, bool>(text =>
            {
                if (string.IsNullOrWhiteSpace(text)) { throw new ApplicationException($"Received a null or whitespace response from {Name} when requesting the UserNowEntrustSheet."); }
                var translatedItems = translator(text);
                foreach (var translatedItem in translatedItems)
                {
                    const int ExpectedStatus = 200;
                    if (translatedItem.Status != ExpectedStatus) { throw new ApplicationException($"Expected status {ExpectedStatus} but received status {translatedItem.Status} from {Name} when requesting the UserNowEntrustSheet."); }
                }

                return true;
            });

            var retriever = new Func<string>(() =>
            {
                try
                {
                    var apiKey = _configClient.GetBitzApiKey();
                    var result = _bitzClient.GetOpenOrderResponses(apiKey);
                    return result != null ? JsonConvert.SerializeObject(result) : null;
                }
                catch (Exception exception)
                {
                    _log.Error(exception);
                    throw;
                }
            });

            var collectionContext = new MongoCollectionContext(DbContext, "bitz--get-client-open-order-responses");

            var cacheResult = _cacheUtil.GetCacheableEx(ThrottleContext, retriever, collectionContext, OpenOrdersThreshold, cachePolicy, validator, null, null);

            return new AsOfWrapper<List<BitzGetOpenOrdersResponse>>
            {
                AsOfUtc = cacheResult?.AsOf,
                Data = translator(cacheResult?.Contents)
            };
        }

        private AsOfWrapper<List<BitzGetOpenOrdersResponse>> GetNativeOpenOrders(CachePolicy cachePolicy)
        {
            var translator = new Func<string, List<BitzGetOpenOrdersResponse>>(text =>
                !string.IsNullOrWhiteSpace(text)
                    ? JsonConvert.DeserializeObject<List<BitzGetOpenOrdersResponse>>(text)
                    : null);

            var validator = new Func<string, bool>(text =>
            {
                if (string.IsNullOrWhiteSpace(text)) { throw new ApplicationException("Bit-Z returned a null or whitespace response when requesting open orders."); }
                JsonConvert.DeserializeObject<List<BitzGetOpenOrdersResponse>>(text);

                return true;
            });

            var retriever = new Func<string>(() =>
            {
                var apiKey = _configClient.GetBitzApiKey();
                var resps = _bitzClient.GetOpenOrderResponses(apiKey);

                return JsonConvert.SerializeObject(resps);
            });

            var collectionContext = new MongoCollectionContext(DbContext, "bitz--open-orders");

            var cacheResult = _cacheUtil.GetCacheableEx(ThrottleContext, retriever, collectionContext, OpenOrdersThreshold, cachePolicy, validator);

            return new AsOfWrapper<List<BitzGetOpenOrdersResponse>>
            {
                AsOfUtc = cacheResult?.AsOf,
                Data = translator(cacheResult?.Contents)
            };
        }

        public List<OpenOrderForTradingPair> GetOpenOrders(CachePolicy cachePolicy)
        {
            var native = GetNativeOpenOrders(cachePolicy);
            var dataItems = new List<BitzOpenOrdersInfoDataItem>();
            foreach (var response in native.Data)
            {
                foreach (var item in response.Data.Data)
                {
                    dataItems.Add(item);
                }
            }

            var orderTypeDictionary = new Dictionary<string, OrderType>(StringComparer.InvariantCultureIgnoreCase)
            {
                { "buy", OrderType.Bid },
                { "sell", OrderType.Ask }
            };

            return dataItems.Select(item =>
            {
                var nativeSymbol = item.CoinFrom;
                var nativeBaseSymbol = item.CoinTo;

                var symbol = _bitzMap.ToCanonicalSymbol(nativeSymbol);
                var baseSymbol = _bitzMap.ToCanonicalSymbol(nativeBaseSymbol);

                var orderType = orderTypeDictionary.ContainsKey(item.Flag)
                    ? orderTypeDictionary[item.Flag]
                    : OrderType.Unknown;

                return new OpenOrderForTradingPair
                {
                    OrderId = item.Id,
                    Symbol = symbol,
                    BaseSymbol = baseSymbol,
                    Price = item.Price ?? 0,
                    Quantity = item.Number ?? 0,
                    OrderType = orderType
                };
            }).ToList();
        }

        public ExchangeTradingPairsWithAsOf GetTradingPairsWithAsOf(CachePolicy cachePolicy)
        {
            var symbolListResponseWithAsOf = GetNativeSymbolListWithAsOf(cachePolicy);

            var bitzTradingPairs = new List<BitzSymbol>();
            if (symbolListResponseWithAsOf?.SymbolListResponse?.Data != null)
            {
                foreach (var key in symbolListResponseWithAsOf.SymbolListResponse.Data.Keys.ToList())
                {
                    var bitzSymbol = symbolListResponseWithAsOf.SymbolListResponse.Data[key];
                    bitzTradingPairs.Add(bitzSymbol);
                }
            }

            var translator = new Func<BitzSymbol, TradingPair>(nativeItem =>
            {
                /*
                    "id": "1",
                    "name": "ltc_btc",
                    "coinFrom": "ltc",
                    "coinTo": "btc",
                    "numberFloat": "4",
                    "priceFloat": "8",
                    "status": "1",
                    "minTrade": "0.010",
                    "maxTrade": "500000000.000"
                */

                var priceTick = nativeItem.PriceFloat.HasValue && nativeItem.PriceFloat.Value >= 1
                    ? (decimal)Math.Pow(0.1, nativeItem.PriceFloat.Value)
                    : (decimal?)null;                

                var lotSize = nativeItem.NumberFloat.HasValue && nativeItem.NumberFloat.Value >= 1
                    ? (decimal)Math.Pow(0.1, nativeItem.NumberFloat.Value)
                    : (decimal?)null;

                var nativeSymbol = nativeItem.CoinFrom.ToUpper();
                var nativeCoin = BitzCoinDictionary.ContainsKey(nativeItem.CoinFrom)
                    ? BitzCoinDictionary[nativeItem.CoinFrom]
                    : null;

                var nativeName = !string.IsNullOrWhiteSpace(nativeCoin?.Display)
                    ? nativeCoin.Display
                    : nativeSymbol;

                var nativeBaseSymbol = nativeItem.CoinTo.ToUpper();
                var nativeBaseCoin = BitzCoinDictionary.ContainsKey(nativeItem.CoinTo)
                    ? BitzCoinDictionary[nativeItem.CoinTo]
                    : null;

                var nativeBaseSymbolName = !string.IsNullOrWhiteSpace(nativeBaseCoin?.Display)
                    ? nativeBaseCoin.Display
                    : nativeBaseSymbol;

                var canon = _bitzMap.GetCanon(nativeSymbol);
                var symbol = !string.IsNullOrWhiteSpace(canon?.Symbol)
                    ? canon.Symbol
                    : nativeSymbol;

                var commodityName = !string.IsNullOrWhiteSpace(canon?.Name)
                    ? canon.Name
                    : nativeName;

                var baseCanon = _bitzMap.GetCanon(nativeBaseSymbol);

                var baseSymbol = !string.IsNullOrWhiteSpace(baseCanon?.Symbol)
                    ? baseCanon.Symbol
                    : nativeBaseSymbol;

                var baseCommodityName = !string.IsNullOrWhiteSpace(baseCanon?.Name)
                    ? baseCanon.Name
                    : nativeBaseSymbolName;

                return new TradingPair
                {
                    CanonicalCommodityId = canon?.Id,
                    Symbol = symbol,
                    CommodityName = commodityName,
                    NativeSymbol = nativeSymbol,
                    NativeCommodityName = nativeName,

                    CanonicalBaseCommodityId = baseCanon?.Id,
                    BaseSymbol = baseSymbol,
                    NativeBaseSymbol = nativeBaseSymbol,
                    NativeBaseCommodityName = nativeBaseSymbolName,

                    MinimumTradeQuantity = nativeItem.MinTrade,
                    PriceTick = priceTick,
                    LotSize = lotSize
                };
            });

            var tradingPairs = bitzTradingPairs.Select(item => translator(item))
                .ToList();

            return new ExchangeTradingPairsWithAsOf
            {
                TradingPairs = tradingPairs,
                AsOfUtc = symbolListResponseWithAsOf?.AsOfUtc
            };
        }

        public ExchangeCommoditiesWithAsOf GetExchangeCommoditiesWithAsOf(CachePolicy cachePolicy)
        {
            var coinDictionary = new Dictionary<string, BitzCoin>(StringComparer.InvariantCultureIgnoreCase);
            foreach (var coin in CachedCoinRes.Data)
            {
                coinDictionary[coin.Name] = coin;
            }

            var coinStatusDictionary = new Dictionary<string, bool?>(StringComparer.InvariantCultureIgnoreCase)
            {
                { "0", true },
                { "1", false },
            };

            var translator = new Func<BitzCoin, CommodityForExchange>(nativeItem =>
            {
                var matchingCoin = coinDictionary.ContainsKey(nativeItem.Name)
                    ? coinDictionary[nativeItem.Name]
                    : null;

                var canDeposit = coinStatusDictionary.ContainsKey(matchingCoin.InStatus)
                    ? coinStatusDictionary[matchingCoin.InStatus]
                    : null;

                var canWithdraw = coinStatusDictionary.ContainsKey(matchingCoin.OutStatus)
                    ? coinStatusDictionary[matchingCoin.OutStatus]
                    : null;

                var nativeSymbol = nativeItem.Name.ToUpper();
                var nativeName = !string.IsNullOrWhiteSpace(matchingCoin?.Display)
                    ? matchingCoin.Display
                    : nativeSymbol.ToUpper();

                var canon = _bitzMap.GetCanon(nativeSymbol);
                var symbol = !string.IsNullOrWhiteSpace(canon?.Symbol)
                    ? canon.Symbol
                    : nativeSymbol;
                
                var name = !string.IsNullOrWhiteSpace(canon?.Name)
                    ? canon.Name
                    : nativeName;

                return new CommodityForExchange
                {
                    CanonicalId = canon?.Id,
                    Symbol = symbol,
                    NativeSymbol = nativeSymbol,
                    NativeName = nativeName,
                    Name = name,      
                    CanDeposit = canDeposit,
                    CanWithdraw = canWithdraw
                };
            });

            return new ExchangeCommoditiesWithAsOf
            {
                Commodities = CachedCoinRes.Data.Select(queryCoin => translator(queryCoin)).ToList(),
                AsOfUtc = null
            };
        }

        public void UpdateCoinList()
        {
            throw new NotImplementedException();
        }

        public List<OpenOrdersForTradingPair> GetOpenOrdersV2()
        {
            throw new NotImplementedException();
        }

        public OpenOrdersWithAsOf GetOpenOrdersForTradingPairV2(string symbol, string baseSymbol, CachePolicy cachePolicy)
        {
            var nativeSymbol = _bitzMap.ToNativeSymbol(symbol);
            var nativeBaseSymbol = _bitzMap.ToNativeSymbol(baseSymbol);

            var clientOpenOrdersResponsesWithAsOf = GetClientOpenOrders(cachePolicy);
            var openOrders = new List<OpenOrder>();
            foreach (var clientOpenOrdersResponse in clientOpenOrdersResponsesWithAsOf?.Data ?? new List<BitzGetOpenOrdersResponse>())
            {
                foreach (var item in clientOpenOrdersResponse.Data?.Data ?? new List<BitzOpenOrdersInfoDataItem>())
                {
                    if (!string.Equals(item.CoinFrom, nativeSymbol, StringComparison.InvariantCultureIgnoreCase)
                        || !string.Equals(item.CoinTo, nativeBaseSymbol, StringComparison.InvariantCultureIgnoreCase))
                    {
                        continue;
                    }

                    var openOrder = new OpenOrder
                    {
                        OrderId = item.Id,
                        Price = item.Price ?? 0,
                        Quantity = item.Number ?? 0,
                        OrderType = string.Equals(item.Flag, "sale", StringComparison.InvariantCultureIgnoreCase)
                            ? OrderType.Ask : OrderType.Bid
                    };

                    openOrders.Add(openOrder);
                }
            }

            return new OpenOrdersWithAsOf
            {
                AsOfUtc = clientOpenOrdersResponsesWithAsOf?.AsOfUtc,
                OpenOrders = openOrders
            };
        }

        public void CancelOrder(string orderId)
        {
            try
            {
                var apiKey = _configClient.GetBitzApiKey();
                var response = _bitzClient.CancelOrder(apiKey, orderId);

                _log.Info($"Response from cancelling {Name} order {orderId}.{Environment.NewLine}{response}");
            }
            catch(Exception exception)
            {
                _log.Error($"An exception occured when attempting to cancel {Name} order {orderId}.{Environment.NewLine}{exception.Message}");
                throw;
            }
        }

        private MongoDatabaseContext DbContext => new MongoDatabaseContext(_getConnectionString.GetConnectionString(), DatabaseName);
    }
}
