using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using BlocktradeExchangeLib;
using BlocktradeExchangeLib.Models;
using cache_lib;
using cache_lib.Models;
using config_client_lib;
using log_lib;
using mongo_lib;
using Newtonsoft.Json;
using trade_constants;
using trade_model;

namespace blocktrade_lib
{
    public class BlockTradeExchange : IBlockTradeExchange
    {
        public Guid Id => new Guid("97D6B19F-12A9-4A25-800F-F4262AB7FB0D");
        public string Name => "Blocktrade";

        private const string DatabaseName = "blocktrade";

        private static ThrottleContext ThrottleContext = new ThrottleContext
        {
            Locker = new object(),
            ThrottleThreshold = TimeSpan.FromSeconds(2.5)
        };

        private readonly IConfigClient _configClient;
        private readonly IBlocktradeClient _blockTradeClient;
        private readonly ICacheUtil _cacheUtil;
        private readonly ILogRepo _log;

        private static TimeSpan AssetsThreshold = TimeSpan.FromMinutes(30);
        private static TimeSpan TradingPairsThreshold = TimeSpan.FromMinutes(30);
        private static TimeSpan OrderBookThreshold = TimeSpan.FromMinutes(10);

        public BlockTradeExchange(
            IConfigClient configClient,
            IBlocktradeClient blockTradeClient,
            ICacheUtil cacheUtil,
            ILogRepo logRepo)
        {
            _configClient = configClient;
            _blockTradeClient = blockTradeClient;
            _cacheUtil = cacheUtil;
            _log = logRepo;
        }

        public List<CommodityForExchange> GetCommodities(CachePolicy cachePolicy)
        {
            var nativeAssetsWithAsOf = GetNativeAssets(cachePolicy);
            return nativeAssetsWithAsOf.Data.Select(item =>
            {
                return new CommodityForExchange
                {
                    Symbol = item.IsoCode,
                    NativeSymbol = item.IsoCode,
                    Name = item.FullName,
                    NativeName = item.FullName
                };
            }).ToList();
        }


        public DepositAddress GetDepositAddress(string symbol, CachePolicy cachePolicy)
        {
            return null;
        }

        public List<DepositAddressWithSymbol> GetDepositAddresses(CachePolicy cachePolicy)
        {
            return new List<DepositAddressWithSymbol>();
        }

        public HoldingInfo GetHoldings(CachePolicy cachePolicy)
        {
            var nativeAssetsWithAsOf = GetNativeAssets(CachePolicy.AllowCache);

            var portfoliosResponse = GetNativePortfolios(cachePolicy);
            var holdingInfo = new HoldingInfo();
            holdingInfo.TimeStampUtc = portfoliosResponse.AsOfUtc;
            holdingInfo.Holdings = new List<Holding>();
            holdingInfo.Holdings = (portfoliosResponse.Data.FirstOrDefault() ?? new BlocktradePortfolio())
                .Assets
                .Select(queryTradingAsset =>
                {
                    var matchingAsset = nativeAssetsWithAsOf.Data.SingleOrDefault(queryItem => queryItem.Id == queryTradingAsset.TradingAssetId);

                    var available = queryTradingAsset.AvailableAmount ?? 0;
                    var inOrders = queryTradingAsset?.ReservedAmount ?? 0;
                    var total = available + inOrders;

                    return new Holding
                    {
                        Symbol = !string.IsNullOrWhiteSpace(matchingAsset?.IsoCode)
                            ? matchingAsset?.IsoCode
                            : queryTradingAsset.TradingAssetId.ToString(),
                        Available = available,
                        InOrders = inOrders,
                        Total = total
                    };
                }).ToList();

            return holdingInfo;
        }

        public OrderBook GetOrderBook(TradingPair tradingPair, CachePolicy cachePolicy)
        {
            var nativeTradingPairId = GetNativeTradingPairId(tradingPair, cachePolicy);
            var nativeOrderBookResponse = GetNativeOrderBook(nativeTradingPairId, cachePolicy);


            return new OrderBook
            {
                AsOf = nativeOrderBookResponse.AsOfUtc,
                Asks = nativeOrderBookResponse.Data.Asks.Select(queryNativeOrder => new Order
                {
                    Price = queryNativeOrder.Price,
                    Quantity = queryNativeOrder.Amount
                }).OrderBy(item => item.Price).ToList(),
                Bids = nativeOrderBookResponse.Data.Bids.Select(queryNativeOrder => new Order
                {
                    Price = queryNativeOrder.Price,
                    Quantity = queryNativeOrder.Amount
                }).OrderBy(item => item.Price).ToList()
            };
        }

