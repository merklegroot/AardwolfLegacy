using date_time_lib;
using idex_data_lib;
using idex_integration_lib.Models;
using idex_integration_lib.Res;
using log_lib;
using mongo_lib;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using parse_lib;
using res_util_lib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using trade_lib;
using cache_lib.Models;
using trade_model;
using trade_res;
using web_util;
using cache_lib;
using trade_lib.Cache;
using config_client_lib;
using idex_client_lib;

namespace idex_integration_lib
{
    // https://github.com/AuroraDAO/idex-api-docs
    public class IdexIntegration : IIdexIntegration
    {
        private const string RelayHost = "206.189.197.96";
        private const int RelayPort = 3050;

        private const string DatabaseName = "idex";
        private static readonly TimeSpan ThrottleThreshold = TimeSpan.FromSeconds(7.5);
        private static readonly TimeSpan RelayThrottleThreshold = TimeSpan.FromSeconds(3.5);

        private readonly IdexClient _idexClient;

        private static ThrottleContext ThrottleContext = new ThrottleContext
        {
            ThrottleThreshold = ThrottleThreshold,
            Locker = new object()
        };

        private static ThrottleContext RelayThrottleContext = new ThrottleContext
        {
            ThrottleThreshold = RelayThrottleThreshold,
            Locker = new object()
        };

        private const string EmptyToken = "0x0000000000000000000000000000000000000000";

        private static readonly TimeSpan OrderBookThreshold = TimeSpan.FromMinutes(10);
        private static readonly TimeSpan OpenOrdersThreshold = TimeSpan.FromMinutes(2.5);
        private static readonly TimeSpan LowVolumeOrderBookThreshold = TimeSpan.FromMinutes(60);
        private static readonly TimeSpan BadGatewayThreshold = TimeSpan.FromMinutes(15);

        private readonly IWebUtil _webUtil;
        private readonly IIdexHoldingsRepo _idexHoldingsRepo;
        private readonly IIdexOrderBookRepo _idexOrderBookRepo;
        private readonly IIdexOpenOrdersRepo _idexOpenOrdersRepo;
        private readonly IIdexHistoryRepo _idexHistoryRepo;
        private readonly ILogRepo _log;

        private readonly ISimpleWebCache _webCache;        
        private readonly IMongoCollection<EventContainerWithContext<IdexCurrencyContext, List<IdexCurrency>>> _currencyCollection;
        private readonly IMongoCollection<BadGatewayEvent> _badGatewayCollecetion;

        private readonly IConfigClient _configClient;

        private IMongoDatabaseContext DbContext
        {
            get { return new MongoDatabaseContext(_configClient.GetConnectionString(), DatabaseName); }
        }

        private Dictionary<string, string> _reverseIdexSynonyms;

        private readonly CacheUtil _cacheUtil = new CacheUtil();

        public IdexIntegration(
            IWebUtil webUtil,
            IConfigClient configClient,
            IIdexHoldingsRepo idexHoldingsRepo,
            IIdexOrderBookRepo idexOrderBookRepo,
            IIdexOpenOrdersRepo idexOpenOrdersRepo,
            IIdexHistoryRepo idexHistoryRepo,
            IIdexClient idexClient,
            ILogRepo log)
        {
            _webUtil = webUtil;
            _configClient = configClient;
            _idexHoldingsRepo = idexHoldingsRepo;
            _idexOrderBookRepo = idexOrderBookRepo;
            _idexOpenOrdersRepo = idexOpenOrdersRepo;
            _idexHistoryRepo = idexHistoryRepo;

            _log = log;

            _reverseIdexSynonyms = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
            foreach (var key in _idexSynonyms.Keys)
            {
                var value = _idexSynonyms[key];
                _reverseIdexSynonyms[value] = key;
            }

            var context = new MongoCollectionContext(DbContext, "idex-web-cache");
            _webCache = new SimpleWebCache(webUtil, context, "idex");

            var currencyContext = new MongoCollectionContext(DbContext, "idex-currency");
            _currencyCollection = currencyContext.GetCollection<EventContainerWithContext<IdexCurrencyContext, List<IdexCurrency>>>();

            var badGatewayContext = new MongoCollectionContext(DbContext, "idex-bad-gateway");
            _badGatewayCollecetion = badGatewayContext.GetCollection<BadGatewayEvent>();
        }

        public bool UseRelay { get; set; }

        private TimeSpan GetTokenThreshold(string symbol)
        {
            return FityRandom(OrderBookThreshold);
        }

        private static Random _random = new Random();

        private TimeSpan FityRandom(TimeSpan original)
        {
            var pinned = original.TotalMilliseconds / 2.0d;
            var youCantContainThis = (original.TotalMilliseconds) * _random.NextDouble();
            var totalMilliseconds = pinned + youCantContainThis;

            return TimeSpan.FromMilliseconds(totalMilliseconds);
        }

        public class BadGatewayEvent
        {
            public ObjectId Id { get; set; }
            public DateTime TimeStampUtc { get; set; }
            public string Message { get; set; }
            public string ExceptionType { get; set; }
            public string StackTrace { get; set; }
            public string TradingPair { get; set; }
        }

        private Dictionary<string, string> _idexSynonyms = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase)
        {
            { "CAT", "BlockCAT (CAT)" }
        };

