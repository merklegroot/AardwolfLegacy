using coss_model;
using coss_lib.Models;
using mongo_lib;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using cache_lib.Models;
using trade_model;
using web_util;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Globalization;
using log_lib;
using config_connection_string_lib;
using cache_lib;
using coss_lib.Res;
using cache_lib.Models.Snapshots;
using coss_cookie_lib;
using System.Net;
using System.IO;
using coss_data_model;
using System.Threading;
using trade_res;
using tfa_lib;
using res_util_lib;
using cache_model.Snapshots;
using trade_lib.Repo;
using coss_api_client_lib;
using config_client_lib;
using trade_constants;
using coss_api_client_lib.Models;
using task_lib;
using System.Diagnostics;
using date_time_lib;
using math_lib;

namespace coss_lib
{
    public class CossIntegration : ICossIntegration
    {
        private static bool UseApiForOpenOrders = true;

        public string Name => "Coss";
        public Guid Id => new Guid("52EEF7F6-031A-4E6E-A4AE-E0495B8AEF05");
        private const string DatabaseName = "coss";

        private static Random _random = new Random();
        private static TimeSpan MarketThreshold = TimeSpan.FromMinutes(15);
        private static TimeSpan MarketCacheThreshold = TimeSpan.FromMinutes(12.5);
        private static TimeSpan WalletThreshold = TimeSpan.FromMinutes(15);
        private static TimeSpan OpenOrdersThreshold = TimeSpan.FromMinutes(30);
        private static TimeSpan ExchangeHistoryThreshold = TimeSpan.FromMinutes(15);
        private static TimeSpan UserDepositAndWithdrawalHistoryThreshold = TimeSpan.FromMinutes(15);
        private static TimeSpan ExchangeInfoThreshold = TimeSpan.FromMinutes(20);
        private static TimeSpan BalanceThreshold = TimeSpan.FromMinutes(20);
        private static TimeSpan NativeTradingPairCompletedOrdersThreshold = TimeSpan.FromMinutes(30);

        private static object ThrottleLocker = new object();
        private static ThrottleContext ThrottleContext = new ThrottleContext
        {
            Locker = new object(),
            ThrottleThreshold = TimeSpan.FromSeconds(2.5)
        };

        private IMongoCollectionContext _holdingContext => new MongoCollectionContext(GetDbContext(), "coss-holding");
        private IMongoCollectionContext _exchangeHistoryContext => new MongoCollectionContext(GetDbContext(), "coss-exchange-history");
        private IMongoCollectionContext _exchangeDepositAndWithdrawalContext => new MongoCollectionContext(GetDbContext(), "coss--get-deposits-and-withdrawals");
        private IMongoCollectionContext _getTradingPairsContext => new MongoCollectionContext(GetDbContext(), "coss-ec-get-trading-pairs");
        private IMongoCollection<TradingPairsProjection> _tradingPairsProjection => new MongoCollectionContext(GetDbContext(), "coss-trading-pairs-projection").GetCollection<TradingPairsProjection>();
        private IMongoCollectionContext _orderHistoryContext => new MongoCollectionContext(GetDbContext(), "coss-ec-get-order-history");

        private MongoCollectionContext _allOrderBooksCollectionContext =>new MongoCollectionContext(GetDbContext(), "coss--all-engine-order-books");

        private MongoCollectionContext _webOpenOrdersContext => new MongoCollectionContext(GetDbContext(), "coss--web-open-orders");
        private MongoCollectionContext _webOpenOrdersSnapshotContext => new MongoCollectionContext(GetDbContext(), "coss--web-open-orders-snapshot-v3");

        private MongoCollectionContext _engineOpenOrdersContext => new MongoCollectionContext(GetDbContext(), "coss--engine-open-orders");
        private MongoCollectionContext _engineOpenOrdersSnapshotContext => new MongoCollectionContext(GetDbContext(), "coss--engine-open-orders-snapshot-v3");


        private readonly IGetConnectionString _getConnectionString;
        private readonly IWebUtil _webUtil;
        private readonly IConfigClient _configClient;
        private readonly ILogRepo _log;
        private readonly ICossCookieUtil _cossCookieUtil;
        private readonly ICacheUtil _cacheUtil;
        private readonly ICossApiClient _cossApiClient;
        private readonly ITfaUtil _tfaUtil;
        private readonly IOpenOrdersSnapshotRepo _openOrdersSnapshotRepo;

        public class TradingPairsProjection
        {
            public ObjectId Id { get; set; }
            public ObjectId EventId { get; set; }
            public List<TradingPair> TradingPairs { get; set; }
        }

        private CossMap _cossMap = new CossMap();

        public CossIntegration(
            IWebUtil webUtil,
            ICossApiClient cossApiClient,
            IConfigClient configClient,
            ICacheUtil cacheUtil,
            ICossCookieUtil cossCookieUtil,
            IGetConnectionString getConnectionString,
            ITfaUtil tfaUtil,
            IOpenOrdersSnapshotRepo openOrdesSnapshotRepo,
            ILogRepo logRepo)
        {
            _webUtil = webUtil;
            _cossApiClient = cossApiClient;
            _configClient = configClient;
            _cossCookieUtil = cossCookieUtil;
            _getConnectionString = getConnectionString;
            _tfaUtil = tfaUtil;
            _openOrdersSnapshotRepo = openOrdesSnapshotRepo;
            _log = logRepo;

            _cacheUtil = cacheUtil;
        }

        private Dictionary<string, decimal> GetLotSizes()
        {
            return ResUtil.Get<Dictionary<string, decimal>>("coss-lot-size.json", typeof(CossRes).Assembly);
        }

        private Dictionary<string, decimal> GetMinumumTradesBaseSymbolValues()
        {
            return ResUtil.Get<Dictionary<string, decimal>>("coss-minimum-trade-base-symbol-value.json", typeof(CossRes).Assembly);
        }

        private Dictionary<string, decimal> GetPriceTicks()
        {
            return ResUtil.Get<Dictionary<string, decimal>>("coss-price-tick.json", typeof(CossRes).Assembly);
        }

        private Dictionary<string, decimal> GetMinimumTradeQuantities()
        {
            return ResUtil.Get<Dictionary<string, decimal>>("coss-minimum-trade-quantity.json", typeof(CossRes).Assembly);
        }

        private AsOfWrapper<CossApiExchangeInfo> GetExchangeInfo(CachePolicy cachePolicy)
        {
            var translator = new Func<string, CossApiExchangeInfo>(text =>
                !string.IsNullOrWhiteSpace(text)
                ? JsonConvert.DeserializeObject<CossApiExchangeInfo>(text)
                : null);

            var validator = new Func<string, bool>(text =>
            {
                if (string.IsNullOrWhiteSpace(text)) { throw new ApplicationException("Received null or whitespace text when requesting coss exchange info."); }
                translator(text);
                return true;
            });

            var retriever = new Func<string>(() =>
            {
                try
                {
                    var text = _cossApiClient.GetExchangeInfoRaw();
                    if (!validator(text)) { throw new ApplicationException("Coss exchange info failed validation."); }

                    return text;
                }
                catch (Exception exception)
                {
                    _log.Error(exception);
                    throw;
                }
            });

            var collectionContext = new MongoCollectionContext(GetDbContext(), "coss--api-exchange-info");

            var cacheResult = _cacheUtil.GetCacheableEx(ThrottleContext, retriever, collectionContext, ExchangeInfoThreshold, cachePolicy, validator);

            return new AsOfWrapper<CossApiExchangeInfo>
            {
                AsOfUtc = cacheResult?.AsOf,
                Data = translator(cacheResult?.Contents)
            };
        }

        private AsOfWrapper<List<CossWebCoin>> GetCoins(CachePolicy cachePolicy)
        {
            var translator = new Func<string, List<CossWebCoin>>(text =>
                !string.IsNullOrWhiteSpace(text)
                ? JsonConvert.DeserializeObject<List<CossWebCoin>>(text)
                : null);

            var validator = new Func<string, bool>(text =>
            {
                if (string.IsNullOrWhiteSpace(text)) { throw new ApplicationException("Received null or whitespace text when requesting coss exchange info."); }
                translator(text);
                return true;
            });

            var retriever = new Func<string>(() =>
            {
                try
                {
                    var text = _cossApiClient.GetWebCoinsRaw();
                    if (!validator(text)) { throw new ApplicationException("Coss exchange info failed validation."); }

                    return text;
                }
                catch (Exception exception)
                {
                    _log.Error(exception);
                    throw;
                }
            });

            var collectionContext = new MongoCollectionContext(GetDbContext(), "coss--get-web-coins");

            var cacheResult = _cacheUtil.GetCacheableEx(ThrottleContext, retriever, collectionContext, ExchangeInfoThreshold, cachePolicy, validator);

            return new AsOfWrapper<List<CossWebCoin>>
            {
                AsOfUtc = cacheResult?.AsOf,
                Data = translator(cacheResult?.Contents)
            };
        }

        public List<CommodityForExchange> GetCommodities(CachePolicy cachePolicy)
        {
            var webCoinsWithAsOf = GetCoins(cachePolicy);
            if (webCoinsWithAsOf?.Data == null || !webCoinsWithAsOf.Data.Any())
            {
                throw new ApplicationException("Coss did not return any coins.");
            }

            return webCoinsWithAsOf.Data.Select(item =>
            {
                var nativeSymbol = item.CurrencyCode;
                var canon = _cossMap.GetCanon(nativeSymbol);
                var symbol = !string.IsNullOrWhiteSpace(canon?.Symbol) ? canon.Symbol : nativeSymbol;
                var nativeName = item.Name;
                var name = !string.IsNullOrWhiteSpace(canon?.Name)
                    ? canon.Name
                    : !string.IsNullOrWhiteSpace(nativeName)
                    ? nativeName
                    : nativeSymbol;

                return new CommodityForExchange
                {
                    CanonicalId = canon?.Id,
                    Symbol = symbol,
                    NativeSymbol = nativeSymbol,
                    Name = name,
                    NativeName = nativeName,
                    CanDeposit = item.AllowDeposit,
                    CanWithdraw = item.AllowWithdrawn,
                    ContractAddress = !string.IsNullOrWhiteSpace(canon?.ContractId) ? canon?.ContractId : null,
                    WithdrawalFee = item.WithdrawnFee,
                    MinimumTradeQuantity = item.MinimumOrderAmount
                };
            }).ToList();
        }