        private AsOfWrapper<BlocktradeOrderBook> GetNativeOrderBook(int nativeTradingPairId, CachePolicy cachePolicy)
        {
            var translator = new Func<string, BlocktradeOrderBook>(text => !string.IsNullOrWhiteSpace(text)
                ? JsonConvert.DeserializeObject<BlocktradeOrderBook>(text)
                : new BlocktradeOrderBook());

            var validator = new Func<string, bool>(text =>
            {
                if (string.IsNullOrWhiteSpace(text)) { throw new ApplicationException($"{Name} returned a null or whitespace response when requesting order book for trading pair {nativeTradingPairId}."); }
                translator(text);

                return true;
            });

            var retriever = new Func<string>(() =>
            {
                try
                {
                    var contents = _blockTradeClient.GetOrderBookRaw(nativeTradingPairId);
                    if (!validator(contents))
                    {
                        throw new ApplicationException($"Blocktrade order book for trading pair {nativeTradingPairId} failed validation.");
                    }

                    return contents;
                }
                catch (Exception exception)
                {
                    _log.Error("Failed to retrieve blocktrade order book for trading pair {nativeTradingPairId}.", exception);
                    throw;
                }
            });

            var collectionContext = new MongoCollectionContext(GetDbContext(), "blocktrade-order-book");

            var cacheResult = _cacheUtil.GetCacheableEx(ThrottleContext, retriever, collectionContext, OrderBookThreshold, cachePolicy, validator, null, nativeTradingPairId.ToString());

            return new AsOfWrapper<BlocktradeOrderBook>
            {
                AsOfUtc = cacheResult?.AsOf,
                Data = translator(cacheResult?.Contents)
            };
        }

        private int GetNativeTradingPairId(TradingPair tradingPair, CachePolicy cachePolicy)
        {
            var lowerCachePolicy = cachePolicy == CachePolicy.ForceRefresh ? CachePolicy.AllowCache : cachePolicy;
            var nativeAssets = GetNativeAssets(lowerCachePolicy);            

            var matchingAsset = nativeAssets.Data.SingleOrDefault(queryNativeAsset => string.Equals(queryNativeAsset.IsoCode, tradingPair.Symbol, StringComparison.InvariantCultureIgnoreCase));
            var matchingBaseAsset = nativeAssets.Data.SingleOrDefault(queryNativeAsset => string.Equals(queryNativeAsset.IsoCode, tradingPair.BaseSymbol, StringComparison.InvariantCultureIgnoreCase));

            if ((matchingAsset == null || matchingBaseAsset == null) && lowerCachePolicy != cachePolicy)
            {
                nativeAssets = GetNativeAssets(cachePolicy);
                matchingAsset = nativeAssets.Data.SingleOrDefault(queryNativeAsset => string.Equals(queryNativeAsset.IsoCode, tradingPair.Symbol, StringComparison.InvariantCultureIgnoreCase));
                matchingBaseAsset = nativeAssets.Data.SingleOrDefault(queryNativeAsset => string.Equals(queryNativeAsset.IsoCode, tradingPair.BaseSymbol, StringComparison.InvariantCultureIgnoreCase));
            }

            if (matchingAsset == null) { throw new ApplicationException($"Failed to find a matching blocktrade asset for {tradingPair.Symbol}."); }
            if (matchingBaseAsset == null) { throw new ApplicationException($"Failed to find a matching blocktrade asset for {tradingPair.BaseSymbol}."); }

            var nativeTradingPairs = GetNativeTradingPairs(lowerCachePolicy);
            var matchingTradingPair = nativeTradingPairs.Data.SingleOrDefault(queryNativeTradingPair =>
            {
                return queryNativeTradingPair.BaseAssetId == matchingAsset.Id
                    && queryNativeTradingPair.QuoteAssetId == matchingBaseAsset.Id;
            });

            if (matchingTradingPair == null && lowerCachePolicy != cachePolicy)
            {
                nativeTradingPairs = GetNativeTradingPairs(cachePolicy);
                matchingTradingPair = nativeTradingPairs.Data.SingleOrDefault(queryNativeTradingPair =>
                {
                    return queryNativeTradingPair.BaseAssetId == matchingAsset.Id
                        && queryNativeTradingPair.QuoteAssetId == matchingBaseAsset.Id;
                });
            }

            if (matchingTradingPair == null)
            {
                throw new ApplicationException($"Failed to find a matching blocktrade trading pair for {tradingPair.Symbol}-{tradingPair.BaseSymbol}.");
            }

            return matchingTradingPair.Id;
        }

