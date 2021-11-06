using log_lib;
using mongo_lib;
using Newtonsoft.Json;
using qryptos_lib.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using cache_lib.Models;
using trade_model;
using trade_res;
using web_util;
using cache_lib;
using qryptos_lib.Res;
using cache_lib.Models.Snapshots;
using MongoDB.Bson;
using MongoDB.Driver;
using config_client_lib;
using trade_lib.Repo;
using cache_model.Snapshots;
using qryptos_lib.Client;
using qryptos_lib.Models.Snapshot;
using res_util_lib;

namespace qryptos_lib
{
    // https://developers.quoine.com/
    public class QryptosIntegration : IQryptosIntegration
    {
        public Guid Id => new Guid("E64F17F5-10F5-4016-98BC-4ED0F0CFB2EF");
        public string Name => "Qryptos";
        private const string DatabaseName = "qryptos";

        private static ThrottleContext ThrottleContext = new ThrottleContext
        {
            Locker = new object(),
            ThrottleThreshold = TimeSpan.FromSeconds(5)
        };

        private static TimeSpan GetOpenOrdersForTradingPairThreshold = TimeSpan.FromMinutes(20);
        private static TimeSpan BalancesThreshold = TimeSpan.FromMinutes(10);

        private readonly IConfigClient _configClient;
        private readonly IQryptosClient _qryptosClient;
        private readonly IWebUtil _webUtil;
        private readonly ILogRepo _log;
        private readonly IOpenOrdersSnapshotRepo _openOrdersSnapshotRepo;

        private readonly CacheUtil _cacheUtil = new CacheUtil();

        private readonly QryptosMap _qryptosMap = new QryptosMap();

        public QryptosIntegration(
            IConfigClient configClient,
            IQryptosClient qryptosClient,
            IWebUtil webUtil,
            IOpenOrdersSnapshotRepo openOrdersSnapshotRepo,
            ILogRepo log)
        {
            _configClient = configClient;
            _qryptosClient = qryptosClient;
            _webUtil = webUtil;
            _openOrdersSnapshotRepo = openOrdersSnapshotRepo;
            _log = log;
        }

        private IMongoCollectionContext CommoditySnapshotCollection => new MongoCollectionContext(DbContext, "qryptos--commodity-snapshot");

        public List<CommodityForExchange> GetCommodities(CachePolicy cachePolicy)
        {
            if (cachePolicy == CachePolicy.OnlyUseCacheUnlessEmpty || cachePolicy == CachePolicy.OnlyUseCacheUnlessEmpty)
            {
                var snapshotCacheResult = CommoditySnapshotCollection.GetLast<QryptosCommoditiesSnapshot>();
                if (snapshotCacheResult?.ExchangeCommodities != null) { return snapshotCacheResult.ExchangeCommodities; }
            }

            var mapTimeStampNullable = _qryptosMap.TimeStampUtc;
            if (!mapTimeStampNullable.HasValue)
            {
                throw new ApplicationException($"The {Name} commodity map is missing a time stamp.");
            }
            var mapTimeStamp = mapTimeStampNullable.Value;

            var productsWithAsOf = GetNativeProducts(cachePolicy);
            var products = productsWithAsOf?.Data;

            var commodities = new List<CommodityForExchange>();
            foreach (var product in products)
            {
                var nativeSymbol = product.base_currency;
                var canon = _qryptosMap.GetCanon(nativeSymbol);
                
                if (commodities.Any(existingCommodity => string.Equals(existingCommodity.NativeSymbol, nativeSymbol, StringComparison.InvariantCultureIgnoreCase)))
                {
                    continue;
                }

                var commodity = new CommodityForExchange
                {
                    CanonicalId = canon?.Id,
                    Symbol = !string.IsNullOrWhiteSpace(canon?.Symbol) ? canon.Symbol : product.base_currency,
                    NativeSymbol = nativeSymbol,
                    Name = !string.IsNullOrWhiteSpace(canon?.Name) ? canon.Name : product.base_currency,
                    NativeName = product.base_currency
                };

                if (canon != null && canon.Id == CommodityRes.Augur.Id)
                {
                    commodity.CanDeposit = false;
                }

                commodities.Add(commodity);
            }

            var snapshot = new QryptosCommoditiesSnapshot
            {
                TimeStampUtc = DateTime.UtcNow,
                MapTimeStampUtc = mapTimeStamp,

                ExchangeCommodities = commodities
            };            
            
            CommoditySnapshotCollection.Insert(snapshot);

            var collection = CommoditySnapshotCollection.GetCollection<BsonDocument>();
            var filter = Builders<BsonDocument>.Filter.Lt("_id", snapshot.Id);

            collection.DeleteMany(filter);

            return commodities;
        }