        public List<CommodityForExchange> GetCommodities(CachePolicy cachePolicy = CachePolicy.AllowCache)
        {
            var currencies = GetNativeCurrencies(cachePolicy);

            var iggies = new List<string> { "ETH", "BCD" };

            return currencies
                .Where(item => !iggies.Any(iggy => string.Equals(item.Symbol, iggy, StringComparison.InvariantCultureIgnoreCase)))
                .Select(item =>
                {
                    var canonSymbol = FromIdexSymbol(item.Symbol);
                    var canon = CommodityRes.BySymbolAndContract(canonSymbol, item.Address);
                    var name = canon != null && !string.IsNullOrWhiteSpace(canon.Name) ? canon.Name : item.Name;

                    var commodity = new CommodityForExchange
                    {
                        CanonicalId = canon?.Id,
                        Symbol = canonSymbol,
                        NativeSymbol = item.Symbol,
                        Name = name,
                        NativeName = item.Name,
                        ContractAddress = item.Address
                    };

                    return commodity;
                })
                .OrderBy(item => item.Symbol)
                .ToList();
        }

        public HoldingInfo GetHoldings(CachePolicy cachePolicy)
        {
            var native = GetNativeHoldings(cachePolicy);
            var holdingInfo = new HoldingInfo
            {
                TimeStampUtc = DateTime.UtcNow,
                Holdings = new List<Holding>()
            };
            if (native == null) { return holdingInfo; }

            foreach (var key in native.Keys)
            {
                var item = native[key];
                var holding = new Holding();
                holding.Asset = key;
                holding.Available = (decimal)item.Available;
                holding.InOrders = (decimal)item.OnOrders;
                holding.Total = holding.Available + holding.InOrders;

                holdingInfo.Holdings.Add(holding);
            }

            return holdingInfo;
        }

        public IdexBalance GetNativeHoldings(CachePolicy cachePolicy)
        {
            var retriever = new Func<string>(() => GetHoldingsFromApi());
            var translator = new Func<string, IdexBalance>(text =>           
                !string.IsNullOrWhiteSpace(text)
                    ? JsonConvert.DeserializeObject<IdexBalance>(text)
                    : new IdexBalance()
            );
            var validator = new Func<string, bool>(text =>
            {
                if (string.IsNullOrWhiteSpace(text)) { return false; }
                translator(text);

                return true;
            });

            var threshold = TimeSpan.FromMinutes(5);
            var collectionContext = new MongoCollectionContext(DbContext, "idex--returnCompleteBalances");

            var cacheResult = _cacheUtil.GetCacheableEx(ThrottleContext, retriever, collectionContext, threshold, cachePolicy, validator);

            return translator(cacheResult?.Contents);
        }

        public string GetHoldingsFromApi()
        {
            var address = _configClient.GetMewWalletAddress();
            var payload = "{\"address\":\"" + address + "\"}";

            const string Url = "https://api.idex.market/returnCompleteBalances";

            return _webUtil.Post(Url, payload);
        }

        private int GetDecimalsForNativeSymbol(string nativeSymbol, CachePolicy cachePolicy)
        {
            const int DefaultDecimals = 18;

            var currency = GetNativeCurrency(nativeSymbol, cachePolicy);
            if (currency != null) { return currency.Decimals; }

            return DefaultDecimals;
        }

        public class IdexOrderBookContext
        {
            public TradingPair TradingPair { get; set; }
            public string Url { get; set; }
            public string Payload { get; set; }
        }

        public class IdexCurrencyContext
        {
            public string Url { get; set; }
        }

        private string RequestNativeOrderBook(string nativeSymbol, string nativeBaseSymbol)
        {
            var json = $"{{ \"selectedMarket\": \"{nativeBaseSymbol}\", \"tradeForMarket\": \"{nativeSymbol}\" }}";
            const string Url = "https://api.idex.market/returnOrderBookForMarket";

            return _webUtil.Post(Url, json);
        }