        public List<TradingPair> GetTradingPairs(CachePolicy cachePolicy)
        {
            var nativeAssets = GetNativeAssets(cachePolicy);
            var nativePairs = GetNativeTradingPairs(cachePolicy);

            return nativePairs.Data.Select(queryNativePair =>
            {                
                var nativeAsset = nativeAssets.Data.SingleOrDefault(queryNativeAsset => queryNativeAsset.Id == queryNativePair.BaseAssetId);
                var nativeBaseAsset = nativeAssets.Data.SingleOrDefault(queryNativeAsset => queryNativeAsset.Id == queryNativePair.QuoteAssetId);

                if (nativeAsset == null || nativeBaseAsset == null) { return null; }

                var nativeSymbol = nativeAsset.IsoCode;
                var nativeCommodityName = nativeAsset.FullName;

                var nativeBaseSymbol = nativeBaseAsset.IsoCode;
                var nativeBaseCommodityName = nativeBaseAsset.FullName;

                return new TradingPair
                {
                    Symbol = nativeSymbol,
                    NativeSymbol = nativeSymbol,
                    NativeCommodityName = nativeCommodityName,
                    
                    BaseSymbol = nativeBaseSymbol,
                    NativeBaseSymbol = nativeBaseSymbol,
                    NativeBaseCommodityName = nativeBaseCommodityName,

                    LotSize = queryNativePair.LotSize,
                    PriceTick = queryNativePair.TickSize
                };
            })
            .Where(item => item != null)
            .ToList();
        }

        public decimal? GetWithdrawalFee(string symbol, CachePolicy cachePolicy)
        {
            return null;
        }

        public Dictionary<string, decimal> GetWithdrawalFees(CachePolicy cachePolicy)
        {
            return new Dictionary<string, decimal>();
        }

        public LimitOrderResult BuyLimit(string symbol, string baseSymbol, QuantityAndPrice quantityAndPrice)
        {
            var apiKey = GetApiKey();
            var tradingPairId = GetNativeTradingPairId(new TradingPair(symbol, baseSymbol), CachePolicy.AllowCache);

            const int PortfolioId = 1;

            var response = _blockTradeClient.PlaceOrder(apiKey, PortfolioId, BlocktradeDirection.Buy, BlocktradeOrderType.Limit, tradingPairId, quantityAndPrice.Quantity, quantityAndPrice.Price, BlocktradeTimeInForce.GoodUntilCancelled, null);
            return new LimitOrderResult
            {
                WasSuccessful = response.Id > 0,
                OrderId = response.Id.ToString()
            };
        }

        public LimitOrderResult SellLimit(string symbol, string baseSymbol, QuantityAndPrice quantityAndPrice)
        {
            var apiKey = GetApiKey();

            var tradingPairId = GetNativeTradingPairId(new TradingPair(symbol, baseSymbol), CachePolicy.AllowCache);

            Thread.Sleep(TimeSpan.FromSeconds(1.5d));
            const int PortfolioId = 1;
            var response = _blockTradeClient.PlaceOrder(apiKey, PortfolioId, BlocktradeDirection.Sell, BlocktradeOrderType.Limit, tradingPairId, quantityAndPrice.Quantity, quantityAndPrice.Price, BlocktradeTimeInForce.GoodUntilCancelled, null);

            return new LimitOrderResult
            {
                WasSuccessful = response.Id > 0,
                OrderId = response.Id.ToString()
            };
        }