        public DepositAddress GetDepositAddress(string symbol, CachePolicy cachePolicy)
        {
            var accounts = GetNativeCryptoAccounts(cachePolicy);
            if(accounts?.Data == null) { return null; }

            var match = accounts.Data.SingleOrDefault(item => string.Equals(item.CurrencySymbol, symbol, StringComparison.InvariantCultureIgnoreCase));
            if (match == null) { return null; }

            return new DepositAddress
            {
                Address = match.Address,
                Memo = null
            };
        }

        public List<DepositAddressWithSymbol> GetDepositAddresses(CachePolicy cachePolicy)
        {
            var accounts = GetNativeCryptoAccounts(cachePolicy);
            if(accounts?.Data == null) { return null; }
            return (accounts.Data ?? new List<QryptosCryptoAccount>())
                .Select(item => new DepositAddressWithSymbol
                {
                    Symbol = item.CurrencySymbol,
                    Address = item.Address,
                    Memo = null
                }).ToList();
        }

        private static Dictionary<string, string> HoldingSymbolDictionary = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase)
        {
            { "Ξ", "ETH" },
            { "฿", "BTC" },
            { "Ꝗ", "QASH" },
        };

        private static string TranslateHoldingSymbol(string nativeHoldingSymbol)
        {
            if (string.IsNullOrWhiteSpace(nativeHoldingSymbol)) { return nativeHoldingSymbol; }
            var effectiveNativeHoldingSymbol = nativeHoldingSymbol.Trim();

            return HoldingSymbolDictionary.ContainsKey(effectiveNativeHoldingSymbol)
                ? HoldingSymbolDictionary[effectiveNativeHoldingSymbol]
                : nativeHoldingSymbol;
        }

        public BalanceWithAsOf GetBalanceForSymbol(string symbol, CachePolicy cachePolicy)
        {
            const string NO_ACCOUNT = "NO_ACCOUNT";

            var translator = new Func<string, QryptosIndividualAccount>(text =>
                !string.IsNullOrWhiteSpace(text) && !string.Equals(text, NO_ACCOUNT, StringComparison.InvariantCultureIgnoreCase)
                ? JsonConvert.DeserializeObject<QryptosIndividualAccount>(text)
                : null);

            var validator = new Func<string, bool>(text =>
            {
                if (string.Equals(text, NO_ACCOUNT, StringComparison.InvariantCultureIgnoreCase)) { return true; }
                if (string.IsNullOrWhiteSpace(text)) { throw new ApplicationException(@"Received an empty response when requesting balance for {symbol} from {Name}"); }
                return true;
            });

            var nativeSymbol = _qryptosMap.ToNativeSymbol(symbol);

            var retriever = new Func<string>(() =>
            {
                try
                {
                    var apiKey = _configClient.GetQryptosApiKey();
                    var contents = _qryptosClient.GetAccount(apiKey, nativeSymbol);
                    if (!validator(contents)) { throw new ApplicationException($"Validation failed when trying to retrieve {Name} account {nativeSymbol}."); }

                    return contents;
                }
                catch (Exception exception)
                {
                    _log.Error(exception);
                    throw;
                }
            });

            var key = $"{symbol.ToUpper()}";
            var collectionContext = new MongoCollectionContext(DbContext, "qryptos--native-individual-account");

            var cacheResult = _cacheUtil.GetCacheableEx(ThrottleContext, retriever, collectionContext, BalancesThreshold, cachePolicy, validator, null, key);
            var native = translator(cacheResult?.Contents);

            return new BalanceWithAsOf
            {
                Symbol = symbol.ToUpper(),
                Available = native?.FreeBalance ?? 0,
                InOrders = native?.OrdersMargin ?? 0,
                Total = native?.Balance ?? 0,
                AsOfUtc = cacheResult?.AsOf
            };
        }