        public List<CommodityForExchange> GetCommoditiesOld(CachePolicy cachePolicy)
        {
            var exchangeInfoWithAsOf = GetExchangeInfo(cachePolicy);

            return (exchangeInfoWithAsOf?.Data?.Coins ?? new List<CossApiExchangeInfo.CossApiCoin>())
                .Select(item =>
                {
                    var nativeSymbol = item.CurrencyCode;
                    var nativeName = item.Name;
                    var canon = _cossMap.GetCanon(nativeSymbol);

                    var commodity = new CommodityForExchange
                    {
                        CanonicalId = canon?.Id,
                        Symbol = !string.IsNullOrWhiteSpace(canon?.Symbol) ? canon.Symbol : nativeSymbol,
                        NativeSymbol = nativeSymbol,
                        Name = !string.IsNullOrWhiteSpace(canon?.Name) ? canon.Name : nativeName,
                        NativeName = nativeName,
                        //CanDeposit = !item.CurrencyIsDepositLocked,
                        //CanWithdraw = !item.CurrencyIsWithdrawalLocked,
                        //WithdrawalFee = item.CurrencyWithdrawalFee,
                    };

                    return commodity;
                }).ToList();
        }

        public Dictionary<string, decimal> GetWithdrawalFees(CachePolicy cachePolicy)
        {
            return CossRes.WithdrawalFees;
        }

        public OrderBook GetOrderBook(TradingPair tradingPair, CachePolicy cachePolicy)
        {
            var shouldFilter = new Func<CossEngineOrderBook.CossEngineOrder, bool>(queryOrder =>
                false
            );

            var marketInfo = GetMarketInfo(tradingPair, cachePolicy);

            var asks = (marketInfo?.Data?.Asks ?? new List<CossEngineOrderBook.CossEngineOrder>())
                .Where(queryOrder => !shouldFilter(queryOrder))
                .Select(item => new Order { Price = item.Price, Quantity = item.Quantity })
                .OrderBy(item => item.Price)
                .ToList();

            var bids = (marketInfo?.Data?.Bids ?? new List<CossEngineOrderBook.CossEngineOrder>())
                .Where(queryOrder => !shouldFilter(queryOrder))
                .Select(item => new Order { Price = item.Price, Quantity = item.Quantity })
                .OrderByDescending(item => item.Price)
                .ToList();

            var bestAsk = asks.FirstOrDefault();
            var bestBid = bids.FirstOrDefault();

            if (bestAsk != null && bestBid != null && bestBid.Price > bestAsk.Price)
            {
                if (bestAsk.Quantity > bestBid.Quantity) { bids.Remove(bestBid); }
                else { asks.Remove(bestAsk); }
            }

            return new OrderBook
            {
                Asks = asks,
                Bids = bids,
                AsOf = marketInfo?.AsOf
            };
        }

        private CacheResult<CossEngineOrderBook> GetMarketInfo(TradingPair tradingPair, CachePolicy cachePolicy)
        {
            var nativeSymbol = _cossMap.ToNativeSymbol(tradingPair.Symbol);
            var nativeBaseSymbol = _cossMap.ToNativeSymbol(tradingPair.BaseSymbol);

            var result = GetNativeMarketInfo(nativeSymbol, nativeBaseSymbol, cachePolicy);

            return result;
        }