        private AsOfWrapper<List<BlocktradeAsset>> GetNativeAssets(CachePolicy cachePolicy)
        {
            var translator = new Func<string, List<BlocktradeAsset>>(text => !string.IsNullOrWhiteSpace(text)
                ? JsonConvert.DeserializeObject<List<BlocktradeAsset>>(text)
                : new List<BlocktradeAsset>());

            var validator = new Func<string, bool>(text =>
            {
                if (string.IsNullOrWhiteSpace(text)) { throw new ApplicationException($"{Name} returned a null or whitespace response when requesting assets."); }
                translator(text);

                return true;
            });

            var retriever = new Func<string>(() =>
            {
                try
                {
                    var contents = _blockTradeClient.GetTradingAssetsRaw();
                    if (!validator(contents))
                    {
                        throw new ApplicationException("Blocktrade assets failed validation.");
                    }

                    return contents;
                }
                catch (Exception exception)
                {
                    _log.Error("Failed to retrieve blocktrade assets.", exception);
                    throw;
                }
            });

            var collectionContext = new MongoCollectionContext(GetDbContext(), "blocktrade-assets");

            var cacheResult = _cacheUtil.GetCacheableEx(ThrottleContext, retriever, collectionContext, AssetsThreshold, cachePolicy, validator);

            return new AsOfWrapper<List<BlocktradeAsset>>
            {
                AsOfUtc = cacheResult?.AsOf,
                Data = translator(cacheResult?.Contents)
            };
        }

        private AsOfWrapper<List<BlocktradeTradingPair>> GetNativeTradingPairs(CachePolicy cachePolicy)
        {
            var translator = new Func<string, List<BlocktradeTradingPair>>(text => !string.IsNullOrWhiteSpace(text)
                ? JsonConvert.DeserializeObject<List<BlocktradeTradingPair>>(text)
                : new List<BlocktradeTradingPair>());

            var validator = new Func<string, bool>(text =>
            {
                if (string.IsNullOrWhiteSpace(text)) { throw new ApplicationException($"{Name} returned a null or whitespace response when requesting assets."); }
                translator(text);

                return true;
            });

            var retriever = new Func<string>(() =>
            {
                try
                {
                    var contents = _blockTradeClient.GetTradingPairsRaw();
                    if (!validator(contents))
                    {
                        throw new ApplicationException("Blocktrade trading pairs failed validation.");
                    }

                    return contents;
                }
                catch (Exception exception)
                {
                    _log.Error("Failed to retrieve blocktrade trading pairs.", exception);
                    throw;
                }
            });

            var collectionContext = new MongoCollectionContext(GetDbContext(), "blocktrade-trading-pairs");

            var cacheResult = _cacheUtil.GetCacheableEx(ThrottleContext, retriever, collectionContext, TradingPairsThreshold, cachePolicy, validator);

            return new AsOfWrapper<List<BlocktradeTradingPair>>
            {
                AsOfUtc = cacheResult?.AsOf,
                Data = translator(cacheResult?.Contents)
            };
        }

        private AsOfWrapper<List<BlocktradePortfolio>> GetNativePortfolios(CachePolicy cachePolicy)
        {
            var translator = new Func<string, List<BlocktradePortfolio>>(text => !string.IsNullOrWhiteSpace(text)
                ? JsonConvert.DeserializeObject<List<BlocktradePortfolio>>(text)
                : new List<BlocktradePortfolio>());

            var validator = new Func<string, bool>(text =>
            {
                if (string.IsNullOrWhiteSpace(text)) { throw new ApplicationException($"{Name} returned a null or whitespace response when requesting assets."); }
                translator(text);

                return true;
            });

            var retriever = new Func<string>(() =>
            {
                try
                {
                    var apiKey = GetApiKey();
                    var contents = _blockTradeClient.GetUserPortfoliosRaw(apiKey);
                    if (!validator(contents))
                    {
                        throw new ApplicationException("Blocktrade trading pairs failed validation.");
                    }

                    return contents;
                }
                catch (Exception exception)
                {
                    _log.Error("Failed to retrieve blocktrade trading pairs.", exception);
                    throw;
                }
            });

            var collectionContext = new MongoCollectionContext(GetDbContext(), "blocktrade-portfolios");

            var cacheResult = _cacheUtil.GetCacheableEx(ThrottleContext, retriever, collectionContext, TradingPairsThreshold, cachePolicy, validator);

            return new AsOfWrapper<List<BlocktradePortfolio>>
            {
                AsOfUtc = cacheResult?.AsOf,
                Data = translator(cacheResult?.Contents)
            };
        }