        public BalanceWithAsOf GetBalanceForSymbolOld(string symbol, CachePolicy cachePolicy)
        {
            const string NO_ACCOUNT = "NO_ACCOUNT";

            var translator = new Func<string, QryptosIndividualAccount>(text => 
                !string.IsNullOrWhiteSpace(text) && !string.Equals(text, NO_ACCOUNT, StringComparison.InvariantCultureIgnoreCase)
                ? JsonConvert.DeserializeObject<QryptosIndividualAccount>(text)
                : null);

            var validator = new Func<string, bool>(text =>
            {
                if (string.Equals(text, NO_ACCOUNT, StringComparison.InvariantCultureIgnoreCase)) { return true; }
                if (string.IsNullOrWhiteSpace(text)) { throw new ApplicationException(@"Received an empty response when requesting balance for {symbol} from {Name}"); }
                return true;
            });

            var retriever = new Func<string>(() =>
            {
                try
                {
                    var nativeSymbol = _qryptosMap.ToNativeSymbol(symbol);
                    var nativeAccounts = GetNativeCryptoAccounts(CachePolicy.OnlyUseCacheUnlessEmpty);

                    var matchingAccount = nativeAccounts.Data.FirstOrDefault(queryAccount =>
                        string.Equals(queryAccount.Currency, nativeSymbol, StringComparison.InvariantCultureIgnoreCase));

                    if (matchingAccount == null && (cachePolicy == CachePolicy.AllowCache || cachePolicy == CachePolicy.ForceRefresh))
                    {
                        nativeAccounts = GetNativeCryptoAccounts(cachePolicy);
                        matchingAccount = nativeAccounts.Data.FirstOrDefault(queryAccount =>
                        string.Equals(queryAccount.CurrencySymbol, nativeSymbol, StringComparison.InvariantCultureIgnoreCase));

                        if (matchingAccount == null)
                        {
                            return NO_ACCOUNT;
                            // throw new ApplicationException($"{Name} - Failed to determine the account id for {symbol}.");
                        }
                    }

                    var apiKey = _configClient.GetQryptosApiKey();
                    var text = _qryptosClient.GetCryptoAccount(apiKey, matchingAccount.Id);

                    if (!validator(text)) { throw new ApplicationException("Validation failed when attempting to retreive balance for {symbol} from {Name}.{Environment.NewLine}{exception.message}"); }
                    return text;
                }
                catch (Exception exception)
                {
                    _log.Error($"Failed to retreive balance for {symbol} from {Name}.{Environment.NewLine}{exception.Message}");
                    throw;
                }
            });

            var key = $"{symbol.ToUpper()}";
            var collectionContext = new MongoCollectionContext(DbContext, "qryptos--native-individual-account");

            var cacheResult = _cacheUtil.GetCacheableEx(ThrottleContext, retriever, collectionContext, BalancesThreshold, cachePolicy, validator, null, key);
            var native = translator(cacheResult?.Contents);

            return new BalanceWithAsOf
            {
                Symbol = symbol.ToUpper(),
                Available = native?.FreeBalance ?? 0,
                InOrders = native?.OrdersMargin ?? 0,
                Total = native?.Balance ?? 0,
                AsOfUtc = cacheResult?.AsOf
            };
        }

        public HoldingInfo GetHoldings(CachePolicy cachePolicy)
        {
            var native = GetNativeCryptoAccounts(cachePolicy);

            return new HoldingInfo
            {
                TimeStampUtc = native.AsOfUtc,
                Holdings = native.Data
                .Where(item => item.Balance.HasValue && item.Balance.Value > 0).Select(item =>
                {
                    var symbol = _qryptosMap.ToCanonicalSymbol(item.Currency);

                    return new Holding
                    {
                        Symbol = symbol,
                        Total = item.Balance ?? 0,
                        Available = item.Balance ?? 0,
                        InOrders = 0
                    };
                }).ToList()
            };
        }

        public OrderBook GetOrderBook(TradingPair tradingPair, CachePolicy cachePolicy)
        {
            var nativeSymbol = _qryptosMap.ToNativeSymbol(tradingPair.Symbol);
            var nativeBaseSymbol = _qryptosMap.ToNativeSymbol(tradingPair.BaseSymbol);

            var native = GetNativeOrderBook(nativeSymbol, nativeBaseSymbol, cachePolicy);
            return new OrderBook
            {
                Asks = native.sell_price_levels.Select(item => new Order { Price = item[0], Quantity = item[1] }).ToList(),
                Bids = native.buy_price_levels.Select(item => new Order { Price = item[0], Quantity = item[1] }).ToList(),
                AsOf = native.AsOf
            };
        }

