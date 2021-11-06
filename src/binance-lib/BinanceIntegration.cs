using Binance.Net;
using Binance.Net.Objects;
using binance_lib.Models;
using binance_lib.Models.Canonical;
using binance_lib.Models.Ccxt;
using CryptoExchange.Net;
using CryptoExchange.Net.Authentication;
using date_time_lib;
using log_lib;
using mongo_lib;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using trade_lib;
using cache_lib.Models;
using trade_model;
using trade_node_integration;
using trade_res;
using web_util;
using trade_lib.Cache;
using cache_lib;
using binance_lib.res;
using commodity_map;
using config_client_lib;

namespace binance_lib
{
    // https://github.com/binance-exchange/binance-official-api-docs/blob/master/rest-api.md
    public class BinanceIntegration :
        OrderBookIntegration,
        IBinanceIntegration
    {
        private const string DatabaseName = "binance";
        private const string CcxtExchangeName = "binance";
        protected override string CollectionPrefix => "binance";

        private static TimeSpan BinanceOrderBookThreshold = TimeSpan.FromMinutes(10);
        private static TimeSpan ThrottleThreshold = TimeSpan.FromMilliseconds(500);
        private static TimeSpan TradeHistoryThreshold = TimeSpan.FromMinutes(10);

        private static readonly ThrottleContext BinanceThrottleContext = new ThrottleContext
        {
            Locker = new object(),
            ThrottleThreshold = TimeSpan.FromSeconds(1)
        };

        protected override ThrottleContext ThrottleContext => BinanceThrottleContext;

        private readonly IWebUtil _webUtil;
        private readonly ISimpleWebCache _webCache;
        private readonly ILogRepo _log;

        private readonly BinanceClient _binanceClient;

        private readonly IMongoDatabaseContext _dbContext;

        private readonly IMongoCollectionContext _orderBookContext;
        private readonly IMongoCollectionContext _orderBookSnapshotContext;
        private readonly IMongoCollectionContext _orderBookGroupSnapShotContext;

        private readonly IMongoCollection<OrderBookEventContainer> _orderBookCollection;
        private readonly IMongoCollection<OrderBookSnapShot> _orderBookSnapShotCollection;
        private readonly IMongoCollection<OrderBookGroupSnapShot> _orderBookGroupSnapShotCollection;
        private readonly IMongoCollectionContext _exchangeInfoContext;

        private const string OrderBookCollectionName = "binance-order-book";
        private const string OrderBookSnapShotCollectionName = "binance-order-book-snapshot";
        private const string OrderBookGroupSnapShotCollectionName = "binance-order-book-group-snapshot";
        private const string ExchangeInfoCollectionName = "binance-ec-exchange-info";
        
        private readonly ITradeNodeUtil _nodeUtil;

        private readonly CacheUtil _cacheUtil;

        private readonly IConfigClient _configClient;

        private readonly BinanceMap _binanceMap = new BinanceMap();

        protected override IMongoDatabaseContext DbContext => _dbContext;
        protected override ILogRepo Log => _log;

        public BinanceIntegration(
            IWebUtil webUtil,
            IConfigClient configClient,
            ITradeNodeUtil nodeUtil,
            ILogRepo log)
        {
            _configClient = configClient;

            _webUtil = webUtil;
            _dbContext = new MongoDatabaseContext(_configClient.GetConnectionString(), DatabaseName);
            _nodeUtil = nodeUtil;
            _log = log;

            _cacheUtil = new CacheUtil();
            _webCache = new SimpleWebCache(webUtil, new MongoCollectionContext(_dbContext, "binance-cache"), "binance");
            
            var apiKey = _configClient.GetBinanceApiKey();
            var options = new BinanceClientOptions
            {
                AutoTimestamp = true,
                ApiCredentials = new ApiCredentials(apiKey.Key, apiKey.Secret)
            };
            _binanceClient = new BinanceClient(options);

            _orderBookContext = new MongoCollectionContext(_dbContext, OrderBookCollectionName);
            _orderBookSnapshotContext = new MongoCollectionContext(_dbContext, OrderBookSnapShotCollectionName);
            _orderBookGroupSnapShotContext = new MongoCollectionContext(_dbContext, OrderBookGroupSnapShotCollectionName);

            _orderBookCollection = _orderBookContext.GetCollection<OrderBookEventContainer>();
            _orderBookSnapShotCollection = _orderBookSnapshotContext.GetCollection<OrderBookSnapShot>();
            _orderBookGroupSnapShotCollection = _orderBookGroupSnapShotContext.GetCollection<OrderBookGroupSnapShot>();

            _exchangeInfoContext = new MongoCollectionContext(_dbContext, ExchangeInfoCollectionName);
        }

        private object _getClientLocker = new object();
        private ApiKey _keyForCurrentClient = null;
        private BinanceClient _currentClient = null;
        private BinanceClient GetClient()
        {
            lock (_getClientLocker)
            {
                var apiKey = _configClient.GetBinanceApiKey();

                var hasKeyChanged = new Func<ApiKey, ApiKey, bool>((a, b) =>
                {
                    if (a == null && b == null) { return false; }
                    if (a == null || b == null) { return true; }

                    if (!string.Equals(a.Key ?? string.Empty, b.Key ?? string.Empty, StringComparison.Ordinal)) { return true; }
                    if (!string.Equals(a.Secret ?? string.Empty, b.Secret ?? string.Empty, StringComparison.Ordinal)) { return true; }

                    return false;
                });

                if (_currentClient != null && !hasKeyChanged(_keyForCurrentClient, apiKey)) { return _currentClient; }
                _keyForCurrentClient = apiKey;

                var options = apiKey != null
                    ? new BinanceClientOptions { ApiCredentials = new ApiCredentials(apiKey.Key, apiKey.Secret) }
                    : new BinanceClientOptions();

                return _currentClient = new BinanceClient(options);
            }
        }

        public string Name { get { return "Binance"; } }

        public Guid Id => new Guid("291CB209-A884-484A-A259-326F24397638");

        protected override CommodityMap Map => _binanceMap;

        public List<HistoricalTrade> GetUserTradeHistory(CachePolicy cachePolicy)
        {
            var allTrades = new List<HistoricalTrade>();

            var pairs = GetTradingPairs(cachePolicy);

            for (var index = 0; index < pairs.Count(); index++)
            {
                var pair = pairs[index];
                var trades = GetNativeUserTradeHistory(pair, cachePolicy);
                allTrades.AddRange(trades.Select(item =>
                {
                    var historicalTrade = new HistoricalTrade
                    {
                        TradingPair = pair,
                        TimeStampUtc = item.Time,
                        Symbol = pair.Symbol,
                        BaseSymbol = pair.BaseSymbol,
                        Price = item.Price,
                        Quantity = item.Quantity,
                        TradeType = item.IsBuyer ? TradeTypeEnum.Buy : TradeTypeEnum.Sell
                    };

                    return historicalTrade;
                }).ToList());
            }

            var withdrawalHistory = GetCcxtWithdrawalHistory(cachePolicy);

            var withdrawalFees = GetWithdrawalFees(CachePolicy.OnlyUseCacheUnlessEmpty);
            foreach (var withdrawal in withdrawalHistory?.WithdrawList ?? new List<BinanceCcxtWithdrawalHistoryResponse.BinanceCcxtWithdrawalHistoryItem>())
            {
                DateTime? applyTimeUtc = null;
                if (withdrawal.ApplyTime.HasValue)
                {
                    var localApplyTime = DateTimeUtil.UnixTimeStampToDateTime(withdrawal.ApplyTime.Value / 1000.0d);
                    if (localApplyTime.HasValue) { applyTimeUtc = localApplyTime.Value.ToUniversalTime(); }
                }

                DateTime? successTimeUtc = null;
                if (withdrawal.SuccessTime.HasValue)
                {
                    var localSuccessTime = DateTimeUtil.UnixTimeStampToDateTime(withdrawal.ApplyTime.Value / 1000.0d);
                    if (localSuccessTime.HasValue) { successTimeUtc = localSuccessTime.Value.ToUniversalTime(); }
                }

                var nativeSymbol = withdrawal.Asset;
                var canon = _binanceMap.GetCanon(nativeSymbol);
                var symbol = !string.IsNullOrWhiteSpace(canon?.Symbol) ? canon.Symbol : nativeSymbol;

                var historicalTrade = new HistoricalTrade
                {
                    Symbol = symbol,
                    Quantity = withdrawal.Amount ?? 0,
                    TradeType = TradeTypeEnum.Withdraw,
                    TimeStampUtc = applyTimeUtc ?? default(DateTime),
                    SuccessTimeStampUtc = successTimeUtc,
                    WalletAddress = withdrawal.Address,
                    TransactionHash = withdrawal.TxId,
                };

                if (withdrawalFees != null
                    && !string.IsNullOrWhiteSpace(historicalTrade.Symbol)
                    && withdrawalFees.ContainsKey(historicalTrade.Symbol))
                {
                    historicalTrade.FeeCommodity = historicalTrade.Symbol;
                    historicalTrade.FeeQuantity = withdrawalFees[historicalTrade.Symbol];
                }

                allTrades.Add(historicalTrade);
            }

            var depositHistory = GetNativeDepositHistory(cachePolicy);
            foreach(var deposit in depositHistory?.Data?.List ?? new List<BcDeposit>())
            {
                var nativeSymbol = deposit.Asset;
                var canon = _binanceMap.GetCanon(nativeSymbol);
                var symbol = !string.IsNullOrWhiteSpace(canon?.Symbol) ? canon.Symbol : nativeSymbol;

                var historicalTrade = new HistoricalTrade
                {
                    Symbol = symbol,
                    Quantity = deposit.Amount,
                    TradeType = TradeTypeEnum.Deposit,
                    TimeStampUtc = deposit.InsertTime.ToUniversalTime(),
                    WalletAddress = deposit.Address,
                    TransactionHash = deposit.TransactionId
                };

                allTrades.Add(historicalTrade);
            }

            return allTrades.OrderByDescending(item => item.TimeStampUtc).ToList();
        }

        public BinanceCcxtWithdrawalHistoryResponse GetCcxtWithdrawalHistory(CachePolicy cachePolicy)
        {
            var retriever = new Func<string>(() => _nodeUtil.GetWithdrawalHistory(CcxtExchangeName));
            var translator = new Func<string, BinanceCcxtWithdrawalHistoryResponse>(text =>
                !string.IsNullOrWhiteSpace(text)
                    ? JsonConvert.DeserializeObject<BinanceCcxtWithdrawalHistoryResponse>(text)
                    : null
            );
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

            var threshold = TimeSpan.FromMinutes(5);
            var collectionContext = new MongoCollectionContext(_dbContext, "binance--get-ccxt-withdrawal-history");

            var cacheResult = _cacheUtil.GetCacheableEx(ThrottleContext, retriever, collectionContext, threshold, cachePolicy, validator);
            return translator(cacheResult?.Contents);
        }

        public BcCallResult<BcWithdrawalList> GetNativeWithdrawalHistory(CachePolicy cachePolicy)
        {
            var nativeRetriever = new Func<BcCallResult<BcWithdrawalList>>(() =>
            {
                var result = _binanceClient.GetWithdrawHistory();
                if (result == null) { return null; }

                var res = new BcCallResult<BcWithdrawalList>
                {
                    Success = result.Success,
                    Error = BcError.FromModel(result.Error),
                    Data = new BcWithdrawalList
                    {
                        Success = result.Data.Success,
                        Message = result.Data.Message,
                        List = result.Data?.List?.Select(item =>
                            new BcWithdrawal
                            {
                                Address = item.Address,
                                Amount = item.Amount,
                                ApplyTime = item.ApplyTime,
                                Asset = item.Asset,
                                Id = item.Id,
                                Status = (BcWithdrawalStatus)item.Status,
                                TransactionId = item.TransactionId
                            })?.ToList()
                    }
                };

                return res;
            });

            var retriever = new Func<string>(() =>
            {
                var res = nativeRetriever();
                if (res == null) { return null; }
                return JsonConvert.SerializeObject(res);
            });

            var translator = new Func<string, BcCallResult<BcWithdrawalList>>(text =>
                !string.IsNullOrWhiteSpace(text) ? JsonConvert.DeserializeObject<BcCallResult<BcWithdrawalList>>(text) : null);

            var validator = new Func<string, bool>(text => {
                if (string.IsNullOrWhiteSpace(text)) { return false; }
                var translated = translator(text);
                return translated.Success && translated.Data.Success;
            });

            var threshold = TimeSpan.FromMinutes(5);
            var collectionContext = new MongoCollectionContext(_dbContext, "binance--get-withdrawal-history");

            var cacheResult = _cacheUtil.GetCacheableEx(ThrottleContext, retriever, collectionContext, threshold, cachePolicy, validator);
            if (cacheResult == null || string.IsNullOrWhiteSpace(cacheResult.Contents)) { return null; }
            return translator(cacheResult.Contents);
        }

        public BcCallResult<BcDepositList> GetNativeDepositHistory(CachePolicy cachePolicy)
        {
            var nativeRetriever = new Func<BcCallResult<BcDepositList>>(() =>
            {
                CallResult<BinanceDepositList> result = _binanceClient.GetDepositHistory();
                if (result == null) { return null; }

                var res = new BcCallResult<BcDepositList>
                {
                    Success = result.Success,
                    Error = BcError.FromModel(result.Error),
                    Data = new BcDepositList
                    {
                        Success = result.Data.Success,
                        Message = result.Data.Message,
                        List = result.Data?.List?.Select(item =>
                            new BcDeposit
                            {
                                Address = item.Address,
                                Amount = item.Amount,
                                InsertTime = item.InsertTime,
                                Asset = item.Asset,
                                Status = (BcDepositStatus)item.Status,
                                TransactionId = item.TransactionId
                            })?.ToList()
                    }
                };

                return res;
            });

            var retriever = new Func<string>(() =>
            {
                var res = nativeRetriever();
                if (res == null) { return null; }
                return JsonConvert.SerializeObject(res);
            });

            var translator = new Func<string, BcCallResult<BcDepositList>>(text =>
                !string.IsNullOrWhiteSpace(text) ? JsonConvert.DeserializeObject<BcCallResult<BcDepositList>>(text) : null);

            var validator = new Func<string, bool>(text => {
                if (string.IsNullOrWhiteSpace(text)) { return false; }
                var translated = translator(text);
                return translated.Success && translated.Data.Success;
            });

            var threshold = TimeSpan.FromMinutes(5);
            var collectionContext = new MongoCollectionContext(_dbContext, "binance--get-deposit-history");

            var cacheResult = _cacheUtil.GetCacheableEx(ThrottleContext, retriever, collectionContext, threshold, cachePolicy, validator);
            if (cacheResult == null || string.IsNullOrWhiteSpace(cacheResult.Contents)) { return null; }
            return translator(cacheResult.Contents);
        }

        public List<BcTrade> GetNativeUserTradeHistory(TradingPair tradingPair, CachePolicy cachePolicy)
        {
            var nativeSymbol = _binanceMap.ToNativeSymbol(tradingPair.Symbol);
            var nativeBaseSymbol = _binanceMap.ToNativeSymbol(tradingPair.BaseSymbol);

            var tradingPairText = $"{nativeSymbol.ToUpper()}{nativeBaseSymbol.ToUpper()}";

            var getter = new Func<BcCallResult<List<BcTrade>>>(() =>
            {
                var native = _binanceClient.GetMyTrades(tradingPairText);
                if (native == null) { return null; }

                var trades = native?.Data?.Select(item => BcTrade.FromModel(item)).ToList();
                return new BcCallResult<List<BcTrade>>
                {
                    Success = native.Success,
                    Error = BcError.FromModel(native.Error),
                    Data = trades
                };
            });
            var textGetter = new Func<string>(() => JsonConvert.SerializeObject(getter()));

            var validator =
                new Func<string, bool>(text =>
                {
                    if (string.IsNullOrWhiteSpace(text)) { return false; }

                    var callResult = JsonConvert.DeserializeObject<BcCallResult<List<BcTrade>>>(text);
                    return callResult != null && callResult.Success;
                });

            var collectionContext = new MongoCollectionContext(_dbContext, $"binance--get-trade-history-for-{tradingPairText}");
            var cacheResult = _cacheUtil.GetCacheableEx(ThrottleContext, textGetter, collectionContext, BinanceGetAllAssetsCacheThreshold, cachePolicy, validator);

            if (cacheResult == null || string.IsNullOrWhiteSpace(cacheResult.Contents)) { return new List<BcTrade>(); }
            var data = JsonConvert.DeserializeObject<BcCallResult<List<BcTrade>>>(cacheResult.Contents);
            if (data == null) { return new List<BcTrade>(); }
            if (!data.Success)
            {
                if (cachePolicy == CachePolicy.OnlyUseCache)
                {
                    return GetNativeUserTradeHistory(tradingPair, CachePolicy.ForceRefresh);
                }

                throw new ApplicationException(data.Error.Message);
            }

            return data.Data;
        }

        private bool ValidateBinanceResponse<T>(CallResult<T> result, string desc, Func<CallResult<T>, bool> additionalValidator = null)
        {
            if (result == null) { throw new ApplicationException($"Binance returned a null result from {desc}."); }
            if (!result.Success)
            {
                var error = new StringBuilder()
                    .AppendLine("Binance result indicated failure on GetAccountInfo().");

                if (result.Error != null)
                {
                    error.AppendLine($"Error Code: {result.Error.Code}");
                    error.AppendLine($"Error Message: {result.Error.Message}");
                }

                throw new ApplicationException(error.ToString());
            }

            if (result.Data == null) { throw new ApplicationException("Binance returned null response.Data on GetAccountInfo()"); }

            if (additionalValidator != null)
            {
                return additionalValidator(result);
            }

            return true;
        }

        private static readonly TimeSpan GetOpenOrdersTimeSpan = TimeSpan.FromMinutes(5);
        public AsOfWrapper<List<BinanceCcxtFetchOpenOrdersResponse>> GetCcxtOpenOrders(string nativeSymbol, string nativeBaseSymbol, CachePolicy cachePolicy)
        {
            var nativeTradingPair = new TradingPair(nativeSymbol, nativeBaseSymbol);
            var retriever = new Func<string>(() => _nodeUtil.GetNativeOpenOrders(Name, nativeTradingPair));
            var translator = new Func<string, List<BinanceCcxtFetchOpenOrdersResponse>>(text =>
            {
                return !string.IsNullOrWhiteSpace(text)
                    ? JsonConvert.DeserializeObject<List<BinanceCcxtFetchOpenOrdersResponse>>(text)
                    : null;
            });

            var validator = new Func<string, bool>(text =>
            {
                if (string.IsNullOrWhiteSpace(text)) { return false; }
                var translated = translator(text);
                return translated != null;
            });

            var collectionContext = new MongoCollectionContext(_dbContext, "binance--fetch-open-orders");
            var cacheResult = _cacheUtil.GetCacheableEx(ThrottleContext, retriever, collectionContext, GetOpenOrdersTimeSpan, cachePolicy, validator);

            return new AsOfWrapper<List<BinanceCcxtFetchOpenOrdersResponse>>
            {
                Data = translator(cacheResult?.Contents),
                AsOfUtc = cacheResult?.AsOf
            };
        }

        public void CancelAllOpenOrdersForTradingPair(TradingPair tradingPair)
        {
            _nodeUtil.CancelAllOpenOrdersForTradingPair(Name, tradingPair);
        }

        public HoldingInfo GetHoldings(CachePolicy cachePolicy)
        {
            var getter = new Func<CallResult<BinanceAccountInfo>>(() => _binanceClient.GetAccountInfo((long)TimeSpan.FromSeconds(20).TotalMilliseconds));
            const string Key = "GetAccountInfo";

            var validator = new Func<CallResult<BinanceAccountInfo>, bool>(result =>
            {
                if (result == null) { throw new ApplicationException("Biannce returned a null result from GetAccountInfo."); }
                if (!result.Success)
                {
                    var error = new StringBuilder()
                        .AppendLine("Binance result indicated failure on GetAccountInfo().");

                    if (result.Error != null)
                    {
                        error.AppendLine($"Error Code: {result.Error.Code}");
                        error.AppendLine($"Error Message: {result.Error.Message}");
                    }

                    throw new ApplicationException(error.ToString());
                }

                if (result.Data == null) { throw new ApplicationException("Binance returned null response.Data on GetAccountInfo()"); }
                if (result.Data.Balances == null) { throw new ApplicationException("Binance returned null response.Data.Balances on GetAccountInfo()"); }

                return true;
            });

            var accountInfo = _webCache.Get(getter, Key, validator, cachePolicy == CachePolicy.ForceRefresh ? true : false);
            validator(accountInfo);

            var holdingInfo = new HoldingInfo
            {
                TimeStampUtc = DateTime.UtcNow, //accountInfo.Data.UpdateTime;
                Holdings = accountInfo.Data.Balances.Select(item =>
                    new Holding
                    {
                        Asset = _binanceMap.ToCanonicalSymbol(item.Asset),
                        Total = item.Total,
                        Available = item.Free,
                        InOrders = item.Locked
                    }
                ).ToList()
            };

            return holdingInfo;
        }

        public class OrderBookEventContainer
        {
            public ObjectId Id { get; set; }

            public DateTime RequestTimeUtc { get; set; }
            public DateTime ResponseTimeUtc { get; set; }
            public bool WasSuccessful { get; set; }

            public string Symbol { get; set; }
            public string BaseSymbol { get; set; }

            public string TradingPair { get; set; }
            public OrderBook OrderBook { get; set; }
            public string Native { get; set; }
        }

        private static object Locker = new object();
        private static DateTime? LastReadTime;


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

        private static Random _random = new Random();

        private OrderBook NativeToCanon(BcCallResult<BcOrderBook> native)
        {
            if (native == null) { return null; }

            return new OrderBook
            {
                Asks = native.Data?.Asks?.Select(order => new Order { Price = order.Price, Quantity = order.Quantity })
                    .OrderBy(order => order.Price).ToList() ?? new List<Order>(),
                Bids = native.Data?.Bids?.Select(order => new Order { Price = order.Price, Quantity = order.Quantity })
                    .OrderByDescending(order => order.Price).ToList() ?? new List<Order>(),
            };
        }

        private BcCallResult<BcOrderBook> TranslateOrderBook(string orderBookContents)
        {
            return JsonConvert.DeserializeObject<BcCallResult<BcOrderBook>>(orderBookContents);
        }

        protected override string GetNativeOrderBookContents(string symbol, string baseSymbol)
        {
            if (string.IsNullOrWhiteSpace(symbol)) { throw new ArgumentException($"{nameof(symbol)} must not be null or whitespace."); }
            if (string.IsNullOrWhiteSpace(baseSymbol)) { throw new ArgumentException($"{nameof(baseSymbol)} must not be null or whitespace."); }

            var combo = $"{symbol}{baseSymbol}";

            var native = _binanceClient.GetOrderBook(combo);
            if (native == null) { return null; }

            var serializable = BcCallResult<BcOrderBook>.FromModel(native);
            if (serializable == null) { return null; }

            var contents = JsonConvert.SerializeObject(serializable);

            return contents;
        }

        public void UpdateGroupSnapShot()
        {
            const int MaxToApplyInOneShot = 100;

            bool keepRunning = true;

            // keep running while 
            while (keepRunning)
            {
                keepRunning = false;
                var existingGroupSnapShot = _orderBookGroupSnapShotCollection
                    .AsQueryable()
                    .OrderByDescending(item => item.LastOrderBookSnapShotId)
                    .FirstOrDefault();

                var updates =
                    existingGroupSnapShot != null
                    ? _orderBookSnapShotCollection
                        .AsQueryable()
                        .Where(item => item.Id > existingGroupSnapShot.LastOrderBookSnapShotId)
                        .OrderBy(item => item.Id)
                        .Take(MaxToApplyInOneShot)
                        .ToList()
                    : _orderBookSnapShotCollection
                        .AsQueryable()
                        .OrderBy(item => item.Id)
                        .Take(MaxToApplyInOneShot)
                        .ToList();

                if (updates == null || !updates.Any()) { return; }
                keepRunning = updates.Count == MaxToApplyInOneShot;

                OrderBookGroupSnapShot snapShotToInsert = null;
                foreach (var update in updates)
                {
                    snapShotToInsert = ApplyEvent(update, snapShotToInsert ?? existingGroupSnapShot);
                }

                if (snapShotToInsert != null)
                {
                    _orderBookGroupSnapShotCollection.InsertOne(snapShotToInsert);
                }
            }
        }

        private OrderBookGroupSnapShot ApplyEvent(OrderBookSnapShot eventContainer, OrderBookGroupSnapShot groupSnapShot)
        {
            if (eventContainer == null) { return null; }
            if (groupSnapShot != null && groupSnapShot.LastOrderBookSnapShotId >= eventContainer.Id) { return null; }

            var effectiveSnapShot = new OrderBookGroupSnapShot
            {
                LastOrderBookSnapShotId = eventContainer.Id,
                OrderBooks = groupSnapShot != null && groupSnapShot.OrderBooks != null
                ? groupSnapShot.OrderBooks
                : new List<OrderBookForTradingPair>()
            };

            var existingData =
                effectiveSnapShot.OrderBooks.SingleOrDefault(item =>
                string.Equals(item.TradingPair, eventContainer.TradingPair, StringComparison.InvariantCultureIgnoreCase));

            if (existingData != null)
            {
                existingData.OrderBook = eventContainer.OrderBook;
            }
            else
            {
                effectiveSnapShot.OrderBooks.Add(new OrderBookForTradingPair
                {
                    TradingPair = eventContainer.TradingPair,
                    OrderBook = eventContainer.OrderBook
                });
            }

            return effectiveSnapShot;
        }

        public class OrderBookSnapShot
        {
            public ObjectId Id { get; set; }

            public ObjectId LastEventId { get; set; }
            public DateTime RequestTimeUtc { get; set; }
            public DateTime ResponseTimeUtc { get; set; }
            public OrderBook OrderBook { get; set; }
            public string TradingPair { get; set; }
        }

        public class OrderBookForTradingPair
        {
            public string TradingPair { get; set; }
            public OrderBook OrderBook { get; set; }
        }

        public class OrderBookGroupSnapShot
        {
            public ObjectId Id { get; set; }
            public ObjectId LastOrderBookSnapShotId { get; set; }
            public List<OrderBookForTradingPair> OrderBooks { get; set; }
        }

        public Dictionary<string, decimal> GetWithdrawalFees(CachePolicy cachePolicy)
        {
            var nativeAssets = GetNativeAssets(cachePolicy);

            var dict = new Dictionary<string, decimal>(StringComparer.InvariantCultureIgnoreCase);
            foreach (var item in nativeAssets)
            {
                var nativeSymbol = item.assetCode;
                var canonicalSymbol = _binanceMap.ToCanonicalSymbol(nativeSymbol);
                dict[canonicalSymbol] = (decimal)item.transactionFee;
            }

            return dict;
        }

        private static TimeSpan BinanceGetAllAssetsCacheThreshold = TimeSpan.FromHours(2);

        private List<GetAllAssetsRespone> GetNativeAssets(CachePolicy cachePolicy)
        {
            const string Url = "https://www.binance.com/assetWithdraw/getAllAsset.html";
            var retriever = new Func<string>(() => _webUtil.Get(Url));
            var translator = new Func<string, List<GetAllAssetsRespone>>(text => JsonConvert.DeserializeObject<List<GetAllAssetsRespone>>(text));
            var validator = new Func<string, bool>(text => !string.IsNullOrWhiteSpace(text));

            var context = new MongoCollectionContext(_dbContext, "binance--get-all-assets");

            var cacheResult = _cacheUtil.GetCacheableEx(ThrottleContext, retriever, context, BinanceGetAllAssetsCacheThreshold, cachePolicy, validator);
            return translator(cacheResult.Contents);
        }

        private static TimeSpan ExchangeInfoThreshold = TimeSpan.FromHours(2);
        public BcExchangeInfo GetExchangeInfo(CachePolicy cachePolicy)
        {
            var retriever = new Func<string>(() => _webUtil.Get("https://api.binance.com/api/v1/exchangeInfo"));

            var translator = new Func<string, BcExchangeInfo>(text =>
            {
                if (string.IsNullOrWhiteSpace(text)) { return null; }
                return JsonConvert.DeserializeObject<BcExchangeInfo>(text);
            });

            var validator = new Func<string, bool>(text =>
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(text)) { return false; }
                    var translated = translator(text);
                    return translated != null && translated.Symbols != null && translated.Symbols.Any();
                }
                catch
                {
                    return false;
                }
            });

            var cacheUtil = new CacheUtil();
            var cacheResult = cacheUtil.GetCacheableEx(ThrottleContext, retriever, _exchangeInfoContext, ExchangeInfoThreshold, cachePolicy, validator);

            if (cacheResult == null || string.IsNullOrWhiteSpace(cacheResult.Contents))
            {
                return null;
            }

            return translator(cacheResult.Contents);
        }

        public List<CommodityForExchange> GetCommodities(CachePolicy cachePolicy = CachePolicy.OnlyUseCacheUnlessEmpty)
        {
            var nativeAssets = GetNativeAssets(cachePolicy);
            var withdrawalFees = GetWithdrawalFees(cachePolicy);
            
            var commodities = new List<CommodityForExchange>();

            foreach (var item in nativeAssets)
            {
                var nativeSymbol = item.assetCode;
                var canon = _binanceMap.GetCanon(nativeSymbol);
                var symbol = !string.IsNullOrWhiteSpace(canon?.Symbol) ? canon.Symbol : item.assetCode;
                var withdrawalFee = withdrawalFees.ContainsKey(symbol) ? withdrawalFees[symbol] : (decimal?)null;

                var commodity = new CommodityForExchange
                {
                    CanonicalId = canon?.Id,
                    Symbol = !string.IsNullOrWhiteSpace(canon?.Symbol) ? canon.Symbol : item.assetCode,
                    NativeSymbol = item.assetCode,
                    Name = !string.IsNullOrWhiteSpace(canon?.Name) ? canon.Name : item.assetName,
                    NativeName = item.assetName,

                    // there doesn't seem to be a matching param for this one.
                    // for now, living with the bad assumption that assets that can be withdrawn can also be depositied.
                    CanDeposit = item.enableWithdraw,
                    CanWithdraw = item.enableWithdraw, // true,
                    WithdrawalFee = withdrawalFee
                };            

                commodities.Add(commodity);
            }

            return commodities;
        }

        public List<TradingPair> GetTradingPairs(CachePolicy cachePolicy)
        {
            var exchangeInfo = GetExchangeInfo(cachePolicy);
            var nativeAssets = GetNativeAssets(cachePolicy);

            var tradingPairs = new List<TradingPair>();
            foreach (var nativeTradingPair in exchangeInfo.Symbols)
            {
                if (!string.Equals(nativeTradingPair.Status, "Trading", StringComparison.InvariantCultureIgnoreCase))
                {
                    continue;
                }

                decimal? priceTick = null;

                try
                {
                    var priceFilter = nativeTradingPair.Filters.SingleOrDefault(queryFilter => string.Equals(queryFilter.filterType, "PRICE_FILTER"));
                    priceTick = !string.IsNullOrWhiteSpace(priceFilter?.tickSize) ? decimal.Parse(priceFilter?.tickSize) : (decimal?)null;
                }
                catch (Exception exception)
                {
                    priceTick = null;
                    _log.Error(exception);
                }

                decimal? lotSize = null;
                try
                {
                    var lotFilter = nativeTradingPair.Filters.SingleOrDefault(queryFilter => string.Equals(queryFilter.filterType, "LOT_SIZE"));
                    lotSize = !string.IsNullOrWhiteSpace(lotFilter?.stepSize) ? decimal.Parse(lotFilter?.stepSize) : (decimal?)null;
                }
                catch (Exception exception)
                {
                    lotSize = null;
                    _log.Error(exception);
                }

                var nativeSymbol = nativeTradingPair.BaseAsset;
                var nativeBaseSymbol = nativeTradingPair.QuoteAsset;

                var nativeCommodity = nativeAssets.SingleOrDefault(item => string.Equals(item.assetCode, nativeSymbol, StringComparison.InvariantCultureIgnoreCase));
                var nativeCommodityName = nativeCommodity?.assetName;

                var nativeBaseCommodity = nativeAssets.SingleOrDefault(item => string.Equals(item.assetCode, nativeBaseSymbol, StringComparison.InvariantCultureIgnoreCase));
                var nativeBaseCommodityName = nativeBaseCommodity?.assetName;

                var canon = _binanceMap.GetCanon(nativeSymbol);
                var baseCanon = _binanceMap.GetCanon(nativeBaseSymbol);

                var tradingPair = new TradingPair
                {
                    CanonicalCommodityId = canon?.Id,
                    Symbol = !string.IsNullOrWhiteSpace(canon?.Symbol) ? canon.Symbol : nativeSymbol,
                    NativeSymbol = nativeSymbol,
                    CommodityName = !string.IsNullOrWhiteSpace(canon?.Name) ? canon.Name : nativeSymbol,
                    NativeCommodityName = !string.IsNullOrWhiteSpace(nativeCommodityName) ? nativeCommodityName : nativeSymbol,

                    CanonicalBaseCommodityId = baseCanon?.Id,
                    BaseSymbol = !string.IsNullOrWhiteSpace(baseCanon?.Symbol) ? baseCanon.Symbol : nativeBaseSymbol,
                    NativeBaseSymbol = nativeBaseSymbol,
                    BaseCommodityName = !string.IsNullOrWhiteSpace(baseCanon?.Name) ? baseCanon.Name : nativeBaseSymbol,
                    NativeBaseCommodityName = !string.IsNullOrWhiteSpace(nativeBaseCommodityName) ? nativeBaseCommodityName : nativeBaseSymbol,

                    LotSize = lotSize,
                    PriceTick = priceTick
                };
                
                tradingPairs.Add(tradingPair);
            }

            return tradingPairs;
        }

        public bool Withdraw(Commodity commodity, decimal quantity, DepositAddress address)
        {
            if (commodity == null) { throw new ArgumentNullException(nameof(commodity)); }
            if (address == null) { throw new ArgumentNullException(nameof(address)); }
            if (string.IsNullOrWhiteSpace(address.Address)) { throw new ArgumentNullException(nameof(address.Address)); }
            if (quantity <= 0) { throw new ArgumentOutOfRangeException(nameof(quantity), $"{nameof(quantity)} must be > 0."); }

            var canonicalSymbol = commodity.Symbol.Trim().ToUpper();
            var nativeSymbol = _binanceMap.ToNativeSymbol(canonicalSymbol);
            var effectiveAddress = address.Address.Trim();

            _log.Info($"About to withdraw {quantity} {canonicalSymbol} from {Name} to {effectiveAddress}.");
            try
            {
                var response = _binanceClient.Withdraw(nativeSymbol, effectiveAddress, quantity, null, nativeSymbol);
                if (response == null) { throw new ApplicationException($"Binance returned a null response when attempting to withdraw {quantity} {canonicalSymbol} from {Name} to {effectiveAddress}."); }
                if (!response.Success)
                {
                    var errorBuilder = new StringBuilder()
                        .AppendLine($"Binance returned a failure response when attempting to withdraw {quantity} {canonicalSymbol} from {Name} to {effectiveAddress}.");

                    if (response.Error != null)
                    {
                        errorBuilder.AppendLine($"Error Code: {response.Error.Code}");
                        if (!string.IsNullOrWhiteSpace(response.Error.Message))
                        {
                            errorBuilder.AppendLine("Error Message:");
                            errorBuilder.AppendLine(response.Error.Message);
                        }
                    }

                    throw new ApplicationException(errorBuilder.ToString());
                }

                _log.Info($"Successfully withdrew {quantity} {canonicalSymbol} from {Name} to {effectiveAddress}.");

                return true;
            }
            catch (Exception exception)
            {
                var errorText = new StringBuilder()
                    .AppendLine($"Failed to withdraw {quantity} {canonicalSymbol} from {Name} to {address}.")
                    .AppendLine("Exception:")
                    .AppendLine(exception.Message)
                    .ToString();

                return false;
            }
        }

        public bool BuyMarket(TradingPair tradingPair, decimal quantity)
        {
            var binanceSymbol = $"{tradingPair.Symbol.ToUpper()}{tradingPair.BaseSymbol.ToUpper()}";
            var result = _binanceClient.PlaceOrder(binanceSymbol, OrderSide.Buy, Binance.Net.Objects.OrderType.Market, quantity);

            if (result == null)
            {
                throw new ApplicationException($"Binance returned a null result when attempting to place a market purchase for {quantity} of {binanceSymbol}.");
            }

            if (!result.Success)
            {
                var error = new StringBuilder().AppendLine($"Binance indicated failure when attempting to place a market purchase for {quantity} of {binanceSymbol}.");
                if (result.Error != null && !string.IsNullOrWhiteSpace(result.Error.Message))
                {
                    error.AppendLine(result.Error.Message);
                }

                throw new ApplicationException(error.ToString());
            }

            return true;
        }

        public bool SellMarket(TradingPair tradingPair, decimal quantity)
        {
            if (tradingPair == null) { throw new ArgumentNullException(nameof(tradingPair)); }
            if (string.Equals(tradingPair.Symbol, tradingPair.BaseSymbol, StringComparison.InvariantCultureIgnoreCase)) { throw new ArgumentNullException("Coin cannot be sold for itself."); }
            if (quantity <= 0) { throw new ArgumentOutOfRangeException(nameof(quantity)); }

            var nativeTradingPair = _binanceMap.ToNativeTradingPair(tradingPair);

            var permittedBases = new List<string> { "ETH", "BTC", "BNB" };
            if (!permittedBases.Any(permittedBase => string.Equals(permittedBase, tradingPair.BaseSymbol)))
            {
                throw new NotImplementedException($"Market sell with base symbol {tradingPair.BaseSymbol} is not yet implemented.");
            }

            var holding = GetHolding(tradingPair.Symbol, CachePolicy.ForceRefresh);
            var effectiveQuantity = LimitDecimals(nativeTradingPair, holding.Available < quantity ? holding.Available : quantity);
            if (effectiveQuantity <= 0)
            {
                throw new ApplicationException($"There is no {tradingPair.Symbol} available to sell.");
            }

            var binanceSymbol = $"{nativeTradingPair.Symbol.ToUpper()}{nativeTradingPair.BaseSymbol.ToUpper()}";
            _log.Info($"About to sell {quantity} {binanceSymbol} at market on {Name}.");
            var result = _binanceClient.PlaceOrder(binanceSymbol, OrderSide.Sell, Binance.Net.Objects.OrderType.Market, effectiveQuantity);

            if (result.Success)
            {
                _log.Info($"Successfully sold {quantity} {binanceSymbol} at market on {Name}.");
                return true;
            }

            var error = new StringBuilder().AppendLine($"Binance indicated failure when attempting to place a market purchase for {quantity} of {binanceSymbol}.");
            if (result.Error != null && !string.IsNullOrWhiteSpace(result.Error.Message))
            {
                error.AppendLine(result.Error.Message);
            }

            throw new ApplicationException(error.ToString());
        }

        public bool BuyLimit(TradingPair tradingPair, decimal quantity, decimal price)
        {
            return BuyLimit(tradingPair, new QuantityAndPrice { Quantity = quantity, Price = price });
        }

        public bool BuyLimit(TradingPair tradingPair, QuantityAndPrice quantityAndPrice)
        {
            if (tradingPair == null) { throw new ArgumentNullException(nameof(tradingPair)); }
            if (quantityAndPrice == null) { throw new ArgumentNullException(nameof(quantityAndPrice)); }
            if (quantityAndPrice.Quantity <= 0) { throw new ArgumentOutOfRangeException(nameof(quantityAndPrice.Quantity)); }
            if (quantityAndPrice.Price <= 0) { throw new ArgumentOutOfRangeException(nameof(quantityAndPrice.Price)); }

            var nativeTradingPair = _binanceMap.ToNativeTradingPair(tradingPair);
            var binanceSymbol = $"{nativeTradingPair.Symbol.ToUpper()}{nativeTradingPair.BaseSymbol.ToUpper()}";

            var effectiveQuantity = LimitDecimals(nativeTradingPair, quantityAndPrice.Quantity);
            if (effectiveQuantity <= 0)
            {
                throw new ApplicationException($"Cannot buy less than the lot size.");
            }

            var result = _binanceClient.PlaceOrder(binanceSymbol, OrderSide.Buy, Binance.Net.Objects.OrderType.Limit, effectiveQuantity, null, quantityAndPrice.Price, TimeInForce.GoodTillCancel);

            if (result == null)
            {
                throw new ApplicationException($"Binance returned a null result when attempting to place a lmit purchase for {effectiveQuantity} of {binanceSymbol} at {quantityAndPrice.Price}.");
            }

            if (!result.Success)
            {
                var error = new StringBuilder().AppendLine($"Binance indicated failure when attempting to place a market purchase for {effectiveQuantity} of {binanceSymbol} at {quantityAndPrice.Price}.");
                if (result.Error != null && !string.IsNullOrWhiteSpace(result.Error.Message))
                {
                    error.AppendLine(result.Error.Message);
                }

                throw new ApplicationException(error.ToString());
            }

            return true;
        }

        public bool SellLimit(TradingPair tradingPair, decimal quantity, decimal price)
        {
            return SellLimit(tradingPair, new QuantityAndPrice { Quantity = quantity, Price = price });
        }

        public bool SellLimit(TradingPair tradingPair, QuantityAndPrice quantityAndPrice)
        {
            if (tradingPair == null) { throw new ArgumentNullException(nameof(tradingPair)); }
            if (quantityAndPrice == null) { throw new ArgumentNullException(nameof(quantityAndPrice)); }
            if (quantityAndPrice.Quantity <= 0) { throw new ArgumentOutOfRangeException(nameof(quantityAndPrice.Quantity)); }
            if (quantityAndPrice.Price <= 0) { throw new ArgumentOutOfRangeException(nameof(quantityAndPrice.Price)); }

            var nativeTradingPair = _binanceMap.ToNativeTradingPair(tradingPair);
            var binanceSymbol = $"{nativeTradingPair.Symbol.ToUpper()}{nativeTradingPair.BaseSymbol.ToUpper()}";

            var effectiveQuantity = LimitDecimals(nativeTradingPair, quantityAndPrice.Quantity);
            if (effectiveQuantity <= 0)
            {
                throw new ApplicationException($"Cannot sell less than the lot size.");
            }

            var result = _binanceClient.PlaceOrder(binanceSymbol, OrderSide.Sell, Binance.Net.Objects.OrderType.Limit, effectiveQuantity, null, quantityAndPrice.Price, TimeInForce.GoodTillCancel);
            if (result != null && !result.Success && result.Error != null && result.Error.Code == 3 && result.Error.Message != null && result.Error.Message.ToUpper().IndexOf("Timestamp for this request was 1000ms ahead of the server's time.".ToUpper()) != -1)
            {
                Thread.Sleep(TimeSpan.FromSeconds(2.5));
                result = _binanceClient.PlaceOrder(binanceSymbol, OrderSide.Sell, Binance.Net.Objects.OrderType.Limit, effectiveQuantity, null, quantityAndPrice.Price, TimeInForce.ImmediateOrCancel);
            }

            if (result == null)
            {
                throw new ApplicationException($"Binance returned a null result when attempting to place a lmit purchase for {effectiveQuantity} of {binanceSymbol} at {quantityAndPrice.Price}.");
            }

            if (!result.Success)
            {
                var error = new StringBuilder().AppendLine($"Binance indicated failure when attempting to place a market purchase for {effectiveQuantity} of {binanceSymbol} at {quantityAndPrice.Price}.");
                if (result.Error != null && !string.IsNullOrWhiteSpace(result.Error.Message))
                {
                    error.AppendLine(result.Error.Message);
                }

                throw new ApplicationException(error.ToString());
            }

            return true;
        }

        private Holding GetHolding(string canonicalSymbol, CachePolicy cachePolicy)
        {
            var holdings = GetHoldings(cachePolicy);
            var match = holdings.Holdings.SingleOrDefault(item => string.Equals(item.Symbol, canonicalSymbol, StringComparison.InvariantCultureIgnoreCase));

            return match;
        }

        public List<BcSymbol> GetBinanceTradingPairsForSymbol(string symbol, CachePolicy cachePolicy)
        {
            var nativeSymbol = _binanceMap.ToNativeSymbol(symbol);
            var exchangeInfo = GetExchangeInfo(CachePolicy.OnlyUseCacheUnlessEmpty);

            var matches = exchangeInfo.Symbols.Where(item => string.Equals(item.BaseAsset, nativeSymbol, StringComparison.InvariantCultureIgnoreCase))
                .ToList();

            return matches;
        }

        public BcSymbol GetBinanceTradingPairFromCanonicalTradingPair(TradingPair tradingPair, CachePolicy cachePolicy)
        {
            var nativeSymbol = _binanceMap.ToNativeSymbol(tradingPair.Symbol);
            var nativeBaseSymbol = _binanceMap.ToNativeSymbol(tradingPair.BaseSymbol);

            var exchangeInfo = GetExchangeInfo(cachePolicy);

            var match = exchangeInfo.Symbols
                .SingleOrDefault(item => 
                string.Equals(item.BaseAsset, nativeSymbol, StringComparison.InvariantCultureIgnoreCase)
                && string.Equals(item.QuoteAsset, nativeBaseSymbol, StringComparison.InvariantCultureIgnoreCase));

            return match;
        }

        public decimal LimitDecimals(NativeTradingPair nativeTradingPair, decimal quantity)
        {
            var binanceSymbol = $"{nativeTradingPair.Symbol.ToUpper()}{nativeTradingPair.BaseSymbol.ToUpper()}";

            var exchangeInfo = GetExchangeInfo(CachePolicy.OnlyUseCacheUnlessEmpty);
            var matchingSymbol = exchangeInfo.Symbols.Where(item => string.Equals(item.Symbol, binanceSymbol, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
            if (matchingSymbol == null) { exchangeInfo = GetExchangeInfo(CachePolicy.ForceRefresh); }
            if (matchingSymbol == null) { throw new ApplicationException($"No matching symbol found for {binanceSymbol}."); }

            var lotSizeFilter = matchingSymbol.Filters.FirstOrDefault(queryFilter => string.Equals(queryFilter.filterType, "LOT_SIZE", StringComparison.InvariantCultureIgnoreCase));
            if(lotSizeFilter == null) { return quantity; }
            var stepSizeText = lotSizeFilter.stepSize;
            if (string.IsNullOrWhiteSpace(stepSizeText)) { return quantity; }

            double stepSize;
            stepSize = double.Parse(stepSizeText);

            var precision = -((int)Math.Log10(stepSize));
            var factor = (decimal)Math.Pow(10.0d, precision);

            return Math.Truncate(quantity * factor) / factor;
        }

        public decimal? GetWithdrawalFee(string symbol, CachePolicy cachePolicy)
        {
            var fees = GetWithdrawalFees(cachePolicy);
            return fees.ContainsKey(symbol) ? fees[symbol] : (decimal?)null;
        }

        public List<DepositAddressWithSymbol> GetDepositAddresses(CachePolicy cachePolicy)
        {
            var commodities = GetCommodities(cachePolicy);
            var symbols = commodities.Select(item => item.Symbol)
                .Distinct().ToList();

            var addresses = symbols
            .Select(symbol =>
            {
                var depositAddress = GetDepositAddress(symbol, cachePolicy);
                return new DepositAddressWithSymbol
                {
                    Symbol = symbol,
                    Address = depositAddress.Address,
                    Memo = depositAddress.Memo
                };
            })
            .Where(item => item != null)
            .ToList();

            var coinsWithoutAddresses = symbols.Where(item => !addresses.Any(addr => string.Equals(addr.Symbol, item, StringComparison.InvariantCultureIgnoreCase)))
                .ToList();

            var coinsWithAddresses = symbols.Where(item => addresses.Any(addr => string.Equals(addr.Symbol, item, StringComparison.InvariantCultureIgnoreCase)))
                .ToList();

            return addresses;
        }

        public DepositAddress GetDepositAddress(string symbol, CachePolicy cachePolicy)
        {
            if (string.IsNullOrWhiteSpace(symbol)) { throw new ArgumentNullException(nameof(symbol)); }
            var nativeSymbol = _binanceMap.ToNativeSymbol(symbol);

            var retriever = new Func<string>(() =>
            {
                return _nodeUtil.GetDepositAddress("binance", nativeSymbol);
            });

            var translator = new Func<string, BinanceCcxtDepositAddress>(text =>
                !string.IsNullOrWhiteSpace(text)
                ? JsonConvert.DeserializeObject<BinanceCcxtDepositAddress>(text)
                : null);

            var validator = new Func<string, bool>(text =>
            {
                if (string.IsNullOrWhiteSpace(text)) { return false; }
                var item = translator(text);
                return item != null && item.Info != null && item.Info.Success && !string.IsNullOrWhiteSpace(item.Address);
            });

            var context = new MongoCollectionContext(_dbContext, $"binance--get-deposit-address--{nativeSymbol}");
            var threshold = TimeSpan.FromDays(1);
            var cacheResponse = _cacheUtil.GetCacheableEx(ThrottleContext, retriever, context, threshold, cachePolicy, validator);
            
            if (cacheResponse == null || string.IsNullOrWhiteSpace(cacheResponse.Contents))
            {
                return null;
            }

            if (!validator(cacheResponse.Contents)) { return null; }

            var response = translator(cacheResponse.Contents);
            
            return new DepositAddress { Address = response.Address };
        }

        public void SetDepositAddress(DepositAddress depositAddress)
        {
            throw new NotImplementedException();
        }

        private string ToCanonicalSymbol(
            string nativeSymbol,
            List<Commodity> allCanon,
            List<CommodityMapItem> commodityMap)
        {
            var mapItem = commodityMap.SingleOrDefault(item => string.Equals(item.NativeSymbol, nativeSymbol, StringComparison.InvariantCultureIgnoreCase));
            if (mapItem == null) { return nativeSymbol; }
            var canon = allCanon.ById(mapItem.CanonicalId);
            return !string.IsNullOrWhiteSpace(canon?.Symbol)
                ? canon.Symbol
                : nativeSymbol;
        }

        private string ToNativeSymbol(
            string canonicalSymbol,
            List<Commodity> allCanon,
            List<CommodityMapItem> commodityMap)
        {
            var canonWithMapItem = commodityMap.Select(item =>
            {
                var canon = allCanon.Single(queryCanon => queryCanon.Id == item.CanonicalId);
                return new { MapItem = item, Canon = canon };
            });

            var match = canonWithMapItem.SingleOrDefault(item =>
            {
                return string.Equals(item.Canon.Symbol, canonicalSymbol, StringComparison.InvariantCultureIgnoreCase);
            });

            return match != null
                ? match.MapItem.NativeSymbol
                : canonicalSymbol;
        }

        private NativeTradingPair ToNativeTradingPair(
            TradingPair tradingPair,
            List<Commodity> allCanon,
            List<CommodityMapItem> commodityMap)
        {
            var nativeSymbol = ToNativeSymbol(tradingPair.Symbol, allCanon, commodityMap);
            var nativeBaseSymbol = ToNativeSymbol(tradingPair.BaseSymbol, allCanon, commodityMap);
            return new NativeTradingPair { Symbol = nativeSymbol, BaseSymbol = nativeBaseSymbol };
        }

        protected override OrderBook ToOrderBook(string text, DateTime? asOf)
        {
            var native = TranslateOrderBook(text);

            var result = NativeToCanon(native);
            if (result != null) { result.AsOf = asOf; }

            return result;
        }

        public List<OpenOrdersForTradingPair> GetOpenOrdersV2()
        {
            throw new NotImplementedException();
        }

        public OpenOrdersWithAsOf GetOpenOrdersForTradingPairV2(string symbol, string baseSymbol, CachePolicy cachePolicy)
        {
            var sideDictionary = new Dictionary<string, trade_model.OrderType>(StringComparer.InvariantCultureIgnoreCase)
            {
                { "buy", trade_model.OrderType.Bid },
                { "sell", trade_model.OrderType.Ask }
            };

            var nativeSymbol = _binanceMap.ToNativeSymbol(symbol);
            var nativeBaseSymbol = _binanceMap.ToNativeSymbol(baseSymbol);
            var nativeOpenOrders = GetCcxtOpenOrders(nativeSymbol, nativeBaseSymbol, cachePolicy);

            return new OpenOrdersWithAsOf
            {
                AsOfUtc = nativeOpenOrders?.AsOfUtc,
                OpenOrders = (nativeOpenOrders?.Data ?? new List<BinanceCcxtFetchOpenOrdersResponse>())
                .Select(native =>
                {
                    var aggregateId = new BinanceAggregateOrderId
                    {
                        Id = long.Parse(native.Id),
                        //Symbol = native.Symbol
                        Symbol = nativeSymbol,
                        BaseSymbol = nativeBaseSymbol
                    };

                    var aggregateIdText = JsonConvert.SerializeObject(aggregateId);

                    return new OpenOrder
                    {
                        OrderId = aggregateIdText,
                        OrderType = sideDictionary.ContainsKey(native.Side) ? sideDictionary[native.Side] : trade_model.OrderType.Unknown,
                        Price = native.Price,
                        Quantity = native.Amount
                    };
                }).ToList()
            };
        }

        private class BinanceAggregateOrderId
        {
            public long Id { get; set; }
            public string Symbol { get; set; }
            public string BaseSymbol { get; set; }
        }

        public void CancelOrder(string orderId)
        {
            var aggregateOrderId = JsonConvert.DeserializeObject<BinanceAggregateOrderId>(orderId);

            var combo = $"{aggregateOrderId.Symbol.ToUpper()}{aggregateOrderId.BaseSymbol.ToUpper()}";
            var callResult = _binanceClient.CancelOrder(combo, aggregateOrderId.Id);

            if (!callResult.Success)
            {
                throw new ApplicationException($"Binance -- failed to cancel order \"{orderId}\".");
            }
        }
    }
}