        private AsOfWrapper<BlocktradeGetUserOrdersResponse> GetNativeUserOrders(CachePolicy cachePolicy, List<string> desiredStatuses = null)
        {
            var translator = new Func<string, BlocktradeGetUserOrdersResponse>(text => !string.IsNullOrWhiteSpace(text)
                ? JsonConvert.DeserializeObject<BlocktradeGetUserOrdersResponse>(text)
                : new BlocktradeGetUserOrdersResponse());

            var validator = new Func<string, bool>(text =>
            {
                if (string.IsNullOrWhiteSpace(text)) { throw new ApplicationException($"{Name} returned a null or whitespace response when requesting user orders."); }
                translator(text);

                return true;
            });

            var retriever = new Func<string>(() =>
            {
                try
                {
                    var apiKey = GetApiKey();
                    var contents = _blockTradeClient.GetUserOrdersRaw(apiKey, desiredStatuses);
                    if (!validator(contents))
                    {
                        throw new ApplicationException("Blocktrade user orders failed validation.");
                    }

                    return contents;
                }
                catch (Exception exception)
                {
                    _log.Error("Failed to retrieve blocktrade user orders.", exception);
                    throw;
                }
            });

            var key = desiredStatuses == null || !desiredStatuses.Any()
                ? "ALL"
                : string.Join(",", desiredStatuses.Select(item => item.Trim().ToUpper()));

            var collectionContext = new MongoCollectionContext(GetDbContext(), "blocktrade-user-orders");

            var cacheResult = _cacheUtil.GetCacheableEx(ThrottleContext, retriever, collectionContext, TradingPairsThreshold, cachePolicy, validator, null, key);

            return new AsOfWrapper<BlocktradeGetUserOrdersResponse>
            {
                AsOfUtc = cacheResult?.AsOf,
                Data = translator(cacheResult?.Contents)
            };
        }

        private IMongoDatabaseContext GetDbContext()
        {
            var connectionString = _configClient.GetConnectionString();
            return new MongoDatabaseContext(connectionString, DatabaseName);
        }
        
        private BlocktradeApiKey GetApiKey()
        {
            var apiKey = _configClient.GetApiKey(IntegrationNameRes.Blocktrade);
            return new BlocktradeApiKey
            {
                PublicKey = apiKey.Key,
                PrivateKey = apiKey.Secret
            };
        }

        private static Dictionary<string, OrderType> BlocktradeOrderTypeDictionary = new Dictionary<string, OrderType>
        {
            { "BUY", OrderType.Bid },
            { "SELL", OrderType.Ask }
        };