        private static TimeSpan OrderBookThreshold = TimeSpan.FromMinutes(10);
        private QryptosOrderBook GetNativeOrderBook(string nativeSymbol, string nativeBaseSymbol, CachePolicy cachePolicy)
        {
            if (string.IsNullOrWhiteSpace(nativeSymbol)) { throw new ArgumentNullException(nameof(nativeSymbol)); }
            if (string.IsNullOrWhiteSpace(nativeBaseSymbol)) { throw new ArgumentNullException(nameof(nativeBaseSymbol)); }

            var productCachePolicy = cachePolicy == CachePolicy.ForceRefresh
                ? CachePolicy.AllowCache
                : cachePolicy;
            
            AsOfWrapper<List<QryptosProduct>> productsWithAsOf = null;
            List<QryptosProduct> products = null;
            try
            {
                productsWithAsOf = GetNativeProducts(productCachePolicy);
                products = productsWithAsOf?.Data;
            }
            catch (Exception exception)
            {
                _log.Error(exception);

                if (productCachePolicy == CachePolicy.AllowCache)
                {
                    productsWithAsOf = GetNativeProducts(CachePolicy.OnlyUseCache);
                    products = productsWithAsOf?.Data;
                    if (products == null || !products.Any()) { throw; }
                }
            }

            var matchingProduct = products.SingleOrDefault(queryProduct =>
                string.Equals(queryProduct.product_type, "CurrencyPair", StringComparison.InvariantCultureIgnoreCase)
                && string.Equals(queryProduct.base_currency, nativeSymbol, StringComparison.InvariantCultureIgnoreCase)
                && string.Equals(queryProduct.currency, nativeBaseSymbol, StringComparison.InvariantCultureIgnoreCase));

            if (matchingProduct == null)
            {
                throw new ApplicationException($"Failed to find a matching Qryptos product for {nativeSymbol}-{nativeBaseSymbol}");
            }

            // GET /products/:id/price_levels
            var url = $"https://api.liquid.com/products/{matchingProduct.Id}/price_levels";

            var translator = new Func<string, QryptosOrderBook>(text =>
            {
                if (string.IsNullOrWhiteSpace(text)) { return null; }
                return JsonConvert.DeserializeObject<QryptosOrderBook>(text);
            });

            var validator = new Func<string, bool>(text =>
            {
                if (string.IsNullOrWhiteSpace(text)) { return false; }
                var translated = translator(text);
                if (translated == null) { return false; }

                return true;
            });

            var retriever = new Func<string>(() =>
            {
                try
                {
                    var responseText = _webUtil.Get(url);                    
                    return responseText;
                }
                catch (Exception exception)
                {
                    _log.Error($"Web request failed to {url} when attempting to get {Name} book for {nativeSymbol}-{nativeBaseSymbol}.");
                    _log.Error(exception);
                    throw;
                }
            });

            var key = $"{nativeSymbol}-{nativeBaseSymbol}";
            var context = GetOrderBookContext();
            var cacheResult = _cacheUtil.GetCacheableEx(ThrottleContext, retriever, context, OrderBookThreshold, cachePolicy, validator, AfterInsertOrderBook, key);

            var result = translator(cacheResult?.Contents);
            result.AsOf = cacheResult?.AsOf;

            return result;
        }

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

        public Dictionary<string, decimal> GetLotSizes()
        {
            return ResUtil.Get<Dictionary<string, decimal>>("qryptos-lot-size.json", typeof(QryptosResDummy).Assembly);
        }

        private static List<TradingPair> _inMemoryTradingPairs = null;

        public List<TradingPair> GetTradingPairs(CachePolicy cachePolicy = CachePolicy.AllowCache)
        {
            if ((cachePolicy == CachePolicy.OnlyUseCache || cachePolicy == CachePolicy.OnlyUseCacheUnlessEmpty)
                && _inMemoryTradingPairs != null)
            {
                return _inMemoryTradingPairs;
            }

            var productsWithAsOf = GetNativeProducts(cachePolicy);
            var products = productsWithAsOf?.Data;

            var lotSizeDictionary = GetLotSizes();

            return _inMemoryTradingPairs =  products
                .Where(item => !item.disabled)
                .Select(item =>
                {
                    var nativeSymbol = item.base_currency;
                    var nativeBaseSymbol = item.currency;

                    var canonCommodity = _qryptosMap.GetCanon(nativeSymbol);
                    var canonBaseSymbol = _qryptosMap.GetCanon(nativeBaseSymbol);

                    var lotSize = lotSizeDictionary.ContainsKey(nativeSymbol)
                        ? lotSizeDictionary[nativeSymbol]
                        : (decimal?)null;

                    return new TradingPair
                    {
                        CanonicalCommodityId = canonCommodity?.Id,
                        CommodityName = canonCommodity?.Name ?? item.base_currency,
                        NativeCommodityName = item.base_currency,
                        Symbol = canonCommodity?.Symbol ?? item.base_currency,
                        NativeSymbol = item.base_currency,

                        CanonicalBaseCommodityId = canonBaseSymbol?.Id,
                        BaseCommodityName = canonBaseSymbol?.Name ?? item.currency,
                        NativeBaseCommodityName = item.currency,
                        BaseSymbol = item.currency,
                        NativeBaseSymbol = item.currency,

                        LotSize = lotSize
                    };
                })
                .ToList();
        }

        public decimal? GetWithdrawalFee(string symbol, CachePolicy cachePolicy)
        {
            // currently, qryptos does not charge fees to withdraw.
            return 0;
        }

        public Dictionary<string, decimal> GetWithdrawalFees(CachePolicy cachePolicy)
        {
            return new Dictionary<string, decimal>();
        }