        private CacheResult<CossEngineOrderBook> GetNativeMarketInfo(string nativeSymbol, string nativeBaseSymbol, CachePolicy cachePolicy, TimeSpan? cacheThreshold = null)
        {
            var key = $"{nativeSymbol.ToLower()}-{nativeBaseSymbol.ToLower()}";
            var translator = new Func<string, CossEngineOrderBook>(text =>
                !string.IsNullOrWhiteSpace(text)
                    ? JsonConvert.DeserializeObject<CossEngineOrderBook>(text)
                    : null
            );

            var validator = new Func<string, bool>(contents =>
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(contents)) { return false; }
                    return translator(contents) != null;
                }
                catch
                {
                    return false;
                }
            });

            var retriever = new Func<string>(() =>
            {
                try
                {
                    var text = _cossApiClient.GetOrderBookRaw(nativeSymbol, nativeBaseSymbol);
                    if (!validator(text))
                    {
                        throw new ApplicationException($"Coss order book for {nativeSymbol}-{nativeBaseSymbol} failed validation.");
                    }

                    return text;
                }
                catch (Exception exception)
                {
                    _log.Error(exception);
                    throw;
                }
            });

            var threshold = cacheThreshold ?? MarketThreshold;

            var collection = GetOrderBookContext();
            var cacheResult = _cacheUtil.GetCacheableEx(ThrottleContext, retriever, collection, threshold, cachePolicy, validator, AfterInsertOrderBook, key);

            var data = translator(cacheResult.Contents);
            var result = new CacheResult<CossEngineOrderBook>
            {
                CacheAge = cacheResult.CacheAge,
                Contents = cacheResult.Contents,
                WasFromCache = cacheResult.WasFromCache,
                Data = data,
                AsOf = cacheResult.AsOf
            };

            return result;
        }

        public List<TradingPair> GetTradingPairs(CachePolicy cachePolicy)
        {
            //var lotSizes = GetLotSizes();
            //var minimumTradeBaseSymbolValues = GetMinumumTradesBaseSymbolValues();
            //var priceTicks = GetPriceTicks();
            var minimumTradeQuantities = GetMinimumTradeQuantities();

            var exchangeInfoWithAsOf = GetExchangeInfo(cachePolicy);

            var tradingPairs = (exchangeInfoWithAsOf?.Data?.Symbols ?? new List<CossApiExchangeInfo.CossApiSymbol>())
                .Select(item =>
                {
                    var nativeCombo = item.Symbol;
                    var pieces = nativeCombo.Split('_');

                    var nativeSymbol = pieces[0].ToUpper();
                    var nativeBaseSymbol = pieces[1].ToUpper();

                    var canon = _cossMap.GetCanon(nativeSymbol);
                    var baseCanon = _cossMap.GetCanon(nativeBaseSymbol);

                    var lotSize = item.AmountLimitDecimal.HasValue
                        ? (decimal?)Math.Pow(0.1, item.AmountLimitDecimal.Value)
                        : null;

                    //var minimumTradeBaseSymbolValue =
                    //    minimumTradeBaseSymbolValues.ContainsKey($"{nativeSymbol.ToUpper()}-{nativeBaseSymbol.ToUpper()}")
                    //        ? minimumTradeBaseSymbolValues[$"{nativeSymbol.ToUpper()}-{nativeBaseSymbol.ToUpper()}"]
                    //    : minimumTradeBaseSymbolValues.ContainsKey(nativeSymbol.ToUpper())
                    //        ? minimumTradeBaseSymbolValues[nativeSymbol.ToUpper()]
                    //    : (decimal?)null;

                    var baseCurrency = exchangeInfoWithAsOf.Data.BaseCurrencies.Single(queryBaseCurrency =>
                        string.Equals(queryBaseCurrency.CurrencyCode, nativeBaseSymbol, StringComparison.InvariantCultureIgnoreCase));

                    decimal? minimumTradeBaseSymbolValue = baseCurrency != null && baseCurrency.MinimumTotalOrder > 0
                        ? baseCurrency.MinimumTotalOrder
                        : (decimal?)null;

                    var priceTick = item.PriceLimitDecimal.HasValue
                        ? (decimal?)Math.Pow(0.1, item.PriceLimitDecimal.Value)
                        : null;

                    var minimumTradeQuantity =
                        minimumTradeQuantities.ContainsKey($"{nativeSymbol.ToUpper()}-{nativeBaseSymbol.ToUpper()}")
                            ? minimumTradeQuantities[$"{nativeSymbol.ToUpper()}-{nativeBaseSymbol.ToUpper()}"]
                        : minimumTradeQuantities.ContainsKey(nativeSymbol.ToUpper())
                            ? minimumTradeQuantities[nativeSymbol.ToUpper()]
                        : (decimal?)null;

                    return new TradingPair
                    {
                        CanonicalCommodityId = canon?.Id,
                        Symbol = !string.IsNullOrWhiteSpace(canon?.Symbol) ? canon.Symbol : nativeSymbol,
                        NativeSymbol = nativeSymbol,
                        CommodityName = !string.IsNullOrWhiteSpace(canon?.Name) ? canon.Symbol : nativeSymbol,
                        NativeCommodityName = nativeSymbol,
                        CanonicalBaseCommodityId = baseCanon?.Id,
                        BaseSymbol = !string.IsNullOrWhiteSpace(baseCanon?.Symbol) ? baseCanon.Symbol : nativeBaseSymbol,
                        NativeBaseSymbol = nativeBaseSymbol,
                        BaseCommodityName = !string.IsNullOrWhiteSpace(baseCanon?.Name) ? baseCanon.Symbol : nativeSymbol,
                        NativeBaseCommodityName = nativeBaseSymbol,
                        LotSize = lotSize,
                        MinimumTradeBaseSymbolValue = minimumTradeBaseSymbolValue,
                        PriceTick = priceTick,
                        MinimumTradeQuantity = minimumTradeQuantity
                    };
                })
                .Where(queryTradingPair => queryTradingPair != null)
                .ToList();

            return tradingPairs;
        }

        public List<TradingPair> GetTradingPairsOld(CachePolicy cachePolicy)
        {
            var nativeTradingPairs = GetNativeTradingPairs(cachePolicy);

            var walletResponse = GetNativeWallet(cachePolicy);
            var wallets = walletResponse?.Wallet?.Wallets;            

            var lotSizes = GetLotSizes();
            var minimumTradeBaseSymbolValues = GetMinumumTradesBaseSymbolValues();
            var priceTicks = GetPriceTicks();
            var minimumTradeQuantities = GetMinimumTradeQuantities();

            var tradingPairs = nativeTradingPairs
                .Select(nativeTradingPair =>
                {
                    var matchingSymbolWallet = wallets?.FirstOrDefault(queryWallet => string.Equals(queryWallet.CurrencyCode, nativeTradingPair.First));
                    var matchingBaseSymbolWallet = wallets?.FirstOrDefault(queryWallet => string.Equals(queryWallet.CurrencyCode, nativeTradingPair.Second));

                    if (matchingSymbolWallet != null && (matchingSymbolWallet.CurrencyIsDepositLocked || matchingSymbolWallet.CurrencyIsWithdrawalLocked))
                    {
                        // hacky, but it does the trick until the CoinController filters based on the commodity.
                        // return null;
                    }

                    var nativeSymbol = nativeTradingPair.First;
                    var nativeBaseSymbol = nativeTradingPair.Second;

                    var canon = _cossMap.GetCanon(nativeSymbol);
                    var baseCanon = _cossMap.GetCanon(nativeBaseSymbol);

                    var lotSize =
                        lotSizes.ContainsKey($"{nativeSymbol.ToUpper()}-{nativeBaseSymbol.ToUpper()}")
                            ? lotSizes[$"{nativeSymbol.ToUpper()}-{nativeBaseSymbol.ToUpper()}"]
                        : lotSizes.ContainsKey(nativeSymbol.ToUpper())
                            ? lotSizes[nativeSymbol.ToUpper()]
                        : (decimal?)null;

                    var minimumTradeBaseSymbolValue =
                        minimumTradeBaseSymbolValues.ContainsKey($"{nativeSymbol.ToUpper()}-{nativeBaseSymbol.ToUpper()}")
                            ? minimumTradeBaseSymbolValues[$"{nativeSymbol.ToUpper()}-{nativeBaseSymbol.ToUpper()}"]
                        : minimumTradeBaseSymbolValues.ContainsKey(nativeSymbol.ToUpper())
                            ? minimumTradeBaseSymbolValues[nativeSymbol.ToUpper()]
                        : (decimal?)null;

                    var priceTick =
                        priceTicks.ContainsKey($"{nativeSymbol.ToUpper()}-{nativeBaseSymbol.ToUpper()}")
                            ? priceTicks[$"{nativeSymbol.ToUpper()}-{nativeBaseSymbol.ToUpper()}"]
                        : priceTicks.ContainsKey(nativeSymbol.ToUpper())
                            ? priceTicks[nativeSymbol.ToUpper()]
                        : (decimal?)null;

                    var minimumTradeQuantity =
                        minimumTradeQuantities.ContainsKey($"{nativeSymbol.ToUpper()}-{nativeBaseSymbol.ToUpper()}")
                            ? minimumTradeQuantities[$"{nativeSymbol.ToUpper()}-{nativeBaseSymbol.ToUpper()}"]
                        : minimumTradeQuantities.ContainsKey(nativeSymbol.ToUpper())
                            ? minimumTradeQuantities[nativeSymbol.ToUpper()]
                        : (decimal?)null;

                    return new TradingPair
                    {
                        CanonicalCommodityId = canon?.Id,
                        Symbol = !string.IsNullOrWhiteSpace(canon?.Symbol) ? canon.Symbol : nativeSymbol,
                        NativeSymbol = nativeSymbol,                        
                        CommodityName = !string.IsNullOrWhiteSpace(canon?.Name)? canon.Symbol : matchingSymbolWallet?.CurrencyDisplayLabel,
                        NativeCommodityName = nativeSymbol,
                        CanonicalBaseCommodityId = baseCanon?.Id,
                        BaseSymbol = !string.IsNullOrWhiteSpace(baseCanon?.Symbol) ? baseCanon.Symbol : nativeBaseSymbol,
                        NativeBaseSymbol = nativeBaseSymbol,                        
                        BaseCommodityName = matchingBaseSymbolWallet?.CurrencyDisplayLabel,
                        NativeBaseCommodityName = nativeBaseSymbol,
                        LotSize = lotSize,     
                        MinimumTradeBaseSymbolValue = minimumTradeBaseSymbolValue,
                        PriceTick = priceTick,
                        MinimumTradeQuantity = minimumTradeQuantity
                    };
                })
                .Where(queryTradingPair => queryTradingPair != null)
                .ToList();

            return tradingPairs;
        }

        private static TimeSpan GetTradingPairsTimeSpan = TimeSpan.FromHours(2);
        private List<CossTradingPair> GetNativeTradingPairs(CachePolicy cachePolicy = CachePolicy.AllowCache)
        {
            const string Url = "https://exchange.coss.io/api/integrated-market/pairs";

            var getter = new Func<string>(() => _webUtil.Get(Url));
            var validator = new Func<string, bool>(contents => !string.IsNullOrWhiteSpace(contents));
            //var afterInsert = new Action(() =>
            //{
                //var lastWeb = _getTradingPairsContext.AsQueryable().OrderByDescending(item => item.Id).FirstOrDefault();
                //if (lastWeb == null) { return; }

                //var retrievedProjection = _tradingPairsProjection.AsQueryable().Where(item => item.EventId > lastWeb.Id).OrderByDescending(item => item.Id).FirstOrDefault();
                //if (retrievedProjection != null) { return; }

                //var cossTradingPairs = JsonConvert.DeserializeObject<List<CossTradingPair>>(lastWeb.Raw);
                //var tradingPairs = cossTradingPairs.Select(item => new TradingPair { Symbol = item.First, BaseSymbol = item.Second })
                //    .ToList();

                //var projection = new TradingPairsProjection
                //{
                //    EventId = lastWeb.Id,
                //    TradingPairs = tradingPairs
                //};

                //_tradingPairsProjection.InsertOne(projection);
            //});

            var response = _cacheUtil.GetCacheableEx(ThrottleContext, getter, _getTradingPairsContext, GetTradingPairsTimeSpan, cachePolicy, validator
                //, afterInsert
                );

            return JsonConvert.DeserializeObject<List<CossTradingPair>>(response.Contents);
        }

        public decimal? GetWithdrawalFee(string symbol, CachePolicy cachePolicy)
        {
            var fees = GetWithdrawalFees(cachePolicy);
            return fees.ContainsKey(symbol) ? fees[symbol] : (decimal?)null;
        }

        public HoldingInfo GetHoldings(CachePolicy cachePolicy)
        {
            var native = GetNativeBalances(cachePolicy);

            return new HoldingInfo
            {
                TimeStampUtc = native?.AsOfUtc,
                Holdings = (native?.Data ?? new List<CossApiBalanceItem>())
                .Select(item =>
                {
                    var nativeSymbol = item.CurrencyCode;
                    var symbol = _cossMap.ToCanonicalSymbol(nativeSymbol);

                    return new Holding
                    {
                        Symbol = symbol,
                        Total = item.Total,
                        Available = item.Available,
                        InOrders = item.InOrder
                    };
                }).ToList()
            };
        }

        private AsOfWrapper<List<CossApiBalanceItem>> GetNativeBalances(CachePolicy cachePolicy)
        {
            var translator = new Func<string, List<CossApiBalanceItem>>(text =>
                !string.IsNullOrWhiteSpace(text)
                ? JsonConvert.DeserializeObject<List<CossApiBalanceItem>>(text)
                : null);

            var validator = new Func<string, bool>(text =>
            {
                if (string.IsNullOrWhiteSpace(text)) { throw new ApplicationException("Coss have a null or whitespace response when requesting balances."); }

                translator(text);
                return true;
            });

            var retriever = new Func<string>(() =>
            {
                try
                {
                    var apiKey = _configClient.GetApiKey(IntegrationNameRes.Coss);
                    var text = _cossApiClient.GetBalanceRaw(apiKey);
                    if (!validator(text))
                    {
                        throw new ApplicationException("Failed validation when attempting to retrieve Coss Holdings.");
                    }

                    return text;
                }
                catch (WebException webException)
                {
                    using (var reader = new StreamReader(webException.Response.GetResponseStream()))
                    {
                        var webExceptionResponseText = reader.ReadToEnd();
                        _log.Error($"Coss - Failed to get balances.{Environment.NewLine}{webExceptionResponseText}");

                        throw;
                    }
                }
                catch (Exception exception)
                {
                    _log.Error(exception);
                    throw;
                }
            });

            var collectionContext = new MongoCollectionContext(GetDbContext(), "coss--engine-get-balances");
            var cacheResult = _cacheUtil.GetCacheableEx(ThrottleContext, retriever, collectionContext, BalanceThreshold, cachePolicy, validator);

            return new AsOfWrapper<List<CossApiBalanceItem>>
            {
                AsOfUtc = cacheResult?.AsOf,
                Data = translator(cacheResult?.Contents)
            };
        }

        private CossWalletResponse GetNativeWallet(CachePolicy cachePolicy)
        {
            var translator = new Func<string, CossWalletResponse>(text =>
            {
                return text != null
                    ? JsonConvert.DeserializeObject<CossWalletResponse>(text)
                    : null;
            });

            var validator = new Func<string, bool>(text =>
            {
                if (string.IsNullOrWhiteSpace(text)) { throw new ApplicationException("Coss "); }
                var translated = translator(text);

                if (translated.Payload == null)
                {
                    if (!string.IsNullOrWhiteSpace(translated.PayloadText))
                    {
                        throw new ApplicationException(translated.PayloadText);
                    }

                    throw new ApplicationException("Coss wallet response contains a null wallet payload.");
                }

                return true;
            });

            var retriever = new Func<string>(() =>
            {
                try
                {
                    const string Url = "https://profile.coss.io/api/user/wallets";
                    var response = _cossCookieUtil.CossAuthRequest(Url);
                    if (!validator(response))
                    {
                        throw new ApplicationException("Coss returned an invalid response when requesting wallet.");
                    }

                    return response;
                }
                catch (Exception exception)
                {
                    _log.Error($"Failed to get wallet from Coss.{Environment.NewLine}{exception.Message}");
                    _log.Error(exception);
                    throw;
                }
            });

            var collectionContext = new MongoCollectionContext(GetDbContext(), "coss--get-user-wallet");

            CacheResult cacheResult = null;

            try
            {
                cacheResult = _cacheUtil.GetCacheableEx(ThrottleContext, retriever, collectionContext, WalletThreshold, cachePolicy, validator);
            }
            catch(Exception exception)
            {
                _log.Error(exception);
            }

            if (string.IsNullOrWhiteSpace(cacheResult?.Contents))
            {
                cacheResult = _cacheUtil.GetCacheableEx(ThrottleContext, retriever, collectionContext, WalletThreshold, CachePolicy.OnlyUseCache, validator);
            }

            return !string.IsNullOrWhiteSpace(cacheResult?.Contents)
                ? JsonConvert.DeserializeObject<CossWalletResponse>(cacheResult?.Contents)
                : null;
        }

        public List<DepositAddressWithSymbol> GetDepositAddresses(CachePolicy cachePolicy)
        {
            var nativeWithAsOf = GetNativeBalances(cachePolicy);
            return nativeWithAsOf.Data.Select(item =>
            {
                var nativeSymbol = item.CurrencyCode;
                var symbol = _cossMap.ToCanonicalSymbol(nativeSymbol);
                return new DepositAddressWithSymbol
                {
                    Symbol = symbol,
                    Address = item.Address,
                    Memo = item.Memo
                };
            }).ToList();
        }

        public List<DepositAddressWithSymbol> GetDepositAddressesOld(CachePolicy cachePolicy)
        {
            var walletResponse = GetNativeWallet(cachePolicy);
            if (walletResponse == null) { return new List<DepositAddressWithSymbol>(); }

            return walletResponse.Wallet.Wallets
                .Where(item =>
                    !string.IsNullOrWhiteSpace(item.CurrencyCode)
                    && !string.IsNullOrWhiteSpace(item.WalletAddress)
                    && !item.CurrencyIsDepositLocked)
                .Select(item => new DepositAddressWithSymbol
                {
                    Symbol = item.CurrencyCode,
                    Address = item.WalletAddress
                }).ToList();
        }

        public DepositAddress GetDepositAddress(string symbol, CachePolicy cachePolicy)
        {
            return GetDepositAddresses(cachePolicy)
                ?.SingleOrDefault(item => string.Equals(item.Symbol, symbol, StringComparison.InvariantCultureIgnoreCase));
        }

        public void InsertResponseContainer(ResponseContainer responseContainer)
        {
            _holdingContext.Insert(responseContainer);
        }

        private List<Holding> ParseHodlings(string holdingsText)
        {
            var startMarker = "CRYPTOCURRENCY BALANCE AVAILABLE IN ORDERS TOTAL DEPOSIT WITHDRAW HISTORY";
            var startMarkerPos = holdingsText.IndexOf(startMarker);
            var startPos = startMarkerPos + startMarker.Length;

            var endMarker = "FIAT BALANCE AVAILABLE IN ORDERS TOTAL";
            var endMarkerPos = holdingsText.IndexOf(endMarker);

            var middle = holdingsText.Substring(startPos, endMarkerPos - startPos);
            var lines = middle.Split('\r').Where(item => !string.IsNullOrWhiteSpace(item)).Select(item => item.Trim()).ToList();

            var cossHoldings =
            lines.Select(line => {
                var pieces = line.Split(' ');
                var symbol = pieces[0];
                var availalbleText = pieces[1];
                var inOrdersText = pieces[2];
                var quantityText = pieces[3];
                var quantity = decimal.Parse(quantityText, NumberStyles.Float);
                var available = decimal.Parse(availalbleText, NumberStyles.Float);
                var inOrders = decimal.Parse(inOrdersText, NumberStyles.Float);
                return new { Symbol = symbol, Available = available, InOrders = inOrders, Total = quantity };
            })
            .Where(item => item.Total > 0)
            .Select(item => new Holding { Asset = item.Symbol, Available = item.Available, InOrders = item.InOrders, Total = item.Total })
            .ToList();

            return cossHoldings;
        }

        public Holding GetHolding(string symbol, CachePolicy cachePolicy)
        {
            var holdings = GetHoldings(cachePolicy);
            return holdings?.GetHoldingForSymbol(symbol) ?? null;
        }

        public bool BuyMarket(TradingPair tradingPair, decimal quantity)
        {
            throw new NotImplementedException();
        }

        public bool SellMarket(TradingPair tradingPair, decimal quantity)
        {
            throw new NotImplementedException();
        }

        public RefreshCacheResult RefreshOrderBook(TradingPair tradingPair)
        {
            var nativeSymbol = _cossMap.ToNativeSymbol(tradingPair.Symbol);
            var nativeBaseSymbol = _cossMap.ToNativeSymbol(tradingPair.BaseSymbol);

            // Refresh before it expires. Throw in some randomness to ensure that everything doesn't need to be refreshed at the same time.
            var threshold = _random.Next() % 2 == 0 ? MarketCacheThreshold : MarketThreshold;

            var cacheResult = GetNativeMarketInfo(nativeSymbol, nativeBaseSymbol, CachePolicy.AllowCache);
            return new RefreshCacheResult
            {
                AsOf = cacheResult.AsOf,
                CacheAge = cacheResult.CacheAge,
                WasRefreshed = !cacheResult.WasFromCache
            };
        }

        //public CossUserExchangeHistorySnapshot GetUserExchangeHistorySnapshot(CachePolicy cachePolicy)
        //{
        //    var eventContext = new MongoCollectionContext(GetDbContext(), "coss--api-user-exchange-history-events");
        //    var snapshotContext = new MongoCollectionContext(GetDbContext(), "coss--api-user-exchange-history-snapshot");

        //    var applyEvents = new Func<CossUserExchangeHistorySnapshot>(() =>
        //    {
        //        var snapshot = snapshotContext.GetLast<CossUserExchangeHistorySnapshot>()
        //            ?? new CossUserExchangeHistorySnapshot();

        //        var snapShotId = snapshot?.Id ?? default(ObjectId);
        //        var eventsToProcess = eventContext.GetCollection<CacheEventContainer>().AsQueryable()
        //            .Where(item => item.Id > snapShotId).OrderBy(item => item.Id);

        //        var didWeProcessAnyEvents = false;
        //        foreach (var eventToProcess in eventsToProcess)
        //        {
        //            snapshot.Merge(eventToProcess);
        //            snapshot.LastId = eventToProcess.Id;
        //            snapshot.LastEventTimeStampUtc = eventToProcess.StartTimeUtc;

        //            didWeProcessAnyEvents = true;
        //        }

        //        if (didWeProcessAnyEvents)
        //        {
        //            snapshot.Id = default(ObjectId);
        //            snapshot.TimeStampUtc = DateTime.UtcNow;
        //            snapshotContext.Insert(snapshot);

        //            if (snapshot.Id != default(ObjectId))
        //            {
        //                var snapshotBsonCollection = snapshotContext.GetCollection<BsonDocument>();
        //                var filter = Builders<BsonDocument>.Filter.Lt("_id", snapshot.Id);

        //                snapshotBsonCollection.DeleteMany(filter);
        //            }
        //        }

        //        return snapshot;
        //    });

        //    if (cachePolicy == CachePolicy.OnlyUseCache || cachePolicy == CachePolicy.OnlyUseCacheUnlessEmpty)
        //    {
        //        var cachedSnapShot = applyEvents();
        //        if (cachePolicy == CachePolicy.OnlyUseCache || 
        //            (cachedSnapShot != null && cachedSnapShot.LastId > default(ObjectId)))
        //        {
        //            return cachedSnapShot;
        //        }
        //    }

        //    var translator = new Func<string, CossUserExchangeHistoryResponse>(text =>
        //        !string.IsNullOrWhiteSpace(text)
        //            ? JsonConvert.DeserializeObject<CossUserExchangeHistoryResponse>(text)
        //            : null);

        //    var validator = new Func<string, bool>(text =>
        //    {
        //        if (string.IsNullOrWhiteSpace(text)) { throw new ApplicationException("Coss - got null response when requesting user exchange history."); }
        //        var translated = translator(text);
        //        if (translated == null) { throw new ApplicationException("Coss - Received an invalid response when requesting user exchange history."); }
        //        if (!translated.successful) { throw new ApplicationException("Coss response indiciated failure when requesting user exchange history."); }

        //        return true;
        //    });

        //    var retriever = new Func<string>(() =>
        //    {
        //        // const string Url = "https://profile.coss.io/api/user/history/exchange";
        //        // var responseText = _cossCookieUtil.CossAuthRequest(Url);
        //        var apiKey = _configClient.GetApiKey(IntegrationNameRes.Coss);

        //        throw new NotImplementedException();
        //        //var responseText = _cossApiClient.GetCompletedOrdersRaw(apiKey);
        //        //if (!validator(responseText))
        //        //{
        //        //    throw new ApplicationException("Coss - Validation failed when requesting user exchange history.");
        //        //}

        //        //return responseText;
        //    });

        //    var afterInsert = new Action<CacheEventContainer>(container =>
        //    {
        //        applyEvents();
        //    });

        //    _cacheUtil.GetCacheableEx(ThrottleContext, retriever, eventContext, ExchangeHistoryThreshold, cachePolicy, validator, afterInsert);

        //    return snapshotContext.GetLast<CossUserExchangeHistorySnapshot>();
        //}

        public HistoryContainer GetUserTradeHistoryV2(CachePolicy cachePolicy)
        {
            var tradeTypeDictionary = new Dictionary<string, TradeTypeEnum>(StringComparer.InvariantCultureIgnoreCase)
            {
                { "BUY", TradeTypeEnum.Buy  },
                { "SELL", TradeTypeEnum.Sell  }
            };

            var snapshot = UserTradeHistorySnapshotCollectionContext.GetLast<CossNativeUserTradeHistorySnapshot>();

            var historyContainer = new HistoryContainer { History = new List<HistoricalTrade>() };
            var allTrades = new List<HistoricalTrade>();
            foreach (var key in snapshot.SnapshotItems.Keys)
            {
                var pieces = key.Split('-');
                var nativeSymbol = pieces[0].Trim().ToUpper();
                var nativeBaseSymbol = pieces[1].Trim().ToUpper();

                var symbol = _cossMap.ToCanonicalSymbol(nativeSymbol);
                var baseSymbol = _cossMap.ToCanonicalSymbol(nativeBaseSymbol);

                var snapshotItem = snapshot.SnapshotItems[key];
                var response = JsonConvert.DeserializeObject<CossApiGetCompletedOrdersResponse>(snapshotItem.Raw);

                if (!historyContainer.AsOfUtc.HasValue || snapshotItem.AsOfUtc < historyContainer.AsOfUtc.Value)
                {
                    historyContainer.AsOfUtc = snapshotItem.AsOfUtc;
                }

                var historicalTrades = (response?.List ?? new List<CossApiCompletedOrder>())
                    .Where(item => item.Executed.HasValue && item.Executed != 0)
                    .Select(item =>
                        {
                            var tradeType = tradeTypeDictionary.ContainsKey(item.OrderSide)
                                ? tradeTypeDictionary[item.OrderSide]
                                : TradeTypeEnum.Unknown;

                            var timeStamp = DateTimeUtil.UnixTimeStamp13DigitToDateTime(item.CreateTime);

                            return new HistoricalTrade
                            {
                                Symbol = symbol,
                                BaseSymbol = baseSymbol,
                                Price = item.OrderPrice ?? 0,
                                NativeId = item.OrderId,
                                Quantity = item.Executed ?? 0,
                                TradeType = tradeType,
                                TimeStampUtc = timeStamp ?? default(DateTime)
                            };
                        });

                allTrades.AddRange(historicalTrades);
            }

            var sortedTrades = allTrades.OrderByDescending(item => item.TimeStampUtc).ToList();

            historyContainer.History = sortedTrades;

            return historyContainer;
        }

        //public HistoryContainer GetUserTradeHistoryV2Old(CachePolicy cachePolicy)
        //{
        //    var snapshot = GetUserExchangeHistorySnapshot(cachePolicy);
        //    if (snapshot == null) { return new HistoryContainer(); }

        //    var tradeTypeDitionary = new Dictionary<string, TradeTypeEnum>(StringComparer.InvariantCultureIgnoreCase)
        //    {
        //        { "B", TradeTypeEnum.Buy },
        //        { "S", TradeTypeEnum.Sell }
        //    };

        //    var converter = new Func<CossExchangeHistoryItem, HistoricalTrade>(item =>
        //    {
        //        var symbol = !string.IsNullOrWhiteSpace(item.from_code)
        //            ? _cossMap.ToCanonicalSymbol(item.from_code)
        //            : null;

        //        var baseSymbol = !string.IsNullOrWhiteSpace(item.to_code)
        //            ? _cossMap.ToCanonicalSymbol(item.to_code)
        //            : null;

        //        return new HistoricalTrade
        //        {
        //            TimeStampUtc = item.created_at,
        //            Quantity = item.amount,
        //            TradingPair = new TradingPair(item.from_code, item.to_code),
        //            Symbol = symbol,
        //            BaseSymbol = item.to_code,
        //            Price = item.price,
        //            FeeQuantity = item.transaction_fee_total,
        //            NativeId = item.id,
        //            TradeType = tradeTypeDitionary.ContainsKey(item.order_direction)
        //                    ? tradeTypeDitionary[item.order_direction]
        //                    : TradeTypeEnum.Unknown
        //        };
        //    });

        //    var historyItems = snapshot?.Items?.Select(item => converter(item)).ToList()
        //        ?? new List<HistoricalTrade>();

        //    var container = new HistoryContainer
        //    {
        //        AsOfUtc = snapshot.LastEventTimeStampUtc,
        //        History = historyItems
        //    };

        //    return container;
        //}

        public List<HistoricalTrade> GetUserTradeHistory(CachePolicy cachePolicy)
        {
            var apiKey = _configClient.GetApiKey(IntegrationNameRes.Coss);
            var contents = _cossApiClient.GetCompletedOrdersRaw(apiKey, "COSS", "ETH");

            return GetUserTradeHistoryV2(cachePolicy)?.History;
        }

        private HistoricalTrade ToModel(CossExchangeHistoryItem item)
        {
            var tradeTypeDitionary = new Dictionary<string, TradeTypeEnum>(StringComparer.InvariantCultureIgnoreCase)
            {
                { "B", TradeTypeEnum.Buy },
                { "S", TradeTypeEnum.Sell }
            };

            var trade = new HistoricalTrade
            {
                TimeStampUtc = item.created_at,
                Quantity = item.amount,
                TradingPair = new TradingPair(item.from_code, item.to_code),
                Symbol = item.from_code,
                BaseSymbol = item.to_code,
                Price = item.price,
                FeeQuantity = item.transaction_fee_total,
                TradeType = tradeTypeDitionary.ContainsKey(item.order_direction)
                    ? tradeTypeDitionary[item.order_direction]
                    : TradeTypeEnum.Unknown
            };

            return trade;
        }

        // Changed password, enabled tfa, etc.
        public string GetNativeUserActionsHistory(int limit = 50, int offset = 0)
        {
            var url = $"https://profile.coss.io/api/user/history/actions?&limit={limit}&offset={offset}";
            return _cossCookieUtil.CossAuthRequest(url);
        }

        public CossDepositAndWithdrawalHistoryResponse GetNativeUserDepositsAndWithdrawalsHistory(CachePolicy cachePolicy)
            // (int limit = 50, int offset = 0)
        {
            var translator = new Func<string, CossDepositAndWithdrawalHistoryResponse>(text =>
                !string.IsNullOrWhiteSpace(text)
                ? JsonConvert.DeserializeObject<CossDepositAndWithdrawalHistoryResponse>(text)
                : null);

            var validator = new Func<string, bool>(text =>
            {
                if (string.IsNullOrWhiteSpace(text)) { throw new ApplicationException("Received a null response when requesting Coss deposit and withdrawal history."); }
                var translated = translator(text);
                return translated != null;
            });

            var retriever = new Func<string>(() =>
            {
                try
                {
                    // var url = $"https://profile.coss.io/api/user/history/deposits-and-withdrawals?&limit={limit}&offset={offset}";
                    var url = $"https://profile.coss.io/api/user/history/deposits-and-withdrawals";
                    var text = _cossCookieUtil.CossAuthRequest(url);
                    if (!validator(text))
                    {
                        throw new ApplicationException("Response from request for Coss deposit and withdrawal history failed validation.");
                    }

                    return text;
                }
                catch (Exception exception)
                {
                    _log.Error($"Failed to retrieve deposit and withdrawal history from Coss. {exception.Message}");
                    throw;
                }
            });

            var collectionContext = new MongoCollectionContext(GetDbContext(), "coss--get-deposits-and-withdrawals");
            var cacheResult = _cacheUtil.GetCacheableEx(ThrottleContext, retriever, collectionContext, UserDepositAndWithdrawalHistoryThreshold, cachePolicy, validator);

            return translator(cacheResult?.Contents);
        }

        public List<OpenOrdersForTradingPair> GetOpenOrdersV2()
        {
            var snapshotContext = _engineOpenOrdersSnapshotContext;
            var snapshot = snapshotContext.GetLast<OpenOrdersSnapshot>();

            if (snapshot?.SnapshotItems?.Keys == null) { return new List<OpenOrdersForTradingPair>(); }

            var items = new List<OpenOrdersForTradingPair>();
            foreach (var key in snapshot.SnapshotItems.Keys)
            {
                var snapshotItem = snapshot.SnapshotItems[key];

                var apiOpenOrdersResponse = !string.IsNullOrWhiteSpace(snapshotItem?.Raw)
                    ? JsonConvert.DeserializeObject<CossEngineGetOpenOrdersResponse>(snapshotItem.Raw)
                    : null;

                var combo = (apiOpenOrdersResponse?.List ?? new List<CossEngineOpenOrder>()).Select(CossApiOpenOrderToOpenOrderForTradingPair)
                        .ToList();

                var openOrdersForTradingPair = new OpenOrdersForTradingPair();
                openOrdersForTradingPair.Symbol = snapshotItem.Symbol;
                openOrdersForTradingPair.BaseSymbol = snapshotItem.BaseSymbol;
                openOrdersForTradingPair.AsOfUtc = snapshotItem.AsOfUtc;
                openOrdersForTradingPair.OpenOrders = new List<OpenOrder>();
                foreach (var item in combo)
                {
                    var openOrder = new OpenOrder
                    {
                        OrderId = item.OrderId,
                        OrderType = item.OrderType,
                        Price = item.Price,
                        Quantity = item.Quantity
                    };

                    openOrdersForTradingPair.OpenOrders.Add(openOrder);
                }

                items.Add(openOrdersForTradingPair);
            }

            return items;
        }

        private static Dictionary<string, OrderType> CossApiOrderTypeDictionary = new Dictionary<string, OrderType>(StringComparer.InvariantCultureIgnoreCase)
        {
            { "BUY", OrderType.Bid },
            { "SELL", OrderType.Ask }
        };

        public OpenOrdersWithAsOf GetOpenOrdersForTradingPairV2(string symbol, string baseSymbol, CachePolicy cachePolicy)
        {
            return UseApiForOpenOrders 
                ? GetApiOpenOrdersForTradingPairV2(symbol, baseSymbol, cachePolicy)
                : GetWebOpenOrdersForTradingPairV2(symbol, baseSymbol, cachePolicy);
        }

        private OpenOrdersWithAsOf GetApiOpenOrdersForTradingPairV2(string symbol, string baseSymbol, CachePolicy cachePolicy)
        {
            var nativeSymbol = _cossMap.ToNativeSymbol(symbol);
            var nativeBaseSymbol = _cossMap.ToNativeSymbol(baseSymbol);

            var openOrders = GetApiOpenOrders(nativeSymbol, nativeBaseSymbol, cachePolicy);

            return new OpenOrdersWithAsOf
            {
                AsOfUtc = openOrders.AsOfUtc,
                OpenOrders = openOrders?.Data?.List != null
                    ? openOrders.Data.List.Select(item =>
                    {
                        var orderIdCombo = new CossExtendedOrderId
                        {
                            Id = item.OrderId,
                            NativeSymbol = nativeSymbol,
                            NativeBaseSymbol = nativeBaseSymbol
                        };

                        var orderIdComboText = JsonConvert.SerializeObject(orderIdCombo);

                        return new OpenOrder
                        {
                            OrderId = orderIdComboText,
                            OrderType = CossApiOrderTypeDictionary.ContainsKey(item.OrderSide) ? CossApiOrderTypeDictionary[item.OrderSide] : OrderType.Unknown,
                            Quantity = item.OrderSize,
                            Price = item.OrderPrice
                        };
                    }).ToList()
                    : new List<OpenOrder>()
            };
        }

        private OpenOrdersWithAsOf GetWebOpenOrdersForTradingPairV2(string symbol, string baseSymbol, CachePolicy cachePolicy)
        {
            var nativeSymbol = _cossMap.ToNativeSymbol(symbol);
            var nativeBaseSymbol = _cossMap.ToNativeSymbol(baseSymbol);

            var openOrders = GetWebOpenOrders(nativeSymbol, nativeBaseSymbol, cachePolicy);

            return new OpenOrdersWithAsOf
            {
                AsOfUtc = openOrders.AsOfUtc,
                OpenOrders = openOrders?.Data != null
                ? openOrders.Data.Select(item =>
                {
                    var openOrderForTradingPair = CossWebOpenOrderToOpenOrderForTradingPair(item);
                    return new OpenOrder
                    {
                        OrderId = openOrderForTradingPair.OrderId,
                        OrderType = openOrderForTradingPair.OrderType,
                        Price = openOrderForTradingPair.Price,
                        Quantity = openOrderForTradingPair.Quantity
                    };                    
                }).ToList()
                : new List<OpenOrder>()
             };
        }

        public List<OpenOrderForTradingPair> GetOpenOrders(CachePolicy cachePolicy)
        {
            if (cachePolicy == CachePolicy.OnlyUseCache || cachePolicy == CachePolicy.OnlyUseCacheUnlessEmpty
                || cachePolicy == CachePolicy.AllowCache)
            {
                var openOrdersForTradingPair = GetOpenOrdersV2();
                if (cachePolicy == CachePolicy.AllowCache)
                {
                    foreach (var group in openOrdersForTradingPair)
                    {
                        if (!group.AsOfUtc.HasValue || (DateTime.UtcNow - group.AsOfUtc) > OpenOrdersThreshold)
                        {
                            var updatedGroup = GetOpenOrdersForTradingPairV2(group.Symbol, group.BaseSymbol, cachePolicy);
                            group.AsOfUtc = updatedGroup.AsOfUtc;
                            group.OpenOrders = updatedGroup.OpenOrders;
                        }
                    }
                }

                var openOrders = openOrdersForTradingPair.SelectMany(
                    ooftp =>
                    {
                        return ooftp.OpenOrders.Select(item => new OpenOrderForTradingPair
                        {
                            Symbol = ooftp.Symbol,
                            BaseSymbol = ooftp.BaseSymbol,
                            OrderId = item.OrderId,
                            OrderType = item.OrderType,
                            OrderTypeText = item.OrderTypeText,
                            Price = item.Price,
                            Quantity = item.Quantity
                        }).ToList();
                    }).ToList();

                return openOrders;
            }

            return GetOpenOrdersOld(cachePolicy);
        }

        public List<OpenOrderForTradingPair> GetOpenOrdersOld(CachePolicy cachePolicy)
        {            
            var tradingPairs = cachePolicy == CachePolicy.ForceRefresh || cachePolicy == CachePolicy.AllowCache
                ? GetTradingPairs(CachePolicy.AllowCache)
                : GetTradingPairs(cachePolicy);

            var allOpenOrders = new List<OpenOrderForTradingPair>();
            for (var i = 0; i < tradingPairs.Count; i++)
            {
                var tradingPair = tradingPairs[i];
                var iterationOpenOrders = GetOpenOrders(tradingPair.Symbol, tradingPair.BaseSymbol, cachePolicy);
                if (iterationOpenOrders != null && iterationOpenOrders.Any())
                {
                    allOpenOrders.AddRange(iterationOpenOrders);
                }
            }

            return allOpenOrders;
        }

        private OpenOrderForTradingPair CossApiOpenOrderToOpenOrderForTradingPair(CossEngineOpenOrder apiOpenOrder)
        {
            var pieces = apiOpenOrder.OrderSymbol.Split('_').ToList();
            var nativeSymbol = pieces.Count == 2 ? pieces[0] : null;
            var nativeBaseSymbol = pieces.Count == 2 ? pieces[1] : null;

            var retrievedSymbol = _cossMap.ToCanonicalSymbol(nativeSymbol);
            var retreieveBaseSymbol = _cossMap.ToCanonicalSymbol(nativeBaseSymbol);

            var orderIdCombo = new CossExtendedOrderId
            {
                Id = apiOpenOrder.OrderId,
                NativeSymbol = nativeSymbol,
                NativeBaseSymbol = nativeBaseSymbol
            };

            var orderIdComboText = JsonConvert.SerializeObject(orderIdCombo);

            return new OpenOrderForTradingPair
            {
                OrderId = orderIdComboText,

                Price = apiOpenOrder.OrderPrice,
                Quantity = apiOpenOrder.OrderSize,
                OrderType = CossApiOrderTypeDictionary.ContainsKey(apiOpenOrder.OrderSide)
                    ? CossApiOrderTypeDictionary[apiOpenOrder.OrderSide]
                    : OrderType.Unknown,
                Symbol = retrievedSymbol,
                BaseSymbol = retreieveBaseSymbol,               
            };
        }

        private OpenOrderForTradingPair CossWebOpenOrderToOpenOrderForTradingPair(CossNativeOpenOrder webOpenOrder)
        {
            var nativeOrderTypeDictionary = new Dictionary<string, OrderType>(StringComparer.InvariantCultureIgnoreCase)
            {
                { "sell", OrderType.Ask },
                { "buy", OrderType.Bid }
            };

            var pieces = webOpenOrder.PairId.Split('-').ToList();

            var orderNativeSymbol = pieces.Count == 2 && pieces[0] != null
                ? pieces[0].Trim().ToUpper()
                : null;

            var orderNativeBaseSymbol = pieces.Count == 2 && pieces[1] != null
                ? pieces[1].Trim().ToUpper()
                : null;

            var orderType = nativeOrderTypeDictionary.ContainsKey(webOpenOrder.Type)
                ? nativeOrderTypeDictionary[webOpenOrder.Type]
                : OrderType.Unknown;

            var orderId = new CossExtendedOrderId
            {
                NativeSymbol = orderNativeSymbol,
                NativeBaseSymbol = orderNativeBaseSymbol,
                Id = webOpenOrder.Order_guid
            };

            var orderIdText = JsonConvert.SerializeObject(orderId);

            return new OpenOrderForTradingPair
            {
                Symbol = orderNativeSymbol,
                BaseSymbol = orderNativeBaseSymbol,
                OrderId = orderIdText,
                Price = webOpenOrder.Price ?? default(decimal),
                Quantity = webOpenOrder.Amount ?? default(decimal),
                OrderType = orderType
            };
        }

        public List<OpenOrderForTradingPair> GetOpenOrders(string symbol, string baseSymbol, CachePolicy cachePolicy)
        {
            var nativeSymbol = _cossMap.ToNativeSymbol(symbol);
            var nativeBaseSymbol = _cossMap.ToNativeSymbol(baseSymbol);

            if (UseApiForOpenOrders)
            {
                var apiOpenOrders = GetApiOpenOrders(nativeSymbol, nativeBaseSymbol, cachePolicy);
                return (apiOpenOrders?.Data?.List ?? new List<CossEngineOpenOrder>())
                    .Select(CossApiOpenOrderToOpenOrderForTradingPair).Where(item => item != null)
                    .ToList();
            }

            var webOpenOrders = GetWebOpenOrders(nativeSymbol, nativeBaseSymbol, cachePolicy);
            return (webOpenOrders?.Data ?? new List<CossNativeOpenOrder>())
                .Select(CossWebOpenOrderToOpenOrderForTradingPair)
                .ToList();
        }

        private AsOfWrapper<CossEngineGetOpenOrdersResponse> GetApiOpenOrders(string nativeSymbol, string nativeBaseSymbol, CachePolicy cachePolicy)
        {
            var translator = new Func<string, CossEngineGetOpenOrdersResponse>(text =>
                !string.IsNullOrWhiteSpace(text)
                ? JsonConvert.DeserializeObject<CossEngineGetOpenOrdersResponse>(text)
                : null);

            var validator = new Func<string, bool>(text =>
            {
                if (string.IsNullOrWhiteSpace(text)) { throw new ApplicationException($"Received a null or whitespace response when requesting open order for {nativeSymbol}-{nativeBaseSymbol} from {Name}."); }
                translator(text);

                return true;
            });

            var retriever = new Func<string>(() =>
            {
                ApiKey apiKey;
                try
                {
                    apiKey = _configClient.GetApiKey(IntegrationNameRes.Coss);
                }
                catch (Exception exception)
                {
                    _log.Error("Failed to retrieve api key for {Name}");
                    _log.Error(exception);
                    throw;
                }

                try
                {
                    var contents = _cossApiClient.GetOpenOrdersRaw(apiKey, nativeSymbol, nativeBaseSymbol);
                    if (!validator(contents)) { throw new ApplicationException($"Failed validation when attempting to retrieve open orders from {Name} for {nativeSymbol}-{nativeBaseSymbol}."); }
                    return contents;
                }
                catch (WebException webException)
                {
                    var data = webException.Data;
                    throw;
                }
                catch(Exception exception)
                {
                    _log.Error($"Failed to retrieve open orders from {Name} for {nativeSymbol}-{nativeBaseSymbol}.{Environment.NewLine}{exception.Message}");
                    _log.Error(exception);
                    throw;
                }
            });

            var key = $"{nativeSymbol.ToUpper()}-{nativeBaseSymbol.ToUpper()}";

            var afterInsert = new Action<CacheEventContainer>(cec => _openOrdersSnapshotRepo.AfterInsertOpenOrders(_engineOpenOrdersContext, _engineOpenOrdersSnapshotContext, cec));

            var collectionContext = _engineOpenOrdersContext;
            var cacheResult = _cacheUtil.GetCacheableEx(ThrottleContext, retriever, collectionContext, OpenOrdersThreshold, cachePolicy, validator, afterInsert, key);

            return new AsOfWrapper<CossEngineGetOpenOrdersResponse>
            {
                AsOfUtc = cacheResult?.AsOf,
                Data = translator(cacheResult?.Contents)
            };
        }

        private AsOfWrapper<List<CossNativeOpenOrder>> GetWebOpenOrders(string nativeSymbol, string nativeBaseSymbol, CachePolicy cachePolicy)
        {
            var nativeOrderTypeDictionary = new Dictionary<string, OrderType>(StringComparer.InvariantCultureIgnoreCase)
            {
                { "sell", OrderType.Ask },
                { "buy", OrderType.Bid }
            };

            var key = $"{nativeSymbol.ToLower()}-{nativeBaseSymbol.ToLower()}";

            var textToNative = new Func<string, List<CossNativeOpenOrder>>(text =>
                !string.IsNullOrWhiteSpace(text)
                    ? JsonConvert.DeserializeObject<List<CossNativeOpenOrder>>(text)
                    : null
            );

            var validator = new Func<string, bool>(text =>
            {
                if (string.IsNullOrWhiteSpace(text)) { return false; }

                return true;
            });

            var retriever = new Func<string>(() =>
            {
                var unixTimeStamp = DateTimeUtil.GetUnixTimeStamp13Digit();
                var url = $"https://exchange.coss.io/api/user/orders/{key}?ts={unixTimeStamp}";
                try
                {
                    var response = _cossCookieUtil.CossAuthRequest(url);
                    if (!validator(response)) { throw new ApplicationException($"Response when attempting to get user open orders for {nativeSymbol}-{nativeBaseSymbol} was invalid."); }

                    return response;
                }
                catch (WebException webException)
                {
                    try
                    {
                        using (var responseStream = webException.Response.GetResponseStream())
                        using (var reader = new StreamReader(responseStream))
                        {
                            var failureText = reader.ReadToEnd();
                            _log.Error(failureText);
                        }
                    }
                    catch { }

                    _log.Error(webException);
                    throw;
                }
                catch (Exception exception)
                {
                    _log.Error($"Failed to get {Name} open orders for {nativeSymbol}-{nativeBaseSymbol}.");
                    _log.Error(exception);
                    throw;
                }
            });

            var collectionContext = _webOpenOrdersContext;

            var afterInsert = new Action<CacheEventContainer>(cec => _openOrdersSnapshotRepo.AfterInsertOpenOrders(_webOpenOrdersContext, _webOpenOrdersSnapshotContext, cec));
            var cacheResult = _cacheUtil.GetCacheableEx(ThrottleContext, retriever, collectionContext, OpenOrdersThreshold, cachePolicy, validator, afterInsert, key);
            var nativeOpenOrders = textToNative(cacheResult?.Contents);

            return new AsOfWrapper<List<CossNativeOpenOrder>>
            {
                AsOfUtc = cacheResult?.AsOf,
                Data = nativeOpenOrders
            };
        }

        public void CancelOrder(string orderId)
        {
            if (string.IsNullOrWhiteSpace(orderId)) { throw new ArgumentNullException(orderId); }
            var extendedId = JsonConvert.DeserializeObject<CossExtendedOrderId>(orderId);
            if (extendedId == null
                || string.IsNullOrWhiteSpace(extendedId.Id)
                || string.IsNullOrWhiteSpace(extendedId.NativeSymbol)
                || string.IsNullOrWhiteSpace(extendedId.NativeBaseSymbol))
            { throw new ApplicationException($"Failed to deserialize {typeof(CossExtendedOrderId).Name} from {nameof(orderId)}."); }

            var apiKey = _configClient.GetApiKey(IntegrationNameRes.Coss);
            var responseText = _cossApiClient.CancelOrderRaw(apiKey, extendedId.NativeSymbol, extendedId.NativeBaseSymbol, extendedId.Id);

            if (string.IsNullOrWhiteSpace(responseText))
            {
                throw new ApplicationException($"Coss returned an empty response when attempting to cancel order {orderId}.");
            }

            var response = JsonConvert.DeserializeObject<CossCancelOrderResponse>(responseText);


            if (!string.Equals(response.OrderId, extendedId.Id))
            {
                throw new ApplicationException($"Coss's cancel response did not include the expected order id {orderId}.{Environment.NewLine}{responseText}");
            }
        }

        private IMongoDatabaseContext GetDbContext()
        {
            var connectionString = _getConnectionString.GetConnectionString();
            return new MongoDatabaseContext(connectionString, DatabaseName);
        }

        public List<OrderBookAndTradingPair> GetCachedOrderBooks()
        {
            var context = _allOrderBooksCollectionContext;
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
            var native = JsonConvert.DeserializeObject<CossEngineOrderBook>(text);

            return new OrderBook
            {
                Asks = native?.Asks?.Select(item => new Order { Price = item.Price, Quantity = item.Quantity }).ToList(),
                Bids = native?.Bids?.Select(item => new Order { Price = item.Price, Quantity = item.Quantity }).ToList(),
                AsOf = asOf
            };
        }

        private void AfterInsertOrderBook(CacheEventContainer container)
        {
            var itemContext = GetOrderBookContext();
            var snapShotContext = _allOrderBooksCollectionContext;
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

        private IMongoCollectionContext GetOrderBookContext()
        {
            return new MongoCollectionContext(GetDbContext(), $"coss--engine-order-book");
        }

        public CossSession GetSession()
        {
            const string Url = "https://exchange.coss.io/api/session";
            var response = _cossCookieUtil.CossAuthRequest(Url);
            return !string.IsNullOrWhiteSpace(response)
                ? JsonConvert.DeserializeObject<CossSession>(response)
                : null;
        }

        public void CancelAllOpenBids()
        {
            throw new NotImplementedException();
        }

        public bool BuyLimit(TradingPair tradingPair, decimal quantity, decimal price)
        {
            if (tradingPair == null) { throw new ArgumentNullException(nameof(tradingPair)); }

            return PlaceLimitOrder(tradingPair.Symbol, tradingPair.BaseSymbol, new QuantityAndPrice { Quantity = quantity, Price = price }, true)
                ?.WasSuccessful ?? false;
        }

        public bool SellLimit(TradingPair tradingPair, decimal quantity, decimal price)
        {
            if (tradingPair == null) { throw new ArgumentNullException(nameof(tradingPair)); }

            return PlaceLimitOrder(tradingPair.Symbol, tradingPair.BaseSymbol, new QuantityAndPrice { Quantity = quantity, Price = price }, false)
                ?.WasSuccessful ?? false;
        }

        public LimitOrderResult BuyLimit(string symbol, string baseSymbol, QuantityAndPrice quantityAndPrice)
        {
            return PlaceLimitOrder(symbol, baseSymbol, quantityAndPrice, true);
        }

        public LimitOrderResult SellLimit(string symbol, string baseSymbol, QuantityAndPrice quantityAndPrice)
        {
            return PlaceLimitOrder(symbol, baseSymbol, quantityAndPrice, false);
        }

        private LimitOrderResult PlaceLimitOrder(string symbol, string baseSymbol, QuantityAndPrice quantityAndPrice, bool isBid, int levelsDeep = 1)
        {
            const int MaxLevelsDeep = 4;

            var nativeSymbol = _cossMap.ToNativeSymbol(symbol);
            var nativeBaseSymbol = _cossMap.ToNativeSymbol(baseSymbol);

            var apiKey = _configClient.GetApiKey(IntegrationNameRes.Coss);

            string response = null;
            var slim = new ManualResetEventSlim(false);

            var bidOrAskText = isBid ? "bid" : "ask";
            string webExceptionContents = null;

            bool abort = false;
            bool shouldTryAgain = false;

            LimitOrderResult result = null;
            var throttleThread = LongRunningTask.Run(() =>
            {
                var maxTimeToWaitBeforePlacingOrder = TimeSpan.FromSeconds(500);
                var stopWatch = new Stopwatch();
                stopWatch.Start();

                lock (ThrottleLocker)
                {
                    stopWatch.Stop();
                    if (stopWatch.Elapsed > maxTimeToWaitBeforePlacingOrder || abort)
                    {
                        throw new ApplicationException($"Finally obtained the lock for Coss - PlaceLimitOrder, but it too too long ({stopWatch.Elapsed.ToString()}). Aborting.");
                    }

                    try
                    {                       
                        response = _cossApiClient.CreateOrderRaw(apiKey, nativeSymbol, nativeBaseSymbol, quantityAndPrice.Price, quantityAndPrice.Quantity, isBid);
                        var apiResponse = JsonConvert.DeserializeObject<CossApiLimitOrderResponse>(response);

                        if (apiResponse != null)
                        {
                            var extendedIdText = !string.IsNullOrWhiteSpace(apiResponse?.OrderId)
                                ? JsonConvert.SerializeObject(new CossExtendedOrderId { Id = apiResponse?.OrderId, NativeSymbol = nativeSymbol, NativeBaseSymbol = nativeBaseSymbol })
                                : null;

                            result = new LimitOrderResult
                            {
                                OrderId = extendedIdText,
                                Price = apiResponse?.OrderPrice,
                                Quantity = apiResponse?.OrderSize,
                                Executed = apiResponse?.Executed,
                                WasSuccessful = true,
                                FailureReason = null,
                            };
                        }

                        Console.WriteLine(response);
                    }
                    catch (WebException webException)
                    {
                        abort = true;
                        try
                        {
                            using (var exceptionResponse = webException.Response)
                            using (var responseStream = exceptionResponse.GetResponseStream())
                            using (var reader = new StreamReader(responseStream))
                            {
                                webExceptionContents = reader.ReadToEnd();
                                _log.Error(webExceptionContents);

                                try
                                {
                                    result = new LimitOrderResult
                                    {
                                        WasSuccessful = false,
                                        FailureReason = webExceptionContents
                                    };

                                    var errorInfo = CossApiUtil.InterpretError(webExceptionContents);
                                    if (errorInfo != null
                                        && MathUtil.IsWithinPercentDiff(errorInfo.ExpectedValue, errorInfo.ReceivedValue, 1.0m)
                                        && MathUtil.IsWithinPercentDiff(quantityAndPrice.Quantity * quantityAndPrice.Price, errorInfo.ExpectedValue, 1.0m))
                                    {
                                        shouldTryAgain = true;
                                    }
                                    else
                                    {
                                        //return new 
                                    }
                                }
                                catch (Exception exceptionB)
                                {
                                    _log.Error(exceptionB);
                                }
                            }

                            _log.Error($"Coss returned a failure response when attempting to place a limit {bidOrAskText} for {quantityAndPrice.Quantity} {nativeSymbol} at {quantityAndPrice.Price} {nativeBaseSymbol}.{Environment.NewLine}{webExceptionContents}");
                        }
                        catch { }

                        throw;
                    }
                    finally
                    {
                        slim.Set();
                        Thread.Sleep(ThrottleContext.ThrottleThreshold);
                    }
                }
            });

            if (!slim.Wait(TimeSpan.FromSeconds(1000)))
            {
                abort = true;
                throw new ApplicationException($"Coss - PlaceLimitOrder - Buy limit did not complete in the expected time frame. Aborting.");
            }

            if (shouldTryAgain && levelsDeep < MaxLevelsDeep)
            {
                _log.Info("Retrying coss limit order...");
                return PlaceLimitOrder(symbol, baseSymbol, quantityAndPrice, isBid, levelsDeep + 1);
            }

            _log.Info($"Response from request to create limit {bidOrAskText} on {Name} for {quantityAndPrice.Quantity} {nativeSymbol} at {quantityAndPrice.Price} {nativeBaseSymbol}.{Environment.NewLine}{response}");

            if (result != null)
            {
                return result;
            }

            //var parsedResponse = !string.IsNullOrWhiteSpace(response)
            //    ? JsonConvert.DeserializeObject<CreateApiOrderResponseMessage>(response)
            //    : null;

            return new LimitOrderResult
            {
                WasSuccessful = !abort,
                FailureReason = webExceptionContents
            };
        }

        private LimitOrderResult PlaceWebLimitOrder(string nativeSymbol, string nativeBaseSymbol, QuantityAndPrice quantityAndPrice, bool isBid, decimal? expectedValue = null)
        {
            if (string.IsNullOrWhiteSpace(nativeSymbol)) { throw new ArgumentNullException(nameof(nativeSymbol)); }
            if (string.IsNullOrWhiteSpace(nativeBaseSymbol)) { throw new ArgumentNullException(nameof(nativeBaseSymbol)); }
            if (quantityAndPrice == null) { throw new ArgumentNullException(nameof(quantityAndPrice)); }

            if (quantityAndPrice.Quantity <= 0) { throw new ArgumentOutOfRangeException(nameof(quantityAndPrice.Quantity)); }
            if (quantityAndPrice.Price <= 0) { throw new ArgumentOutOfRangeException(nameof(quantityAndPrice.Price)); }

            var session = GetSession();
            if (session == null || session.Payload == null || session.Payload.guid == null)
            { throw new ApplicationException("Invalid session."); }

            if (!session.Payload.taker_fee_percentage.HasValue)
            { throw new ApplicationException("Session does not include the taker fee."); }

            var feeRatio = session.Payload.taker_fee_percentage.Value;

            var roundedPrice = Math.Round(quantityAndPrice.Price, 8);
            var roundedQuantity = Math.Round(quantityAndPrice.Quantity, 8);

            var pairId = $"{nativeSymbol.Trim().ToLower()}-{nativeBaseSymbol.Trim().ToLower()}";
            var orderTotalWithoutFee = roundedPrice * roundedQuantity;
            var feeValue = orderTotalWithoutFee * feeRatio;
            var orderTotalWithFee = orderTotalWithoutFee + feeValue;

            var orderPriceText = roundedPrice.ToString();
            var orderAmountText = roundedQuantity.ToString();
            var orderTotalWithFeeText = (expectedValue ?? Math.Round(orderTotalWithFee, 8)).ToString();
            var orderTotalWithoutFeeText = Math.Round(orderTotalWithoutFee, 8).ToString();
            var feeValueText = Math.Round(feeValue, 8).ToString();
            var feeRatioText = Math.Round(feeRatio, 8).ToString();

            var buyOrSellText = isBid ? "buy" : "sell";

            var payload = new PlaceLimitOrderPayload
            {
                PairId = pairId,
                TradeType = buyOrSellText,
                OrderType = "limit",
                OrderPrice = orderPriceText,
                OrderAmount = orderAmountText,
                OrderTotalWithFee = orderTotalWithFeeText,
                OrderTotalWithoutFee = orderTotalWithoutFeeText,
                FeeValue = feeValueText,
                Fee = feeRatioText
            };

            var serializedPayload = JsonConvert.SerializeObject(payload);

            var Url = $"https://exchange.coss.io/api/limit-order/{buyOrSellText}";
            const string Verb = "POST";

            try
            {
                var responseText = _cossCookieUtil.CossAuthRequest(Url, Verb, serializedPayload);
                _log.Info($"Coss - Response from placing limit {buyOrSellText} for {quantityAndPrice.Quantity} {nativeSymbol} at {quantityAndPrice.Price} {nativeBaseSymbol}");

                var response = !string.IsNullOrWhiteSpace(responseText)
                    ? JsonConvert.DeserializeObject<CossPlaceOrderResponse>(responseText)
                    : null;

                return new LimitOrderResult
                {
                    WasSuccessful = response?.Successful ?? false,
                    FailureReason = null // response?.PayloadText
                };
            }
            catch (WebException webException)
            {                
                try
                {
                    string webExceptionResponseText = null;
                    using (var reader = new StreamReader(webException.Response.GetResponseStream()))
                    {
                        webExceptionResponseText = reader.ReadToEnd();
                        _log.Error($"Coss - Failed to place limit {buyOrSellText} for {quantityAndPrice.Quantity} {nativeSymbol} at {quantityAndPrice.Price} {nativeBaseSymbol}.{Environment.NewLine}{webExceptionResponseText}");

                        throw;
                    }
                }
                catch { }

                //// new StreamReader(webException.Response.GetResponseStream()).ReadToEnd()

                //if (webException.Message != null && webException.Message.ToUpper().IndexOf("Bad Request".ToUpper()) >= 0)
                //{
                //    _log.Error("Coss responded with Bad Request when attempting to place a limit sell order.");
                //}

                //if (webException.Message != null && webException.Message.ToUpper().IndexOf("Internal Server Error".ToUpper()) >= 0)
                //{
                //    _log.Error("Coss responded with an Internal Server Error when attempting to place a limit sell order.");
                //}

                _log.Error(webException);
                throw;                
            }
            catch (Exception exception)
            {
                _log.Error(exception);
            }

            return new LimitOrderResult
            {
                WasSuccessful = false,
                FailureReason = null
            };
        }

        // TODO: Migrate this to pass in the canonical symbol instead of the commodity.
        public bool Withdraw(Commodity commodity, decimal quantity, DepositAddress address)
        {
            return Withdraw(commodity.Symbol, quantity, address);
        }

        private bool Withdraw(string symbol, decimal quantity, DepositAddress address)
        {
            if (string.IsNullOrWhiteSpace(symbol)) { throw new ArgumentNullException(nameof(symbol)); }
            if (quantity <= 0) { throw new ArgumentOutOfRangeException(nameof(quantity)); }
            if (address == null) { throw new ArgumentNullException(nameof(address)); }
            if (string.IsNullOrWhiteSpace(address.Address)) { throw new ArgumentNullException(nameof(address.Address)); }

            var nativeSymbol = _cossMap.ToNativeSymbol(symbol);
            var withdrawalFee = GetWithdrawalFee(symbol, CachePolicy.AllowCache);
            var effectiveQuantity = quantity - (withdrawalFee ?? 0);

            if (effectiveQuantity <= 0)
            {
                throw new ApplicationException($"There would be no {symbol} quantity left to withdraw after the withdrawal fee.");
            }

            var responseText = CreateWithdrawal(nativeSymbol, effectiveQuantity, address);

            Console.WriteLine(responseText);

            return true;
        }

        private string CreateWithdrawal(string nativeSymbol, decimal quantity, DepositAddress address)
        {       
            var nativeWallet = GetNativeWallet(CachePolicy.OnlyUseCacheUnlessEmpty);
            var matchingWallet = nativeWallet.Wallet.Wallets.SingleOrDefault(queryWallet =>
                string.Equals(queryWallet.CurrencyCode, nativeSymbol, StringComparison.InvariantCultureIgnoreCase));

            if (matchingWallet == null)
            {
                throw new ApplicationException($"Failed to find Coss Wallet for symbol {nativeSymbol}");
            }

            var walletId = matchingWallet.Id;
            if (walletId == default(Guid))
            {
                throw new ApplicationException($"Wallet Id for Coss symbol {nativeSymbol} is invalid.");
            }

            var tfaToken = _tfaUtil.GetCossTfa();
            if (string.IsNullOrWhiteSpace(tfaToken))
            {
                throw new ApplicationException("Failed to retrieve Coss Tfa token.");
            }

            var payload = new CreateWithdrawalPayload
            {
                Amount = quantity.ToString(),
                WalletGuid = walletId,
                WalletAddress = address.Address,
                TfaToken = tfaToken
            };

            var serializedPayload = JsonConvert.SerializeObject(payload);

            const string Url = "https://profile.coss.io/api/user/withdrawals/crypto";
            const string Verb = "POST";

            var responseText = _cossCookieUtil.CossAuthRequest(Url, Verb, serializedPayload);

            return responseText;
        }

        public AsOfWrapper<CossApiGetCompletedOrdersResponse> GetNativeCompletedOrders(string nativeSymbol, string nativeBaseSymbol, CachePolicy cachePolicy)
        {
            var translator = new Func<string, CossApiGetCompletedOrdersResponse>(text =>
                !string.IsNullOrWhiteSpace(text) 
                    ? JsonConvert.DeserializeObject<CossApiGetCompletedOrdersResponse>(text)
                    : null
            );

            var validator = new Func<string, bool>(text =>
            {
                if (string.IsNullOrWhiteSpace(text)) { throw new ApplicationException($"Coss returned a null or whitespace response when requesting trading pair history for {nativeSymbol}-{nativeBaseSymbol}."); }
                return translator(text) != null;
            });

            var retriever = new Func<string>(() =>
            {
                try
                {
                    var apiKey = _configClient.GetApiKey(IntegrationNameRes.Coss);
                    var contents = _cossApiClient.GetCompletedOrdersRaw(apiKey, nativeSymbol, nativeBaseSymbol);

                    if (!validator(contents)) { throw new ApplicationException($"Coss get completed orders response for {nativeSymbol}-{nativeBaseSymbol} failed validation."); }

                    return contents;
                }
                catch (Exception exception)
                {
                    _log.Error($"Failed to retrieve completed orders for {nativeSymbol}-{nativeBaseSymbol} from Coss.{Environment.NewLine}{exception.Message}");
                    _log.Error(exception);
                    throw;
                }
            });

            var key = $"{nativeSymbol.ToUpper()}-{nativeBaseSymbol.ToUpper()}";
            var cacheResult = _cacheUtil.GetCacheableEx(ThrottleContext, retriever, _nativeCompletedOrdersContext, NativeTradingPairCompletedOrdersThreshold, cachePolicy, validator, UpdateUserTradeHistorySnapshot, key);

            return new AsOfWrapper<CossApiGetCompletedOrdersResponse>
            {
                AsOfUtc = cacheResult?.AsOf,
                Data = translator(cacheResult?.Contents)
            };
        }

        private MongoCollectionContext UserTradeHistorySnapshotCollectionContext => new MongoCollectionContext(GetDbContext(), "coss--user-trade-history-snapshot");

        private void UpdateUserTradeHistorySnapshot(CacheEventContainer cacheEventContainer)
        {
            var snapShotContext = UserTradeHistorySnapshotCollectionContext;
            var itemContext = _nativeCompletedOrdersContext;
            var snapShot = snapShotContext
                .GetLast<CossNativeUserTradeHistorySnapshot>();

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

            if (snapShot == null) { snapShot = new CossNativeUserTradeHistorySnapshot(); }

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

        public HistoryContainer GetUserTradeHistoryForTradingPair(string symbol, string baseSymbol, CachePolicy cachePolicy)
        {
            var nativeSymbol = _cossMap.ToNativeSymbol(symbol);
            var nativeBaseSymbol = _cossMap.ToNativeSymbol(baseSymbol);

            var nativeResult = GetNativeCompletedOrders(nativeSymbol, nativeBaseSymbol, cachePolicy);

            var tradeTypeDictionary = new Dictionary<string, TradeTypeEnum>(StringComparer.InvariantCultureIgnoreCase)
            {
                { "BUY", TradeTypeEnum.Sell  },
                { "SELL", TradeTypeEnum.Sell  }
            };

            return new HistoryContainer
            {
                AsOfUtc = nativeResult.AsOfUtc,
                History = nativeResult.Data.List.Select(item =>
                {
                    var tradeType = tradeTypeDictionary.ContainsKey(item.OrderSide)
                        ? tradeTypeDictionary[item.OrderSide]
                        : TradeTypeEnum.Unknown;

                    var timeStamp = DateTimeUtil.UnixTimeStamp13DigitToDateTime(item.CreateTime);

                    return new HistoricalTrade
                    {
                        Symbol = symbol,
                        BaseSymbol = baseSymbol,
                        Price = item.OrderPrice ?? 0,
                        NativeId = item.OrderId,
                        Quantity = item.Executed ?? 0,
                        TradeType = tradeType,
                        TimeStampUtc = timeStamp ?? default(DateTime)
                    };
                }).ToList()
            };
        }

        private IMongoCollectionContext _nativeCompletedOrdersContext => new MongoCollectionContext(GetDbContext(), "coss--completed-orders");
    }
}