        public List<OpenOrdersForTradingPair> GetOpenOrdersV2()
        {
            var nativeAssets = GetNativeAssets(CachePolicy.AllowCache);
            var nativeTradingPairs = GetNativeTradingPairs(CachePolicy.AllowCache);

            var desiredStatuses = new List<string> { "NEW", "PARTIALLY_FILLED" };
            var response = GetNativeUserOrders(CachePolicy.AllowCache, desiredStatuses);

            var tradingPairIds = response.Data.Data.Select(item => item.TradingPairId).Distinct();

            return tradingPairIds.Select(tradingPairId =>
            {
                var matchingTradingPair = nativeTradingPairs.Data.SingleOrDefault(queryTradingPair =>
                    queryTradingPair.Id == tradingPairId);

                var matchingAsset = matchingTradingPair != null
                    ? nativeAssets.Data.SingleOrDefault(queryNativeAsset => queryNativeAsset.Id == matchingTradingPair.BaseAssetId)
                    : null;

                var matchingQuoteAsset = matchingTradingPair != null
                    ? nativeAssets.Data.SingleOrDefault(queryNativeAsset => queryNativeAsset.Id == matchingTradingPair.QuoteAssetId)
                    : null;

                var notOpenStatuses = new List<string> { "CANCELLED", "FILLED" };

                var openOrders = response.Data.Data
                .Where(queryItem => 
                    queryItem.TradingPairId == tradingPairId
                    && !notOpenStatuses.Any(notOpenStatus => string.Equals(queryItem.Status, notOpenStatus, StringComparison.InvariantCultureIgnoreCase)))
                .Select(queryOrder => new OpenOrder
                {
                    OrderId = queryOrder.Id.ToString(),
                    Quantity = queryOrder.RemainingAmount,
                    Price = queryOrder.Price,
                    OrderType = BlocktradeOrderTypeDictionary.ContainsKey(queryOrder.Direction)
                        ? BlocktradeOrderTypeDictionary[queryOrder.Direction]
                        : OrderType.Unknown
                }).ToList();

                return new OpenOrdersForTradingPair
                {
                    AsOfUtc = response.AsOfUtc,
                    Symbol = matchingAsset.IsoCode,
                    BaseSymbol = matchingQuoteAsset.IsoCode,
                    OpenOrders = openOrders
                };
            }).ToList();
        }

        public OpenOrdersWithAsOf GetOpenOrdersForTradingPairV2(string symbol, string baseSymbol, CachePolicy cachePolicy)
        {
            var nativeAssetsResponse = GetNativeAssets(CachePolicy.AllowCache);
            var matchingAsset = nativeAssetsResponse.Data.Single(queryAsset => string.Equals(queryAsset.IsoCode, symbol, StringComparison.InvariantCultureIgnoreCase));
            var matchingQuoteAsset = nativeAssetsResponse.Data.Single(queryAsset => string.Equals(queryAsset.IsoCode, baseSymbol, StringComparison.InvariantCultureIgnoreCase));

            var nativeTradingPairs = GetNativeTradingPairs(CachePolicy.AllowCache);
            var matchingTradingPair = nativeTradingPairs.Data.SingleOrDefault(queryTradingPair => queryTradingPair.BaseAssetId == matchingAsset?.Id && queryTradingPair.QuoteAssetId == matchingQuoteAsset?.Id);

            var desiredStatuses = new List<string> { "NEW", "PARTIALLY_FILLED" };
            var response = GetNativeUserOrders(cachePolicy, desiredStatuses);
            var allStatuses = response.Data.Data.Select(item => item.Status).Distinct().ToList();

            if (matchingTradingPair == null)
            {
                return new OpenOrdersWithAsOf
                {
                    AsOfUtc = response.AsOfUtc,
                    OpenOrders = new List<OpenOrder>()
                };
            }

            var notOpenStatuses = new List<string> { "CANCELLED", "FILLED" };

            var openOrders = response.Data.Data
                .Where(queryNativeOrder =>
                    queryNativeOrder.TradingPairId == matchingTradingPair.Id
                    && !notOpenStatuses.Any(notOpenStatus => string.Equals(queryNativeOrder.Status, notOpenStatus, StringComparison.InvariantCultureIgnoreCase)))
                .Select(queryNativeOrder =>
                {
                    return new OpenOrder
                    {
                        OrderId = queryNativeOrder.Id.ToString(),
                        Quantity = queryNativeOrder.RemainingAmount,
                        Price = queryNativeOrder.Price,
                        OrderType = BlocktradeOrderTypeDictionary.ContainsKey(queryNativeOrder.Direction)
                            ? BlocktradeOrderTypeDictionary[queryNativeOrder.Direction]
                            : OrderType.Unknown
                    };
                })
                .ToList();


            return new OpenOrdersWithAsOf
            {
                AsOfUtc = response.AsOfUtc,
                OpenOrders = openOrders
            };
        }

        public void CancelOrder(string orderId)
        {
            var apiKey = GetApiKey();
            var parsedOrderId = long.Parse(orderId);
            var response = _blockTradeClient.CancelOrder(apiKey, parsedOrderId);
            _log.Info($"Response from cancelling blocktrade order {parsedOrderId}.");
        }
    }
}