        private static TimeSpan ProductsThreshold = TimeSpan.FromMinutes(30);
        public AsOfWrapper<List<QryptosProduct>> GetNativeProducts(CachePolicy cachePolicy)
        {
            // X-Quoine-API-Version	2
            const string Url = "https://api.liquid.com/products";
            var retriever = new Func<string>(() => _webUtil.Get(Url));
            var translator = new Func<string, List<QryptosProduct>>(text => JsonConvert.DeserializeObject<List<QryptosProduct>>(text));
            var validator = new Func<string, bool>(text =>
            {
                if (string.IsNullOrWhiteSpace(text)) { return false; }
                var translated = translator(text);
                if (translated == null || !translated.Any()) { return false; }
                return true;
            });

            var context = new MongoCollectionContext(_configClient.GetConnectionString(), DatabaseName, "qryptos--get-products");

            var cacheResult = _cacheUtil.GetCacheableEx(ThrottleContext, retriever, context, ProductsThreshold, cachePolicy, validator);

            return new AsOfWrapper<List<QryptosProduct>>
            {
                AsOfUtc = cacheResult?.AsOf,
                Data = translator(cacheResult?.Contents)
            };
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

        private OrderBook ToOrderBook(string text, DateTime? asOf)
        {
            var native = JsonConvert.DeserializeObject<QryptosOrderBook>(text);

            return new OrderBook
            {
                Asks = native.sell_price_levels.Select(item => new Order { Price = item[0], Quantity = item[1] }).ToList(),
                Bids = native.buy_price_levels.Select(item => new Order { Price = item[0], Quantity = item[1] }).ToList(),
                AsOf = asOf
            };
        }
        
        private MongoCollectionContext GetAllOrderBooksCollectionContext()
        {
            return new MongoCollectionContext(DbContext, "qryptos--all-order-books");
        }

        private IMongoDatabaseContext DbContext
        {
            get { return new MongoDatabaseContext(_configClient.GetConnectionString(), DatabaseName); }
        }

        private IMongoCollectionContext GetOrderBookContext()
        {
            return new MongoCollectionContext(DbContext, $"qryptos--order-book");
        }

        public List<OpenOrderForTradingPair> GetOpenOrders(string symbol, string baseSymbol, CachePolicy cachePolicy)
        {
            var openOrdersWithAsOf = GetOpenOrdersForTradingPairV2(symbol, baseSymbol, cachePolicy);

            return openOrdersWithAsOf?.OpenOrders != null
                ? openOrdersWithAsOf.OpenOrders.Select(item => new OpenOrderForTradingPair
                {
                    Symbol = symbol,
                    BaseSymbol = baseSymbol,
                    OrderId = item.OrderId,
                    OrderType = item.OrderType,
                    Price = item.Price,
                    Quantity = item.Quantity,
                }).ToList()
                : new List<OpenOrderForTradingPair>();
        }

        public void CancelOrder(string orderId)
        {
            _log.Info($"About to cancel order {orderId} on {Name}");

            var apiKey = _configClient.GetQryptosApiKey();
            var contents = _qryptosClient.CancelOrder(apiKey, orderId);
            // var contents = _tradeNodeUtil.CancelOrder(Name, orderId);

            _log.Info($"{Name} responded with the text below when attempting to cancel order {orderId}.{Environment.NewLine}{contents}");

            var response = JsonConvert.DeserializeObject<QryptosCancelOrderResponse>(contents);

            const string ExpectedStatus = "cancelled";
            if (!string.Equals(response?.Status, ExpectedStatus, StringComparison.InvariantCultureIgnoreCase))
            {
                throw new ApplicationException($"Attempted to cancel order {orderId} on {Name}. Expected {Name} to return its status as {ExpectedStatus}. However, its actual status was {response?.Status ?? "(null)"}.");
            }
        }

        public bool BuyLimit(TradingPair tradingPair, decimal quantity, decimal price)
        {
            return PlaceLimitOrder(tradingPair, quantity, price, true);
        }

        public bool SellLimit(TradingPair tradingPair, decimal quantity, decimal price)
        {           
            return PlaceLimitOrder(tradingPair, quantity, price, false);
        }

        private bool PlaceLimitOrder(TradingPair tradingPair, decimal quantity, decimal price, bool isBid)
        {
            var apiKey = _configClient.GetQryptosApiKey();
            var nativeSymbol = _qryptosMap.ToNativeSymbol(tradingPair.Symbol);
            var nativeBaseSymbol = _qryptosMap.ToNativeSymbol(tradingPair.BaseSymbol);

            var productId = GetProductId(nativeSymbol, nativeBaseSymbol, CachePolicy.ForceRefresh);
            if (!productId.HasValue || productId.Value == default(long))
            {
                throw new ApplicationException($"Failed to retrieve a {Name} product id for {nativeSymbol}-{nativeBaseSymbol}.");
            }

            var responseText = isBid
                ? _qryptosClient.BuyLimit(apiKey, productId.Value, price, quantity)
                : _qryptosClient.SellLimit(apiKey, productId.Value, price, quantity);

            var bidOrAskTextWithArticle = isBid ? "a bid" : "an ask";
            if (string.IsNullOrWhiteSpace(responseText)) { throw new ApplicationException($"{Name} returned a null or whitespace response when attempting place {bidOrAskTextWithArticle} for {quantity} {nativeSymbol} at {price} {nativeBaseSymbol}."); }

            var response = JsonConvert.DeserializeObject<QryptosPlaceOrderResponse>(responseText);

            _log.Info($"Placed a {bidOrAskTextWithArticle} on {Name} for {quantity} {nativeSymbol} at {price} {nativeBaseSymbol}.{Environment.NewLine}{responseText}");

            return true;
        }

        public List<OpenOrdersForTradingPair> GetOpenOrdersV2()
        {
            var snapshotContext = GetOpenOrdersSnapshotContext();
            var snapshot = snapshotContext.GetLast<OpenOrdersSnapshot>();

            var items = new List<OpenOrdersForTradingPair>();
            foreach (var key in snapshot?.SnapshotItems?.Keys?.ToList() ?? new List<string>())
            {
                var snapshotItem = snapshot.SnapshotItems[key];

                var nativeOpenOrdersForKey = !string.IsNullOrWhiteSpace(snapshotItem?.Raw)
                    ? JsonConvert.DeserializeObject<QryptosGetOpenOrdersResponse>(snapshotItem.Raw)
                    : new QryptosGetOpenOrdersResponse();

                //var keyPieces = key.Split('-').ToList();
                //if (keyPieces.Count != 2)
                //{
                //    _log.Warn($"{Name} - Failed to parse open orders snapshot key \"{key}\".");
                //    continue;
                //}

                //var nativeSymbol = keyPieces[0].Trim().ToUpper();
                //var nativeBaseSymbol = keyPieces[1].Trim().ToUpper();

                //var canonicalSymbol = _qryptosMap.ToCanonicalSymbol(nativeSymbol);
                //var canonicalBaseSymbol = _qryptosMap.ToCanonicalSymbol(nativeBaseSymbol);

                var openOrdersForTradingPair = new OpenOrdersForTradingPair();
                openOrdersForTradingPair.Symbol = snapshotItem.Symbol;
                openOrdersForTradingPair.BaseSymbol = snapshotItem.BaseSymbol;
                openOrdersForTradingPair.AsOfUtc = snapshotItem.AsOfUtc;
                openOrdersForTradingPair.OpenOrders = new List<OpenOrder>();
                foreach (var nativeOrder in nativeOpenOrdersForKey.Models)
                {
                    var orderType = OrderTypeDictionary.ContainsKey(nativeOrder.Side)
                        ? OrderTypeDictionary[nativeOrder.Side]
                        : OrderType.Unknown;

                    var openOrder = new OpenOrder
                    {
                        Price = nativeOrder.Price ?? 0,
                        Quantity = nativeOrder.Quantity ?? 0,
                        OrderId = nativeOrder.Id,
                        OrderType = orderType
                    };

                    openOrdersForTradingPair.OpenOrders.Add(openOrder);
                }

                items.Add(openOrdersForTradingPair);
            }

            return items;
        }

        private static Dictionary<string, OrderType> OrderTypeDictionary = new Dictionary<string, OrderType>(StringComparer.InvariantCultureIgnoreCase)
        {
            { "buy", OrderType.Bid },
            { "sell", OrderType.Ask }
        };

        public OpenOrdersWithAsOf GetOpenOrdersForTradingPairV2(string symbol, string baseSymbol, CachePolicy cachePolicy)
        {
            var nativeSymbol = _qryptosMap.ToNativeSymbol(symbol);
            var nativeBaseSymbol = _qryptosMap.ToNativeSymbol(baseSymbol);

            var native = GetNativeOpenOrders(nativeSymbol, nativeBaseSymbol, cachePolicy);


            var openOrders = native?.Data?.Models != null
            ? native.Data.Models.Select(nativeItem =>
            {
                var orderType = OrderTypeDictionary.ContainsKey(nativeItem.Side)
                    ? OrderTypeDictionary[nativeItem.Side]
                    : OrderType.Unknown;

                return new OpenOrder
                {
                    OrderId = nativeItem.Id,
                    Price = nativeItem.Price ?? 0,
                    Quantity = nativeItem.Quantity ?? 0,
                    OrderType = orderType
                };
            }).ToList()
            : new List<OpenOrder>();

            var openOrdersWithAsOf = new OpenOrdersWithAsOf
            {
                AsOfUtc = native?.AsOfUtc,
                OpenOrders = openOrders
            };

            return openOrdersWithAsOf;
        }

        public AsOfWrapper<QryptosGetOpenOrdersResponse> GetNativeOpenOrders(string nativeSymbol, string nativeBaseSymbol, CachePolicy cachePolicy)
        {
            var translator = new Func<string, QryptosGetOpenOrdersResponse>(text =>
                !string.IsNullOrWhiteSpace(text)
                ? JsonConvert.DeserializeObject<QryptosGetOpenOrdersResponse>(text)
                : null);

            var validator = new Func<string, bool>(text =>
            {
                if (string.IsNullOrWhiteSpace(text)) { throw new ApplicationException($"Received a null or whitespace response when requesting {Name} open orders for {nativeSymbol}-{nativeBaseSymbol}."); }
                translator(text);

                return true;
            });

            var retriever = new Func<string>(() =>
            {
                try
                {
                    var nativeProductsWithAsOf = GetNativeProducts(CachePolicy.OnlyUseCacheUnlessEmpty);
                    var nativeProducts = nativeProductsWithAsOf?.Data;

                    var matchingNativeProduct = nativeProducts.Where(item =>
                        string.Equals(item.base_currency, nativeSymbol, StringComparison.InvariantCultureIgnoreCase)
                        && string.Equals(item.quoted_currency, nativeBaseSymbol, StringComparison.InvariantCultureIgnoreCase))
                        .SingleOrDefault();

                    if (matchingNativeProduct == null && (cachePolicy == CachePolicy.ForceRefresh || cachePolicy == CachePolicy.AllowCache))
                    {
                        // If we couldn't find the product with the cached native products,
                        // and the data is old, then refresh the native product cache and try again.
                        var updatedNativeProducts = GetNativeProducts(cachePolicy);
                        matchingNativeProduct = nativeProducts.Where(item =>
                            string.Equals(item.base_currency, nativeSymbol, StringComparison.InvariantCultureIgnoreCase)
                            && string.Equals(item.quoted_currency, nativeBaseSymbol, StringComparison.InvariantCultureIgnoreCase))
                            .SingleOrDefault();

                        // If we still can't find it, then give up.
                        if (matchingNativeProduct == null) { throw new ApplicationException($"Failed to find a matching {Name} product id for {nativeSymbol}-{nativeBaseSymbol}."); }
                    }

                    var nativeProductId = matchingNativeProduct.Id;

                    var apiKey = _configClient.GetQryptosApiKey();

                    var text = _qryptosClient.GetOpenOrders(apiKey, nativeProductId);
                    if (!validator(text)) { throw new ApplicationException($"Response failed validation when requesting {Name} open orders for {nativeSymbol}-{nativeBaseSymbol}."); }
                    return text;
                }
                catch (Exception exception)
                {
                    _log.Error($"Failed to retrieve {Name} open orders for {nativeSymbol}-{nativeBaseSymbol}.");
                    _log.Error(exception);
                    throw;
                }
            });

            var collectionContext = GetOpenOrdersContext();
            var key = $"{nativeSymbol.ToUpper()}-{nativeBaseSymbol.ToUpper()}";

            var afterInsert = new Action<CacheEventContainer>(cec => _openOrdersSnapshotRepo.AfterInsertOpenOrders(GetOpenOrdersContext(), GetOpenOrdersSnapshotContext(), cec));
            var cacheResult = _cacheUtil.GetCacheableEx(ThrottleContext, retriever, collectionContext, GetOpenOrdersForTradingPairThreshold, cachePolicy, validator, afterInsert, key);

            return new AsOfWrapper<QryptosGetOpenOrdersResponse>
            {
                AsOfUtc = cacheResult?.AsOf,
                Data = translator(cacheResult?.Contents)
            };
        }

        public AsOfWrapper<List<QryptosCryptoAccount>> GetNativeCryptoAccounts(CachePolicy cachePolicy)
        {
            var translator = new Func<string, List<QryptosCryptoAccount>>(text =>
            {
                return !string.IsNullOrWhiteSpace(text)
                ? JsonConvert.DeserializeObject<List<QryptosCryptoAccount>>(text)
                : null;
            });

            var validator = new Func<string, bool>(text =>
            {
                if (string.IsNullOrWhiteSpace(text)) { throw new ApplicationException($"Received a null or whitespace response from {Name} when requesting crypto accounts."); }
                translator(text);

                return true;
            });

            var retriever = new Func<string>(() =>
            {
                try
                {
                    var apiKey = _configClient.GetQryptosApiKey();
                    var responseText = _qryptosClient.GetCryptoAccounts(apiKey);

                    if (!validator(responseText))
                    {
                        throw new ApplicationException($"Response from {Name} when requesting crypto accounts failed validation.");
                    }

                    return responseText;
                }
                catch (Exception exception)
                {
                    _log.Error($"Failed to retrieve crypto accounts from {Name}.{Environment.NewLine}{exception.Message}");
                    _log.Error(exception);
                    throw;
                }
            });

            var collectionContext = new MongoCollectionContext(DbContext, "qryptos--get-crypto-accounts");

            var cacheResult = _cacheUtil.GetCacheableEx(ThrottleContext, retriever, collectionContext, BalancesThreshold, cachePolicy, validator);

            return new AsOfWrapper<List<QryptosCryptoAccount>>
            {
                AsOfUtc = cacheResult?.AsOf,
                Data = translator(cacheResult.Contents)
            };
        }

        public AsOfWrapper<List<QryptosTradingAccount>> GetNativeTradingAccounts(CachePolicy cachePolicy)
        {
            var translator = new Func<string, List<QryptosTradingAccount>>(text =>
            {
                return !string.IsNullOrWhiteSpace(text)
                ? JsonConvert.DeserializeObject<List<QryptosTradingAccount>>(text)
                : null;
            });

            var validator = new Func<string, bool>(text =>
            {
                if (string.IsNullOrWhiteSpace(text)) { throw new ApplicationException($"Received a null or whitespace response from {Name} when requesting crypto accounts."); }
                translator(text);

                return true;
            });

            var retriever = new Func<string>(() =>
            {
                try
                {
                    var apiKey = _configClient.GetQryptosApiKey();
                    var responseText = _qryptosClient.GetTradingAccounts(apiKey);
                    if (!validator(responseText))
                    {
                        throw new ApplicationException($"Response from {Name} when requesting crypto accounts failed validation.");
                    }

                    return responseText;
                }
                catch (Exception exception)
                {
                    _log.Error($"Failed to retrieve crypto accounts from {Name}.{Environment.NewLine}{exception.Message}");
                    _log.Error(exception);
                    throw;
                }
            });

            var collectionContext = new MongoCollectionContext(DbContext, "qryptos--get-trading-accounts");

            var cacheResult = _cacheUtil.GetCacheableEx(ThrottleContext, retriever, collectionContext, BalancesThreshold, cachePolicy, validator);

            return new AsOfWrapper<List<QryptosTradingAccount>>
            {
                AsOfUtc = cacheResult?.AsOf,
                Data = translator(cacheResult.Contents)
            };
        }

        private long? GetProductId(string nativeSymbol, string nativeBaseSymbol, CachePolicy cachePolicy)
        {
            var lowCachLevel = cachePolicy == CachePolicy.OnlyUseCache
                ? CachePolicy.OnlyUseCache
                : CachePolicy.OnlyUseCacheUnlessEmpty;

            var nativeProductsWithAsOf = GetNativeProducts(lowCachLevel);
            var nativeProducts = nativeProductsWithAsOf?.Data;

            var matchingNativeProduct = nativeProducts.Where(item =>
                string.Equals(item.base_currency, nativeSymbol, StringComparison.InvariantCultureIgnoreCase)
                && string.Equals(item.quoted_currency, nativeBaseSymbol, StringComparison.InvariantCultureIgnoreCase))
                .SingleOrDefault();

            if (matchingNativeProduct == null && (cachePolicy == CachePolicy.ForceRefresh || cachePolicy == CachePolicy.AllowCache))
            {
                // If we couldn't find the product with the cached native products,
                // and the data is old, then refresh the native product cache and try again.
                var updatedNativeProducts = GetNativeProducts(cachePolicy);
                matchingNativeProduct = nativeProducts.Where(item =>
                    string.Equals(item.base_currency, nativeSymbol, StringComparison.InvariantCultureIgnoreCase)
                    && string.Equals(item.quoted_currency, nativeBaseSymbol, StringComparison.InvariantCultureIgnoreCase))
                    .SingleOrDefault();

                // If we still can't find it, then give up.
                if (matchingNativeProduct == null) { throw new ApplicationException($"Failed to find a matching {Name} product id for {nativeSymbol}-{nativeBaseSymbol}."); }
            }

            return matchingNativeProduct.Id;
        }

        private OpenOrder NativeOpenOrderToOpenOrder(QryptosOrderResponse queryNativeItem)
        {
            var orderTypeDictionary = new Dictionary<string, OrderType>(StringComparer.InvariantCultureIgnoreCase)
            {
                { "buy", OrderType.Bid },
                { "sell", OrderType.Ask },
            };

            var symbolCombo = queryNativeItem.Symbol;
            var pieces = symbolCombo.Split('/');
            var resultSymbol = pieces.Length >= 1 ? pieces[0] : null;
            var resultBaseSymbol = pieces.Length >= 2 ? pieces[1] : null;

            var orderType = orderTypeDictionary.ContainsKey(queryNativeItem.Side)
                ? orderTypeDictionary[queryNativeItem.Side]
                : OrderType.Unknown;

            return new OpenOrder
            {
                Price = queryNativeItem.Price ?? 0,
                Quantity = queryNativeItem.Amount ?? 0,
                OrderId = queryNativeItem.Id.ToString(),
                OrderType = orderType
            };
        }

        private MongoCollectionContext GetOpenOrdersContext() => new MongoCollectionContext(DbContext, "qryptos--open-orders-v2");
        private MongoCollectionContext GetOpenOrdersSnapshotContext() => new MongoCollectionContext(DbContext, "qryptos--open-orders-snapshot-v3");
    }
}