        public string RequestRelayedNativeOrderBook(string nativeSymbol, string nativeBaseSymbol)
        {
            ServicePointManager.ServerCertificateValidationCallback += delegate (object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
            {
                return true;
            };

            var req = WebRequest.CreateHttp($"https://{RelayHost}:{RelayPort}/data");
            req.Method = "POST";
            req.ContentType = "application/json";
            var reqStream = req.GetRequestStream();
            var writer = new StreamWriter(reqStream);
            var json = $"{{ \"symbol\": \"{nativeSymbol}\", \"baseSymbol\": \"{nativeBaseSymbol}\" }}";
            writer.Write(json);
            writer.Flush();

            var resp = req.GetResponse();
            var respStream = resp.GetResponseStream();
            var reader = new StreamReader(respStream);
            var result = reader.ReadToEnd();

            return result;
        }

        private class NativeOrderBookWithAsOf
        {
            public List<IdexOrderBookItem> Orders { get; set; }
            public DateTime? AsOfUtc { get; set; }
        }

        private NativeOrderBookWithAsOf GetNativeOrderBook(string nativeSymbol, string nativeBaseSymbol, CachePolicy cachePolicy)
        {
            var retriever = UseRelay
                ? new Func<string>(() => RequestRelayedNativeOrderBook(nativeSymbol, nativeBaseSymbol))
                : new Func<string>(() => RequestNativeOrderBook(nativeSymbol, nativeBaseSymbol));

            var translator = new Func<string, List<IdexOrderBookItem>>(text =>
                !string.IsNullOrWhiteSpace(text)
                    ? JsonConvert.DeserializeObject<List<IdexOrderBookItem>>(text)
                    : new List<IdexOrderBookItem>()
            );

            var validator = new Func<string, bool>(text =>
            {
                if (string.IsNullOrWhiteSpace(text)) { return false; }
                return translator(text)?.Any() ?? false;
            });

            var databaseKey = $"{nativeSymbol.ToUpper()}-{nativeBaseSymbol.ToUpper()}";
            var collectionContext = new MongoCollectionContext(DbContext, $"idex--order-book");

            var threshold = TimeSpan.FromMinutes(10);

            var throttleContext =
                UseRelay ? RelayThrottleContext : ThrottleContext;

            var cacheResult = _cacheUtil.GetCacheableEx(throttleContext, retriever, collectionContext, threshold, cachePolicy, validator, null, databaseKey);

            return new NativeOrderBookWithAsOf
            {
                Orders = translator(cacheResult?.Contents),
                AsOfUtc = cacheResult?.AsOf
            };
        }

        public IdexExtendedOrderBook GetExtendedOrderBook(TradingPair tradingPair, CachePolicy cachePolicy)
        {
            var nativeSymbol = ToIdexSymbol(tradingPair.Symbol);
            var nativeBaseSymbol = ToIdexSymbol(tradingPair.BaseSymbol);

            var nativeOrderBook = GetNativeOrderBook(nativeSymbol, nativeBaseSymbol, cachePolicy);
            return NativeOrderBookToExtendedCanonicalOrderBook(nativeSymbol, nativeBaseSymbol, nativeOrderBook);
        }

        public OrderBook GetOrderBook(TradingPair tradingPair, CachePolicy cachePolicy)
        {
            var nativeSymbol = ToIdexSymbol(tradingPair.Symbol);
            var nativeBaseSymbol = ToIdexSymbol(tradingPair.BaseSymbol);

            var nativeOrderBook = GetNativeOrderBook(nativeSymbol, nativeBaseSymbol, cachePolicy);
            return NativeOrderBookToCanonicalOrderBook(nativeSymbol, nativeBaseSymbol, nativeOrderBook);
        }

        private IdexExtendedOrderBook NativeOrderBookToExtendedCanonicalOrderBook(string nativeSymbol, string nativeBaseSymbol, NativeOrderBookWithAsOf nativeOrderBookWithAsOf)
        {
            var currencyDecimals = GetDecimalsForNativeSymbol(nativeSymbol, CachePolicy.OnlyUseCacheUnlessEmpty);
            var baseDecimals = GetDecimalsForNativeSymbol(nativeBaseSymbol, CachePolicy.OnlyUseCacheUnlessEmpty);
            double symbolFactor = 1.0d / Math.Pow(10.0, currencyDecimals);
            double baseSymbolFactor = 1.0d / Math.Pow(10.0, baseDecimals);

            var asks = new List<IdexExtendedOrder>();
            var bids = new List<IdexExtendedOrder>();

            var nativeOrderBook = nativeOrderBookWithAsOf?.Orders;
            var orderBook = new IdexExtendedOrderBook
            {
                Asks = (nativeOrderBook ?? new List<IdexOrderBookItem>()).
                    Where(item => !string.Equals(item.TokenSell, "0x0000000000000000000000000000000000000000", StringComparison.InvariantCultureIgnoreCase))
                    .Select(item => new IdexExtendedOrder
                    {
                        NativeSymbol = nativeSymbol,
                        NativeBaseSymbol = nativeBaseSymbol,
                        ContractAddress = item.TokenSell,
                        BaseContractAddress = item.TokenBuy,
                        Id = item.Id,
                        Hash = item.Hash,
                        User = item.User,
                        CreatedAt = item.CreatedAt,
                        UpdatedAt = item.UpdatedAt,
                        Quantity = (decimal)(symbolFactor * item.AmountSell),
                        Price = (decimal)(item.AmountSell > 0
                        ? (baseSymbolFactor * item.AmountBuy) / (symbolFactor * item.AmountSell)
                        : 0)
                    })
                    .OrderBy(item => item.Price)
                    .ToList(),
                Bids = nativeOrderBook.
                    Where(item => !string.Equals(item.TokenBuy, "0x0000000000000000000000000000000000000000", StringComparison.InvariantCultureIgnoreCase))
                    .Select(item => new IdexExtendedOrder
                    {
                        NativeSymbol = nativeSymbol,
                        NativeBaseSymbol = nativeBaseSymbol,
                        ContractAddress = item.TokenSell,
                        BaseContractAddress = item.TokenBuy,
                        Id = item.Id,
                        Hash = item.Hash,
                        User = item.User,
                        CreatedAt = item.CreatedAt,
                        UpdatedAt = item.UpdatedAt,
                        Quantity = (decimal)(symbolFactor * item.AmountBuy),
                        Price = (decimal)(item.AmountBuy > 0
                        ? (baseSymbolFactor * item.AmountSell) / (symbolFactor * item.AmountBuy)
                        : 0)
                    })
                    .OrderByDescending(item => item.Price)
                    .ToList()
            };


            return orderBook;
        }

        private OrderBook NativeOrderBookToCanonicalOrderBook(string nativeSymbol, string nativeBaseSymbol, NativeOrderBookWithAsOf nativeOrderBookWithAsOf)
        {
            var extended = NativeOrderBookToExtendedCanonicalOrderBook(nativeSymbol, nativeBaseSymbol, nativeOrderBookWithAsOf);
            if (extended == null) { return null; }
            var orderBook = new OrderBook();
            orderBook.AsOf = nativeOrderBookWithAsOf?.AsOfUtc;
            
            if (extended.Asks != null)
            {
                orderBook.Asks = extended.Asks.Select(item => new Order
                {
                    Price = item.Price,
                    Quantity = item.Quantity
                }).ToList();
            }

            if (extended.Bids != null)
            {
                orderBook.Bids = extended.Bids.Select(item => new Order
                {
                    Price = item.Price,
                    Quantity = item.Quantity
                }).ToList();
            }

            return orderBook;
        }

        public OrderBook GetOrderBookOld(TradingPair passedInTradingPair, CachePolicy cachePolicy)
        {
            var tradingPair = new TradingPair(ToIdexSymbol(passedInTradingPair.Symbol), ToIdexSymbol(passedInTradingPair.BaseSymbol));

            var json = $"{{ \"selectedMarket\": \"{tradingPair.BaseSymbol}\", \"tradeForMarket\": \"{tradingPair.Symbol}\" }}";

            const string Url = "https://api.idex.market/returnOrderBookForMarket";

            var orderBookContext = new MongoCollectionContext(DbContext, $"idex-order-book-{passedInTradingPair}");
            var orderBookCollection = orderBookContext.GetCollection<EventContainerWithContext<IdexOrderBookContext, List<IdexOrderBookItem>>>();

            var latestStoredItem =
                (cachePolicy != CachePolicy.ForceRefresh)
                ? orderBookCollection.AsQueryable()
                    .Where(item => item.Context.Payload == json)
                    .OrderByDescending(item => item.Id)
                    .FirstOrDefault()
                    : null;

            List<IdexOrderBookItem> nativeOrderBook = null;

            bool shouldGetFromWeb = true;
            var currentTime = DateTime.UtcNow;

            var thresholdForSymbol = GetTokenThreshold(tradingPair.Symbol);

            if (latestStoredItem != null)
            {
                var timeSince = currentTime - latestStoredItem.StartTimeUtc;
                Console.WriteLine($"It's been {timeSince.ToReadableValue()} since the value was read from the web.");

                if ((latestStoredItem.Data?.Count() ?? 0) < 10) { thresholdForSymbol = LowVolumeOrderBookThreshold; }
            }

            if (latestStoredItem == null)
            {
                Console.WriteLine("Idex -- Didn't find anything cached in the database. ");
            }
            else if (string.IsNullOrWhiteSpace(latestStoredItem.Raw))
            {
                Console.WriteLine("Idex -- Found cached data, but it was empty.");
            }
            else if ((currentTime - latestStoredItem.StartTimeUtc) < thresholdForSymbol)
            {
                var timeSince = currentTime - latestStoredItem.StartTimeUtc;

                Console.WriteLine($"Idex -- The time since {tradingPair} was read from the web is less than the threshold. ({thresholdForSymbol.ToReadableValue()}).");
                Console.WriteLine("Going with the cached value.");

                nativeOrderBook = latestStoredItem.Data;
                shouldGetFromWeb = false;
            }

            if (cachePolicy == CachePolicy.OnlyUseCache)
            {
                shouldGetFromWeb = false;
            }

            if (shouldGetFromWeb)
            {
                Console.WriteLine("Reading value from the web...");

                try
                {
                    var response = HttpPost(Url, json);

                    nativeOrderBook = !string.IsNullOrWhiteSpace(response.contents)
                        ? JsonConvert.DeserializeObject<List<IdexOrderBookItem>>(response.contents)
                        : new List<IdexOrderBookItem>();

                    var context = new IdexOrderBookContext
                    {
                        TradingPair = tradingPair,
                        Url = Url,
                        Payload = json
                    };

                    var ev = new EventContainerWithContext<IdexOrderBookContext, List<IdexOrderBookItem>>
                    {
                        StartTimeUtc = response.startTime,
                        EndTimeUtc = response.endTime,
                        Data = nativeOrderBook,
                        Context = context,
                        Raw = response.contents
                    };

                    orderBookCollection.InsertOne(ev);
                }
                catch
                {
                    nativeOrderBook = latestStoredItem.Data;
                }
            }

            var currencyDecimals = GetDecimalsForNativeSymbol(tradingPair.Symbol, cachePolicy);
            var baseDecimals = GetDecimalsForNativeSymbol(tradingPair.BaseSymbol, cachePolicy);
            double symbolFactor = 1.0d / Math.Pow(10.0, currencyDecimals);
            double baseSymbolFactor = 1.0d / Math.Pow(10.0, baseDecimals);

            var asks = new List<Order>();
            var bids = new List<Order>();

            var orderBook = new OrderBook
            {
                Asks = nativeOrderBook.
                    Where(item => !string.Equals(item.TokenSell, "0x0000000000000000000000000000000000000000", StringComparison.InvariantCultureIgnoreCase))
                    .Select(item => new Order
                    {
                        Quantity = (decimal)(symbolFactor * item.AmountSell),
                        Price = (decimal)(item.AmountSell > 0
                        ? (baseSymbolFactor * item.AmountBuy) / (symbolFactor * item.AmountSell)
                        : 0)
                    })
                    .OrderBy(item => item.Price)
                    .ToList(),
                Bids = nativeOrderBook.
                    Where(item => !string.Equals(item.TokenBuy, "0x0000000000000000000000000000000000000000", StringComparison.InvariantCultureIgnoreCase))
                    .Select(item => new Order
                    {
                        Quantity = (decimal)(symbolFactor * item.AmountBuy),
                        Price = (decimal)(item.AmountBuy > 0
                        ? (baseSymbolFactor * item.AmountSell) / (symbolFactor * item.AmountBuy)
                        : 0)
                    })
                    .OrderByDescending(item => item.Price)
                    .ToList()
            };

            return orderBook;
        }

        public OrderBook GetOrderBookFromRepo(TradingPair tradingPair, CachePolicy cachePolicy)
        {
            var container = _idexOrderBookRepo.Get(tradingPair.Symbol, tradingPair.BaseSymbol);
            if (container == null) { return null; }

            return container.OrderBook;
        }

        public OrderBook GetOrderBookFromApi(TradingPair tradingPair, CachePolicy cachePolicy)
        {
            var nativeOrderBook = GetNativeOrderBook(tradingPair, cachePolicy);
            if (nativeOrderBook == null) { return null; }

            var currencyDecimals = GetDecimalsForNativeSymbol(tradingPair.Symbol, cachePolicy);
            var baseDecimals = GetDecimalsForNativeSymbol(tradingPair.BaseSymbol, cachePolicy);
            double symbolFactor = 1.0d / Math.Pow(10.0, currencyDecimals);
            double baseSymbolFactor = 1.0d / Math.Pow(10.0, baseDecimals);

            var orderBook = new OrderBook
            {
                Asks = nativeOrderBook.
                    Where(item => !string.Equals(item.TokenSell, "0x0000000000000000000000000000000000000000", StringComparison.InvariantCultureIgnoreCase))
                    .Select(item => new Order
                    {
                        Quantity = (decimal)(symbolFactor * item.AmountSell),
                        Price = (decimal)(item.AmountSell > 0
                        ? (baseSymbolFactor * item.AmountBuy) / (symbolFactor * item.AmountSell)
                        : 0)
                    })
                    .OrderBy(item => item.Price)
                    .ToList(),
                Bids = nativeOrderBook.
                    Where(item => !string.Equals(item.TokenBuy, "0x0000000000000000000000000000000000000000", StringComparison.InvariantCultureIgnoreCase))
                    .Select(item => new Order
                    {
                        Quantity = (decimal)(symbolFactor * item.AmountBuy),
                        Price = (decimal)(item.AmountBuy > 0
                        ? (baseSymbolFactor * item.AmountSell) / (symbolFactor * item.AmountBuy)
                        : 0)
                    })
                    .OrderByDescending(item => item.Price)
                    .ToList()
            };

            return orderBook;
        }

        private List<IdexOrderBookItem> GetNativeOrderBook(TradingPair tradingPair, CachePolicy cachePolicy)
        {
            const string Url = "https://api.idex.market/returnOrderBookForMarket";
            var json = $"{{ \"selectedMarket\": \"{tradingPair.BaseSymbol}\", \"tradeForMarket\": \"{tradingPair.Symbol}\" }}";

            var retriever = new Func<string>(() =>
            {
                var postResult = HttpPost(Url, json);
                return postResult.contents;
            });

            var translator = new Func<string, List<IdexOrderBookItem>>(text =>
            {
                if (string.IsNullOrWhiteSpace(text)) { return null; }
                return JsonConvert.DeserializeObject<List<IdexOrderBookItem>>(text);
            });

            var validator = new Func<string, bool>(text =>
            {
                if (string.IsNullOrWhiteSpace(text)) { return false; }
                translator(text);

                return true;
            });

            var context = new MongoCollectionContext(DbContext, $"idex--order-book-for-market--{tradingPair.Symbol}-{tradingPair.BaseSymbol}");

            var threshold = TimeSpan.FromMinutes(30);

            CacheResult cacheResult;
            try
            {
                cacheResult = _cacheUtil.GetCacheableEx(ThrottleContext, retriever, context, threshold, cachePolicy, validator);
            }
            catch (Exception exception)
            {
                _log.Error(exception);
                if (cachePolicy == CachePolicy.AllowCache)
                {
                    return GetNativeOrderBook(tradingPair, CachePolicy.OnlyUseCache);
                }

                throw;
            }

            return translator(cacheResult?.Contents);
        }

        private (DateTime startTime, string contents, DateTime endTime) HttpPost(string url, string data = null)
        {
            return Throttle(() =>
            {
                var startTime = DateTime.UtcNow;

                var contents = (new Func<string>(() =>
                {
                    var mostRecentBadGateway = _badGatewayCollecetion.AsQueryable()
                        .OrderByDescending(item => item.Id)
                        .FirstOrDefault();

                    if (mostRecentBadGateway != null && (DateTime.UtcNow - mostRecentBadGateway.TimeStampUtc) < BadGatewayThreshold)
                    {
                        throw new ApplicationException("Not enough time has passed since the last bad gateway exception.");
                    }

                    try
                    {
                        return _webUtil.Post(url, data);
                    }
                    catch (Exception exception)
                    {
                        Console.WriteLine($"Idex -- encountered an exception while fetching data.");
                        if (exception.Message.Contains("(502)"))
                        {
                            _badGatewayCollecetion.InsertOne(new BadGatewayEvent { TimeStampUtc = DateTime.UtcNow, ExceptionType = exception.GetType().FullName, Message = exception.Message });
                        }
                        
                        _log.Error(exception);
                    }
                    return null;
                }))();
                
                var endTime = DateTime.UtcNow;

                return (startTime, contents, endTime);
            });
        }

        private string HttpPostWithCache(string url, string data = null)
        {
            return Throttle(() =>
            {
                return _webCache.Post(url, data);
            });
        }

        private static object Locker = new object();
        private static DateTime? LastReadTime;
        // idex's rate limit is 100 per minute (or 600ms)
        // taking it up to 1000ms to be on the safe side.        

        public string Name => "Idex";
        public Guid Id => new Guid("DB43FD38-BFF0-4E14-A231-36CA256ED076");

        private static T Throttle<T>(Func<T> getter)
        {
            lock (Locker)
            {
                if (LastReadTime.HasValue)
                {
                    var remainigTime = ThrottleThreshold - (DateTime.UtcNow - LastReadTime.Value);
                    if (remainigTime > TimeSpan.Zero)
                    {
                        Console.WriteLine($"Idex - Throttling for {remainigTime.TotalSeconds} seconds.");
                        Thread.Sleep(remainigTime);
                    }
                }

                LastReadTime = DateTime.UtcNow;
                return getter();
            }
        }

        private string GetMarketJson(TradingPair tradingPair)
        {
            var marketText = $"{tradingPair.BaseSymbol.ToUpper()}_{tradingPair.Symbol.ToUpper()}";
            var data = new { market = marketText };
            return JsonConvert.SerializeObject(data);
        }

        public List<TradingPair> GetTradingPairs(CachePolicy cachePolicy = CachePolicy.AllowCache)
        {
            var currencies = GetNativeCurrencies(cachePolicy);

            var disabledSymbols = ResUtil.Get("disabled-symbols.txt", typeof(IdexResDummy).Assembly)
                .Replace("\r\n", "\r").Replace("\n", "\r")
                .Split('\r')
                .Select(item => item.Trim())
                .Distinct()
                .ToList();
            
            return currencies
                .Where(item => !disabledSymbols.Any(iggy => string.Equals(item.Symbol, iggy, StringComparison.InvariantCultureIgnoreCase)))
                .Select(item =>
                {
                    var canonSymbol = FromIdexSymbol(item.Symbol);
                    const string CanonBaseSymbol = "ETH";
                    var tradingPair = new TradingPair
                    {
                        Symbol = canonSymbol,
                        NativeCommodityName = item.Name,
                        CommodityName = item.Name,
                        BaseSymbol = CanonBaseSymbol
                    };

                    return tradingPair;
                })
                .OrderBy(item => item.Symbol)
                .ToList();
        }

        private string ToIdexSymbol(string symbol)
        {
            var effectiveSymbol = (symbol ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(effectiveSymbol)) { return symbol; }

            if (_reverseIdexSynonyms.ContainsKey(effectiveSymbol))
            {
                return _reverseIdexSynonyms[effectiveSymbol];
            }

            return symbol;
        }

        private string FromIdexSymbol(string symbol)
        {
            var effectiveSymbol = (symbol ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(effectiveSymbol)) { return symbol; }

            if (_idexSynonyms.ContainsKey(effectiveSymbol))
            {
                return _idexSynonyms[effectiveSymbol];
            }

            return symbol;
        }

        private IdexCurrency GetNativeCurrency(string nativeSymbol, CachePolicy cachePolicy)
        {
            var currencies = GetNativeCurrencies(cachePolicy);
            return currencies != null
                ? currencies.SingleOrDefault(item => string.Equals(nativeSymbol, item.Symbol, StringComparison.InvariantCultureIgnoreCase))
                : null;
        }

        private static DateTime? LastGetNativeCurrenciesFailure = null;
        private static object GetNativeCurrenciesLocker = new object();

        private List<IdexCurrency> GetNativeCurrencies(CachePolicy cachePolicy)
        {
            var retriever = new Func<string>(() =>
            {
                const string Url = "https://api.idex.market/returnCurrencies";
                return _webUtil.Post(Url);
            });

            var translator = new Func<string, List<IdexCurrency>>(text => !string.IsNullOrWhiteSpace(text) ? ParseCurrencies(text) : new List<IdexCurrency>());

            var validator = new Func<string, bool>(text =>
            {
                if (string.IsNullOrWhiteSpace(text)) { return false; }
                var translated = translator(text);
                return translated != null && translated.Any();
            });

            var threshold = TimeSpan.FromHours(4);
            var collectionContext = new MongoCollectionContext(DbContext, "idex--return-currencies");

            var cacheResult = _cacheUtil.GetCacheableEx(ThrottleContext, retriever, collectionContext, threshold, cachePolicy, validator);

            return translator(cacheResult?.Contents);
        }

        private List<IdexCurrency> GetNativeCurrenciesFromResource()
        {
            var currenciesRes = ResUtil.Get("currencies.json", GetType().Assembly);
            return ParseCurrencies(currenciesRes);
        }

        private List<IdexCurrency> ParseCurrencies(string contents)
        {
            var json = (JObject)JsonConvert.DeserializeObject(contents);

            var currencies = new List<IdexCurrency>();
            foreach (JProperty kid in json.Children())
            {
                var name = kid.Name;
                var value = kid.Value;
                var valueJson = JsonConvert.SerializeObject(value);
                var currency = JsonConvert.DeserializeObject<IdexCurrency>(valueJson);
                currency.Symbol = name;

                currencies.Add(currency);
            }

            return currencies;
        }

        public string GetTicker(TradingPair tradingPair)
        {
            var data = GetMarketJson(tradingPair);

            const string MethodName = "returnTicker";
            var uri = $"https://api.idex.market/{MethodName}";

            return _webCache.Post(uri, data);
        }

        private readonly TimeSpan GetTickerThreshold = TimeSpan.FromMinutes(5);
        public List<IdexTickerItem> GetTicker(CachePolicy cachePolicy)
        {
            const string MethodName = "returnTicker";
            var uri = $"https://api.idex.market/{MethodName}";

            var retriever = new Func<string>(() => _webUtil.Post(uri, "{}"));
            var validator = new Func<string, bool>(text => !string.IsNullOrWhiteSpace(text));
            
            var translator = new Func<string, List<IdexTickerItem>>(text =>
            {
                var json = (JObject)JsonConvert.DeserializeObject(text);
                var ticker = new List<IdexTickerItem>();
                foreach (JProperty tradingPairNode in json.Children())
                {
                    var tradingPairName = tradingPairNode.Name;
                    if (string.IsNullOrWhiteSpace(tradingPairName)) { continue; }

                    var pieces = tradingPairName.Split('_').ToList();
                    if (pieces.Count != 2) { continue; }
                    var baseSymbol = pieces[0];
                    var symbol = pieces[1];                    
                    
                    var child = tradingPairNode.Children().FirstOrDefault();
                    if (child == null) { continue; }

                    var getVal = new Func<string, decimal?>(nodeName =>
                        ParseUtil.DecimalTryParse(child[nodeName]?.Value<string>())
                    );

                    var tickerItem = new IdexTickerItem
                    {
                        Symbol = symbol,
                        BaseSymbol = baseSymbol,
                        Last = getVal("last"),
                        High = getVal("high"),
                        Low = getVal("low"),
                        LowestAsk = getVal("lowestAsk"),
                        HighestBid = getVal("highestBid"),
                        PercentChange = getVal("percentChange"),
                        BaseVolume = getVal("baseVolume"),
                        QuoteVolume = getVal("quoteVolume")
                    };
                    ticker.Add(tickerItem);
                }

                return ticker;
            });

            var collectionContext = new MongoCollectionContext(_configClient.GetConnectionString(), DatabaseName, "idex--get-ticker");
            var cacheResult = _cacheUtil.GetCacheableEx(ThrottleContext, retriever, collectionContext, GetTickerThreshold, cachePolicy);

            return cacheResult != null && !string.IsNullOrWhiteSpace(cacheResult.Contents)
                ? translator(cacheResult.Contents)
                : new List<IdexTickerItem>();
        }

        public decimal? GetWithdrawalFee(string symbol, CachePolicy cachePolicy)
        {
            return null;
        }

        public Dictionary<string, decimal> GetWithdrawalFees(CachePolicy cachePolicy)
        {
            return new Dictionary<string, decimal>();
        }

        public List<DepositAddressWithSymbol> GetDepositAddresses(CachePolicy cachePolicy)
        {
            if (cachePolicy == CachePolicy.ForceRefresh)
            {
                GetCommodities(CachePolicy.ForceRefresh);
            }

            return new List<DepositAddressWithSymbol>();
        }

        public DepositAddress GetDepositAddress(string symbol, CachePolicy cachePolicy)
        {
            return null;
        }

        public void SetDepositAddress(DepositAddress depositAddress)
        {
            throw new NotImplementedException();
        }

        public string RequestUserTradeHistoryFromApi()
        {            
            var address = _configClient.GetMewWalletAddress();

            var currentTimeUtc = DateTime.UtcNow;
            // DateTimeUtil.GetUnixTimeStamp();
            var start = DateTimeUtil.GetUnixTimeStamp(currentTimeUtc);
                // 1521044609;
            var end = DateTimeUtil.GetUnixTimeStamp(currentTimeUtc.Subtract(TimeSpan.FromHours(48)));
            // 1525278208;

            var payload = "{\"address\": \"" + address + "\", start: "+ start +", end: " + end + "}";

            const string Url = "https://api.idex.market/returnTradeHistoryMeta";

            var response = _webUtil.Post(Url, payload);

            return response;
        }

        public List<HistoricalTrade> GetUserTradeHistoryFromApi(CachePolicy cachePolicy)
        {
            var ethAddress = _configClient.GetMewWalletAddress();

            var native = GetNativeUserTradeHistory(cachePolicy);
            if (native == null) { return new List<HistoricalTrade>(); }

            var trades = new List<HistoricalTrade>();
            foreach (var item in native.Trades ?? new Dictionary<string, List<IdexTradeHistoryItem>>())
            {
                var idexTradingPair = item.Key;
                string symbol = null;
                string baseSymbol = null;
                var pieces = idexTradingPair.Split('_');
                if (pieces != null && pieces.Count() == 2)
                {
                    symbol = pieces[1];
                    baseSymbol = pieces[0];
                }

                foreach (var tradeItem in item.Value ?? new List<IdexTradeHistoryItem>())
                {
                    if (!string.Equals(tradeItem.Maker, ethAddress, StringComparison.InvariantCultureIgnoreCase)
                        && !string.Equals(tradeItem.Taker, ethAddress, StringComparison.InvariantCultureIgnoreCase))
                    {
                        continue;
                    }

                    var trade = new HistoricalTrade();
                    trade.Symbol = symbol;
                    trade.BaseSymbol = baseSymbol;
                    trade.Quantity = tradeItem.Amount ?? 0;
                    trade.Price = tradeItem.Price ?? 0;

                    trade.TimeStampUtc = tradeItem.Timestamp.HasValue
                        ? (DateTimeUtil.UnixTimeStampToDateTime(tradeItem.Timestamp.Value) ?? default(DateTime))
                        : default(DateTime);

                    trades.Add(trade);
                }
            }

            return trades;
        }

        public IdexTradeHistoryMeta GetNativeUserTradeHistory(CachePolicy cachePolicy)
        {
            var retriever = new Func<string>(() => RequestUserTradeHistoryFromApi());
            var translator = new Func<string, IdexTradeHistoryMeta>(text =>
                !string.IsNullOrWhiteSpace(text)
                ? JsonConvert.DeserializeObject<IdexTradeHistoryMeta>(text)
                : null);

            var validator = new Func<string, bool>(text =>
            {
                if (string.IsNullOrWhiteSpace(text)) { return false; }
                translator(text);
                return true;
            });

            var collectionContext = new MongoCollectionContext(DbContext, "idex--trade-history-meta");
            var threshold = TimeSpan.FromMinutes(30);

            var cacheResult = _cacheUtil.GetCacheableEx(ThrottleContext, retriever, collectionContext, threshold, cachePolicy, validator);

            return translator(cacheResult?.Contents);
        }

        public List<HistoricalTrade> GetUserTradeHistory(CachePolicy cachePolicy)
        {
            return GetUserTradeHistoryFromApi(cachePolicy);
        }

        public List<HistoricalTrade> GetUserTradeHistoryFromRepo(CachePolicy cachePolicy)
        {
            var container = _idexHistoryRepo.Get();
            if (container == null || container.HistoryItems == null) { return new List<HistoricalTrade>(); }

            var trades = new List<HistoricalTrade>();
            foreach (var item in container.HistoryItems)
            {
                var trade = new HistoricalTrade
                {
                    Symbol = item.Symbol,
                    BaseSymbol = item.BaseSymbol,
                    Price = item.Price ?? 0,
                    Quantity = item.Quantity ?? 0,
                    TradeType = item.TradeType,
                    TradingPair = new TradingPair(item.Symbol, item.BaseSymbol),
                    FeeQuantity = item.FeeQuantity ?? 0,
                    FeeCommodity = item.FeeSymbol
                };

                if (item.TimeStampLocal.HasValue)
                {
                    var clientTimeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(container.ClientTimeZone);
                    if (clientTimeZoneInfo != null)
                    {
                        trade.TimeStampUtc = TimeZoneInfo.ConvertTime(item.TimeStampLocal.Value, clientTimeZoneInfo, TimeZoneInfo.Utc);                        
                    }
                }

                trades.Add(trade);
            }

            return trades;
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

        private List<string> GetCoins(CachePolicy cachePolicy = CachePolicy.AllowCache)
        {
            var pairs = GetTradingPairs(cachePolicy);
            var allCoins = new List<string>();
            allCoins.AddRange(pairs.Select(item => item.Symbol));
            allCoins.AddRange(pairs.Select(item => item.BaseSymbol));

            return allCoins.Distinct().OrderBy(item => item).ToList();
        }

        public List<OpenOrderForTradingPair> GetOpenOrders(CachePolicy cachePolicy)
        {            
            var nativeOpenOrders = GetNativeOpenOrders(cachePolicy);
            if (nativeOpenOrders == null || !nativeOpenOrders.Any()) { return new List<OpenOrderForTradingPair>(); }

            var currencies = GetNativeCurrencies(CachePolicy.OnlyUseCacheUnlessEmpty);
            var allContracts = nativeOpenOrders.Select(item => item.TokenBuy)
                .Union(nativeOpenOrders.Select(item => item.TokenSell))
                .Distinct()
                .ToList();

            if (allContracts.Any(contract =>
                 !string.Equals((contract ?? string.Empty).Trim(), EmptyToken, StringComparison.InvariantCultureIgnoreCase)
                 && !currencies.Any(currency =>
                     string.Equals((currency.Address ?? string.Empty).Trim(), (contract ?? string.Empty).Trim(), StringComparison.InvariantCultureIgnoreCase))))
            {
                currencies = GetNativeCurrencies(CachePolicy.ForceRefresh);
            }

            return nativeOpenOrders.Select(item => NativeOpenOrderToOpenOrderForTradingPair(item, currencies))
                .ToList();
        }

        private OpenOrderForTradingPair NativeOpenOrderToOpenOrderForTradingPair(           
            IdexOrderBookItem native,
            List<IdexCurrency> currencies)
        {
            var openOrder = new OpenOrderForTradingPair();
            string contractAddress = null;

            Func<string, bool> isContract = text =>
                !string.IsNullOrWhiteSpace(text)
                && !string.Equals(text.Trim(), EmptyToken, StringComparison.InvariantCultureIgnoreCase);

            if (isContract(native.TokenBuy))
            {
                openOrder.OrderType = OrderType.Bid;
                contractAddress = native.TokenBuy.Trim();
            }
            else if (isContract(native.TokenSell))
            {
                openOrder.OrderType = OrderType.Ask;
                contractAddress = native.TokenSell.Trim();
            }

            var currency = currencies.SingleOrDefault(queryCurrency =>
                string.Equals(queryCurrency.Address, contractAddress, StringComparison.InvariantCultureIgnoreCase));

            var baseCurrency = currencies.SingleOrDefault(queryCurrency =>
                string.Equals(queryCurrency.Symbol, "ETH", StringComparison.InvariantCultureIgnoreCase));

            openOrder.Symbol = !string.IsNullOrWhiteSpace(currency?.Symbol)
                ? currency.Symbol.Trim()
                : contractAddress;

            openOrder.BaseSymbol = !string.IsNullOrWhiteSpace(baseCurrency?.Symbol)
                ? baseCurrency.Symbol.Trim()
                : "ETH";

            if (currency != null && baseCurrency != null)
            {
                var currencyDecimals = currency.Decimals;
                var baseDecimals = baseCurrency.Decimals;
                double symbolFactor = 1.0d / Math.Pow(10.0, currencyDecimals);
                double baseSymbolFactor = 1.0d / Math.Pow(10.0, baseDecimals);

                var a = openOrder.OrderType == OrderType.Ask ? native.AmountBuy : native.AmountSell;
                var b = openOrder.OrderType == OrderType.Ask ? native.AmountSell : native.AmountBuy;

                openOrder.Quantity = (decimal)(symbolFactor * b);
                openOrder.Price = (decimal)(b > 0
                            ? (baseSymbolFactor * a) / (symbolFactor * b) : 0);
            }

            return openOrder;
        }

        public List<IdexOrderBookItem> GetNativeOpenOrders(CachePolicy cachePolicy)
        {
            var ethAddress = _configClient.GetMewWalletAddress();
            var payload = "{\"address\": \"" + ethAddress + "\"}";

            const string MethodName = "returnOrderBookForUser";
            var url = $"https://api.idex.market/{MethodName}";

            var retriever = new Func<string>(() => _webUtil.Post(url, payload));

            var translator = new Func<string, List<IdexOrderBookItem>>(text => 
                !string.IsNullOrWhiteSpace(text)
                ? JsonConvert.DeserializeObject<List<IdexOrderBookItem>>(text):
                new List<IdexOrderBookItem>());

            var validator = new Func<string, bool>(text =>
            {
                if (string.IsNullOrWhiteSpace(text)) { return false; }
                translator(text);

                return true;
            });

            var collectionContext = new MongoCollectionContext(DbContext, "idex--get-native-open-orders");

            var cacheResult = _cacheUtil.GetCacheableEx(ThrottleContext, retriever, collectionContext, OpenOrdersThreshold, cachePolicy, validator);
            return translator(cacheResult.Contents);
        }
    }
}
