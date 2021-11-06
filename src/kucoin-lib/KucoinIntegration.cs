using kucoin_lib.Models;
using log_lib;
using mongo_lib;
using MongoDB.Driver;
using Newtonsoft.Json;
using rabbit_lib;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using trade_constants;
using trade_contracts;
using trade_email_lib;
using cache_lib.Models;
using trade_model;
using trade_node_integration;
using trade_res;
using wait_for_it_lib;
using web_util;
using cache_lib;
using MongoDB.Bson;
using cache_lib.Models.Snapshots;
using config_client_lib;
using KucoinClientModelLib;
using System.Diagnostics;
using kucoin_lib.Client;

namespace kucoin_lib
{
    public class KucoinIntegration : IKucoinIntegration
    {
        private const string CcxtExchangeName = "kucoin";
        private const string DatabaseName = "kucoin";

        private static readonly TimeSpan MarketThreshold = TimeSpan.FromMinutes(20);
        private static readonly TimeSpan MarketCacheThreshold = TimeSpan.FromMinutes(17.5);
        private static readonly TimeSpan ThrottleThresh = TimeSpan.FromSeconds(2.5);
        private static readonly TimeSpan BalanceThreshold = TimeSpan.FromMinutes(10);
        private static readonly TimeSpan OpenOrdersThreshold = TimeSpan.FromMinutes(20);
        private static readonly TimeSpan CurrenciesThreshold = TimeSpan.FromMinutes(20);
        private static readonly TimeSpan AccountsThreshold = TimeSpan.FromMinutes(10);

        private static readonly TimeSpan TickerThreshold = TimeSpan.FromMinutes(20);

        private static readonly ThrottleContext ThrottleContext = new ThrottleContext
        {
            Locker = new object(),
            ThrottleThreshold = ThrottleThresh
        };

        private static readonly Random _random = new Random();

        private readonly IKucoinClient _kucoinClient;
        private readonly ITradeNodeUtil _nodeServiceUtil;
        private readonly ITradeEmailUtil _tradeEmailUtil;
        private readonly IWebUtil _webUtil;
        private readonly CacheUtil _cacheUtil;
        private readonly IRabbitConnectionFactory _rabbitConnectionFactory;
        private readonly ILogRepo _log;
        private readonly IWaitForIt _waitForIt;

        private readonly IConfigClient _configClient;

        // private Task<KuCoinMap> _mapTask; // = LongRunningTask.Run(() => new KuCoinMap());
        private static readonly object KuCoinMapLocker = new object();
        private KuCoinMap _kucoinMapInternal = null;
        private KuCoinMap _kucoinMap
        {
            get
            {
                if (_kucoinMapInternal != null) { return _kucoinMapInternal; }
                lock (KuCoinMapLocker)
                {
                    if (_kucoinMapInternal != null) { return _kucoinMapInternal; }
                    return _kucoinMapInternal = new KuCoinMap();
                }

            }
        }

        public KucoinIntegration(
            IKucoinClient kucoinClient,
            ITradeNodeUtil tradeNodeUtil,
            ITradeEmailUtil tradeEmailUtil,
            IWebUtil webUtil,
            IConfigClient configClient,
            IWaitForIt waitForIt,
            IRabbitConnectionFactory rabbitConnectionFactory,
            ILogRepo log)
        {
            _kucoinClient = kucoinClient;
            _configClient = configClient;

            _nodeServiceUtil = tradeNodeUtil;
            _tradeEmailUtil = tradeEmailUtil;
            _webUtil = webUtil;
            _waitForIt = waitForIt;
            _rabbitConnectionFactory = rabbitConnectionFactory;
            _log = log;

            _cacheUtil = new CacheUtil();
        }

        public string Name => "Kucoin";
        public Guid Id => new Guid("1A90A7FB-F6B8-4C62-9A92-B1866183F64F");

        private T Time<T>(Func<T> method, string desc)
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            try
            {
                return method();
            }
            finally
            {
                stopWatch.Stop();
                _log.Info($"{desc} -- {stopWatch.ElapsedMilliseconds} ms");
            }
        }

        public List<CommodityForExchange> GetCommodities(CachePolicy cachePolicy)
        {
            var nativeCurrenciesWithAsOf = GetNativeCurrencies(cachePolicy);

            return (nativeCurrenciesWithAsOf?.Data?.Data ?? new List<KucoinClientCurrency>())
                .Select(queryNative =>
                {
                    var nativeSymbol = queryNative.Currency;
                    var nativeName = queryNative.FullName;

                    var canon = _kucoinMap.GetCanon(nativeSymbol);

                    var lotSize = queryNative.Precision.HasValue && queryNative.Precision.Value >= 1
                        ? (decimal?)Math.Pow(0.1, (double)queryNative.Precision.Value)
                        : (decimal?)null;

                    return new CommodityForExchange
                    {
                        CanonicalId = canon?.Id,
                        Symbol = !string.IsNullOrWhiteSpace(canon?.Symbol)
                            ? canon.Symbol
                            : nativeSymbol,
                        Name = !string.IsNullOrWhiteSpace(canon?.Name)
                            ? canon.Name
                            : nativeName,
                        NativeSymbol = nativeSymbol,
                        NativeName = nativeName,
                        WithdrawalFee = queryNative.WithdrawalMinFee,
                        CanWithdraw = queryNative.IsWithdrawEnabled,
                        CanDeposit = queryNative.IsDepositEnabled,
                        LotSize = lotSize
                    };
                }).ToList();                
        }

        private AsOfWrapper<KucoinClientGetCurrenciesResponse> GetNativeCurrencies(CachePolicy cachePolicy)
        {
            var translator = new Func<string, KucoinClientGetCurrenciesResponse>(text =>
                !string.IsNullOrWhiteSpace(text)
                ? JsonConvert.DeserializeObject<KucoinClientGetCurrenciesResponse>(text)
                : new KucoinClientGetCurrenciesResponse());

            var validator = new Func<string, bool>(text =>
            {
                if (string.IsNullOrWhiteSpace(text)) { throw new ApplicationException($"Received a null or whitespace response when requesting currencies from {Name}."); }
                translator(text);

                return true;
            });

            var retriever = new Func<string>(() =>
            {
                try
                {
                    var text = _kucoinClient.GetCurrenciesRaw();
                    if (!validator(text))
                    {
                        throw new ApplicationException($"Response from {Name} when requesting currencies failed validation.");
                    }

                    return text;
                }
                catch (Exception exception)
                {
                    _log.Error(exception);
                    throw;
                }
            });

            var collectionContext = new MongoCollectionContext(DbContext, "kucoin--get-currencies");
            var cacheResult = Time(() => _cacheUtil.GetCacheableEx(ThrottleContext, retriever, collectionContext, CurrenciesThreshold, cachePolicy, validator), "get currencies cache");

            return new AsOfWrapper<KucoinClientGetCurrenciesResponse>
            {
                AsOfUtc = cacheResult?.AsOf,
                Data = translator(cacheResult?.Contents)
            };
        }

        private AsOfWrapper<KucoinClientGetSymbolsResponse> GetNativeSymbols(CachePolicy cachePolicy)
        {
            var translator = new Func<string, KucoinClientGetSymbolsResponse>(text =>
                !string.IsNullOrWhiteSpace(text)
                ? JsonConvert.DeserializeObject<KucoinClientGetSymbolsResponse>(text)
                : new KucoinClientGetSymbolsResponse());

            var validator = new Func<string, bool>(text =>
            {
                if (string.IsNullOrWhiteSpace(text)) { throw new ApplicationException($"Received a null or whitespace response when requesting currencies from {Name}."); }
                translator(text);

                return true;
            });

            var retriever = new Func<string>(() =>
            {
                try
                {
                    var text = _kucoinClient.GetSymbolsRaw();
                    if (!validator(text))
                    {
                        throw new ApplicationException($"Response from {Name} when requesting currencies failed validation.");
                    }

                    return text;
                }
                catch (Exception exception)
                {
                    _log.Error(exception);
                    throw;
                }
            });

            var collectionContext = new MongoCollectionContext(DbContext, "kucoin--get-symbols");
            var cacheResult = _cacheUtil.GetCacheableEx(ThrottleContext, retriever, collectionContext, CurrenciesThreshold, cachePolicy, validator);

            return new AsOfWrapper<KucoinClientGetSymbolsResponse>
            {
                AsOfUtc = cacheResult?.AsOf,
                Data = translator(cacheResult?.Contents)
            };
        }

        private IMongoCollectionContext GetDepositAddressContext()
        {
            return new MongoCollectionContext(DbContext, $"kucoin--get-deposit-address");
        }

        public DepositAddress GetDepositAddress(string symbol, CachePolicy cachePolicy)
        {
            if (string.IsNullOrWhiteSpace(symbol)) { throw new ArgumentNullException(nameof(symbol)); }

            var effectiveSymbol = symbol.Trim().ToUpper();

            var validator = new Func<string, bool>(text => !string.IsNullOrWhiteSpace(text));

            var retriever = new Func<string>(() =>
            {
                try
                {
                    var apiKey = _configClient.GetKucoinApiKey();
                    var result = _kucoinClient.GetDepositAddress(apiKey, effectiveSymbol);

                    if (!validator(result))
                    {
                        throw new ApplicationException($"Failed validation when attempting to get kucoin deposit address for {effectiveSymbol}.");
                    }

                    return result;
                }
                catch (Exception exception)
                {
                    _log.Error($"An exception was thrown when attempting to get kucoin deposit address for {effectiveSymbol}.");
                    _log.Error(exception);
                    throw;
                }
            });

            var context = GetDepositAddressContext();
            var key = effectiveSymbol;

            var cacheResult = _cacheUtil.GetCacheableEx(ThrottleContext, retriever, context, TimeSpan.FromDays(2), cachePolicy, validator, null, key);

            var nativeText = cacheResult.Contents;
            var native = JsonConvert.DeserializeObject<KucoinGetDepositAddressResponse>(nativeText);

            return new DepositAddress
            {
                Address = native?.Data?.Address,
                // TODO: Need to figure out how they return Memo for coins that need it (XEM, etc.).
                Memo = null
            };
        }

        private List<string> _iggies = new List<string> { "ETN", "CTR", "GOD", "ABTC", "BTCP", "MEET", "BCD", "CFD", "SHL", "BTG", "ETF", "BHC" };

        public List<DepositAddressWithSymbol> GetDepositAddresses(CachePolicy cachePolicy)
        {
            var addresses = new List<DepositAddress>();

            var commodities = GetCommodities(cachePolicy)
                .Where(item =>
                    item != null
                    && !string.IsNullOrWhiteSpace(item.Symbol)
                    && !_iggies.Any(iggy => string.Equals(iggy, item.Symbol, StringComparison.InvariantCultureIgnoreCase)))
                .ToList();

            return commodities.Select(item =>
            {
                var depositAddress = GetDepositAddress(item.Symbol, cachePolicy);
                return new DepositAddressWithSymbol
                {
                    Symbol = item.Symbol,
                    Address = depositAddress.Address,
                    Memo = depositAddress.Memo
                };
            }).ToList();
        }

        public HoldingInfo GetHoldings(CachePolicy cachePolicy)
        {
            var nativeAccountsWithAsOf = GetNativeAccounts(cachePolicy);
            if (nativeAccountsWithAsOf == null) { throw new ApplicationException($"{nameof(nativeAccountsWithAsOf)} should not be null."); }

            return new HoldingInfo
            {
                TimeStampUtc = nativeAccountsWithAsOf.AsOfUtc,
                Holdings = (nativeAccountsWithAsOf?.Data?.Data ?? new List<KucoinClientAccount>())
                    .Select(queryNativeAccount => new Holding
                    {
                        Symbol = _kucoinMap.ToCanonicalSymbol(queryNativeAccount.Currency),
                        Total = queryNativeAccount.Balance,
                        Available = queryNativeAccount.Available,
                        InOrders = queryNativeAccount.Holds
                    })
                    .ToList()
            };
        }

        private AsOfWrapper<KucoinClientGetAccountsResponse> GetNativeAccounts(CachePolicy cachePolicy)
        {
            var translator = new Func<string, KucoinClientGetAccountsResponse>(text =>
                !string.IsNullOrWhiteSpace(text)
                ? JsonConvert.DeserializeObject<KucoinClientGetAccountsResponse>(text)
                : null);

            var validator = new Func<string, bool>(text =>
            {
                if (string.IsNullOrWhiteSpace(text)) { throw new ApplicationException($"{Name} returned a null or whitespace response when requesting accounts."); }
                translator(text);
                return true;
            });

            var apiKey = GetApiKey();

            var retriever = new Func<string>(() =>
            {
                try
                {
                    var contents = _kucoinClient.GetAccountsRaw(apiKey);
                    if (!validator(contents)) { throw new ApplicationException("Kucoin's response to request for accounts failed validation."); }
                    return contents;
                }
                catch (Exception exception)
                {
                    _log.Error(exception);
                    throw;
                }
            });

            var collectionContext = new MongoCollectionContext(DbContext, "kucoin--get-accounts");

            var cacheResult = _cacheUtil.GetCacheableEx(ThrottleContext, retriever, collectionContext, AccountsThreshold, cachePolicy, validator);

            return new AsOfWrapper<KucoinClientGetAccountsResponse>
            {
                AsOfUtc = cacheResult?.AsOf,
                Data = translator(cacheResult?.Contents)
            };
        }

        private void AfterDepositAddressInsert(CacheEventContainer container)
        {
            var itemContext = GetDepositAddressContext();
            var snapShotContext = GetAllOrderBooksCollectionContext();
            var snapShot = snapShotContext
                .GetLast<AllDepositAddressesSnapShot>();

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

            if (snapShot == null) { snapShot = new AllDepositAddressesSnapShot(); }

            if (itemsToApply == null || !itemsToApply.Any()) { return; }

            foreach (var item in itemsToApply)
            {
                snapShot.ApplyEvent(item);
            }

            snapShot.Id = default(ObjectId);
            snapShotContext.Insert(snapShot);

            var collection = snapShotContext.GetCollection<BsonDocument>();
            var filter = Builders<BsonDocument>.Filter.Lt("_id", Id);

            collection.DeleteMany(filter);
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

            //collection.DeleteMany(doc => doc["_id"] < snapShot.Id);
            collection.DeleteMany(filter);
        }

        private OrderBook ToOrderBook(string text, DateTime? asOf)
        {
            var native = JsonConvert.DeserializeObject<KucoinClientGetOrderBookResponse>(text);

            var nativeOrderToCanonicalOrder = new Func<List<string>, Order>(nativeOrder =>
            {
                var price = decimal.Parse(nativeOrder[0], NumberStyles.Float);
                var quantity = decimal.Parse(nativeOrder[1], NumberStyles.Float);

                return new Order { Price = price, Quantity = quantity };
            });

            return new OrderBook
            {
                Asks = native?.Data?.Asks?.Select(item => 
                    new Order
                    {
                        Price = item[0],
                        Quantity = item[1]
                    })?.ToList() ?? new List<Order>(),
                Bids = native?.Data?.Bids?.Select(item => 
                    new Order
                    {
                        Price = item[0],
                        Quantity = item[1]
                    })?
                .ToList() ?? new List<Order>(),
                AsOf = asOf
            };
        }

        public OrderBook GetOrderBook(TradingPair tradingPair, CachePolicy cachePolicy)
        {
            var nativeSymbol = _kucoinMap.ToNativeSymbol(tradingPair.Symbol);
            var nativeBaseSymbol = _kucoinMap.ToNativeSymbol(tradingPair.BaseSymbol);

            var tradingPairText = $"{nativeSymbol.ToUpper()}-{nativeBaseSymbol.ToUpper()}";
            var context = GetOrderBookContext();

            var validator = new Func<string, bool>(text =>
            {
                return !string.IsNullOrWhiteSpace(text);
            });

            var retriever = new Func<string>(() =>
            {
                try
                {
                    var contents = _kucoinClient.GetOrderBookRaw(nativeSymbol, nativeBaseSymbol);
                    if (!validator(contents))
                    {
                        _log.Error($"Failed validator when retrieving {Name} order book for trading pair {tradingPair.Symbol}-{tradingPair.BaseSymbol}.");
                    }

                    return contents;
                }
                catch (Exception exception)
                {
                    try
                    {
                        _log.Error($"Failed to retrieve {Name} order book for trading pair { tradingPair.Symbol}-{ tradingPair.BaseSymbol}.");
                    }
                    catch { }
                    _log.Error(exception);
                    throw;
                }
            });

            var cacheResult = _cacheUtil.GetCacheableEx(
                ThrottleContext, 
                retriever, 
                context, 
                MarketThreshold, 
                cachePolicy, 
                validator,
                AfterInsertOrderBook, 
                tradingPairText);

            var orderBook = ToOrderBook(cacheResult?.Contents, cacheResult?.AsOf);

            return orderBook;
        }       
        
        private MongoCollectionContext GetAllOrderBooksCollectionContext()
        {
            return new MongoCollectionContext(DbContext, "kucoin--all-order-books");
        }

        public List<TradingPair> GetTradingPairs(CachePolicy cachePolicy)
        {
            var nativeSymbolsWithAsOf = GetNativeSymbols(cachePolicy);
            return nativeSymbolsWithAsOf.Data.Data.Select(queryNativeSymbol =>
            {
                var nativeSymbol = queryNativeSymbol.BaseCurrency;
                var canon = _kucoinMap.GetCanon(nativeSymbol);

                var nativeBaseSymbol = queryNativeSymbol.QuoteCurrency;
                var baseCanon = _kucoinMap.GetCanon(nativeBaseSymbol);

                return new TradingPair
                {                    
                    CanonicalCommodityId = canon?.Id,
                    Symbol = !string.IsNullOrWhiteSpace(canon?.Symbol) ? canon.Symbol : nativeSymbol,
                    CommodityName = !string.IsNullOrWhiteSpace(canon?.Name) ? canon.Name : nativeSymbol,
                    NativeSymbol = nativeSymbol,

                    CanonicalBaseCommodityId = baseCanon?.Id,
                    BaseSymbol = !string.IsNullOrWhiteSpace(baseCanon?.Symbol) ? baseCanon.Symbol : nativeBaseSymbol,
                    BaseCommodityName = !string.IsNullOrWhiteSpace(baseCanon?.Name) ? baseCanon.Name : nativeBaseSymbol,
                    NativeBaseSymbol = nativeBaseSymbol
                };
            }).ToList();
        }

        public decimal? GetWithdrawalFee(string symbol, CachePolicy cachePolicy)
        {
            var fees = GetWithdrawalFees(cachePolicy);
            return fees.ContainsKey(symbol) ? fees[symbol] : (decimal?)null;
        }

        public Dictionary<string, decimal> GetWithdrawalFees(CachePolicy cachePolicy)
        {
            var nativeCurrenciesWithAsOf = GetNativeCurrencies(cachePolicy);

            var dictionary = new Dictionary<string, decimal>(StringComparer.InvariantCultureIgnoreCase);
            foreach (var nativeCurrency in nativeCurrenciesWithAsOf.Data.Data)
            {
                var nativeSymbol = nativeCurrency.Currency;
                var symbol = _kucoinMap.ToCanonicalSymbol(nativeSymbol);

                var withdrawalFee = nativeCurrency.WithdrawalMinFee;

                if (withdrawalFee.HasValue)
                {
                    dictionary[symbol] = withdrawalFee.Value;
                }
            }

            return dictionary;
        }

        public bool Withdraw(Commodity commodity, decimal quantity, DepositAddress address)
        {
            if (commodity == null) { throw new ArgumentNullException(nameof(commodity)); }

            var symbol = commodity.Symbol;
            var withdrawalFee = GetWithdrawalFee(symbol, CachePolicy.ForceRefresh);
            if (!withdrawalFee.HasValue || withdrawalFee.Value <= 0)
            {
                throw new ApplicationException($"Failed to retrieve the withdrawal fee for {symbol}.");
            }

            var netQuantity = quantity - withdrawalFee.Value;

            if (netQuantity <= 0)
            {
                throw new ApplicationException($"After the withdrawal fee, the net quantity would be {netQuantity}. Aborting withdrawal.");
            }

            // kucoin recently started requring users to put in the quantity after the withdrawal fee.
            // who know when/if they'll change that back...
            var responseContents = _nodeServiceUtil.Withdraw("kucoin", symbol, netQuantity, address);
            if (string.IsNullOrWhiteSpace(responseContents))
            {
                throw new ApplicationException("Node ccxt indicated failure.");
            }

            var response = JsonConvert.DeserializeObject<KucoinWithdrawalResponse>(responseContents);
            if (!response?.Info?.Success ?? false)
            {
                var errorBuilder = new StringBuilder()
                    .AppendLine($"Failed to withdraw funds from {Name}");

                if (!string.IsNullOrWhiteSpace(response?.Info?.Code))
                {
                    errorBuilder.AppendLine($"Code: {response.Info.Code.Trim()}");
                }

                if (!string.IsNullOrWhiteSpace(response?.Info?.Msg))
                {
                    errorBuilder.AppendLine("Msg:");
                    errorBuilder.AppendLine(response.Info.Msg.Trim());
                }
            }

            // Kucoin has stoped requiring email confirmations for api withdrawals for now...
            // return ConfirmEmailWithdrawal(symbol, netQuantity);

            return true;
        }

        private bool ConfirmEmailWithdrawal(string symbol, decimal netQuantity)
        {
            string link = null;
            _waitForIt.Wait(() =>
            {
                link = _tradeEmailUtil.GetWithdrawalLink("kucoin", symbol, netQuantity);
                return !string.IsNullOrWhiteSpace(link);
            }, TimeSpan.FromMinutes(5), TimeSpan.FromSeconds(30));

            if (string.IsNullOrWhiteSpace(link)) { return false; }

            using (var connection = _rabbitConnectionFactory.Connect())
            {
                var contract = new ConfirmWithdrawalLinkRequestMessage { Url = link };
                connection.PublishContract(TradeRabbitConstants.Queues.KucoinAgentQueue, contract);
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

        private IMongoCollectionContext GetOrderBookContext()
        {
            return new MongoCollectionContext(DbContext, $"kucoin--order-book");
        }

        public bool BuyLimit(TradingPair tradingPair, QuantityAndPrice quantityAndPrice)
        {
            var nativeSymbol = _kucoinMap.ToNativeSymbol(tradingPair.Symbol);
            var nativeBaseSymbol = _kucoinMap.ToNativeSymbol(tradingPair.BaseSymbol);

            var tp = new TradingPair(nativeSymbol, nativeBaseSymbol);

            var apiKey = _configClient.GetKucoinApiKey();
            var result = _kucoinClient.CreateOrder(apiKey, nativeSymbol, nativeBaseSymbol, quantityAndPrice.Price, quantityAndPrice.Quantity, true);

            if (!(result?.Success ?? false))
            {
                if (!string.IsNullOrWhiteSpace(result.Msg)) { throw new ApplicationException(result.Msg); }
                throw new ApplicationException("Kucoin's response to buy limit did not indicate success");
            }

            return true;
        }

        public bool SellLimit(TradingPair tradingPair, QuantityAndPrice quantityAndPrice)
        {
            var nativeSymbol = _kucoinMap.ToNativeSymbol(tradingPair.Symbol);
            var nativeBaseSymbol = _kucoinMap.ToNativeSymbol(tradingPair.BaseSymbol);

            var apiKey = _configClient.GetKucoinApiKey();
            var result = _kucoinClient.CreateOrder(apiKey, nativeSymbol, nativeBaseSymbol, quantityAndPrice.Price, quantityAndPrice.Quantity, false);

            if (!(result?.Success ?? false))
            {
                if (!string.IsNullOrWhiteSpace(result.Msg)) { throw new ApplicationException(result.Msg); }
                throw new ApplicationException("Kucoin's response to sell limit did not indicate success");
            }

            return true;
        }

        public bool SellLimitOld(TradingPair tradingPair, QuantityAndPrice quantityAndPrice)
        {
            var nativeSymbol = string.Equals(tradingPair.Symbol, "CAN", StringComparison.InvariantCultureIgnoreCase)
                ? "CanYaCoin"
                : _kucoinMap.ToNativeSymbol(tradingPair.Symbol);

            var nativeBaseSymbol = _kucoinMap.ToNativeSymbol(tradingPair.BaseSymbol);

            var tp = new TradingPair(nativeSymbol, nativeBaseSymbol);

            var response = _nodeServiceUtil.SellLimit(CcxtExchangeName, tp, quantityAndPrice.Quantity, quantityAndPrice.Price);
            _log.Info($"Kucoin sell limit response: {response}");

            var parsedResponse = JsonConvert.DeserializeObject<KuCoinLimitResponse>(response);
            if (!(parsedResponse?.Info?.Success ?? false))
            {
                throw new ApplicationException($"KuCoin response when placing sell limit order for {quantityAndPrice.Quantity} {tradingPair.Symbol} at {quantityAndPrice.Price} {tradingPair.BaseSymbol} did not indicate success.{Environment.NewLine}{response}");
            }

            return true;
        }

        public List<HistoricalTrade> GetUserTradeHistory(CachePolicy cachePolicy)
        {
            var commodities = GetCommodities(CachePolicy.OnlyUseCacheUnlessEmpty);

            // var symbols = new List<string> { "FOTA", "CS", "LALA", "ETH", "BTC" };
            var nativeSymbols = commodities.Select(item => item.NativeSymbol).ToList();
            var tradeTypeDictionary = new Dictionary<string, TradeTypeEnum>
            {
                { "DEPOSIT", TradeTypeEnum.Deposit },
                { "WITHDRAW", TradeTypeEnum.Deposit },
            };

            var history = new List<HistoricalTrade>();
            foreach (var nativeSymbol in nativeSymbols)
            {
                KucoinListDepositAndWithdrawalRecordsResponse native = null;
                try
                {
                    native = GetNativeTransferHistory(nativeSymbol, cachePolicy);

                    foreach (var item in native?.Data?.Datas ?? new List<KucoinListDepositAndWithdrawalRecordsResponse.KucoinData.KucoinDataItem>())
                    {
                        var trade = new HistoricalTrade
                        {
                            Symbol = nativeSymbol,
                            Quantity = item.Amount,
                            TradeType = tradeTypeDictionary.ContainsKey(item.Type) ? tradeTypeDictionary[item.Type] : TradeTypeEnum.Unknown,
                            WalletAddress = item.OuterWalletTxid
                        };

                        history.Add(trade);
                    }
                }
                catch (Exception exception)
                {
                    _log.Error($"Failed to retrieve KuCoin transfer history for native symbol {nativeSymbol} with cache policy {cachePolicy}.");
                    _log.Error(exception);
                }
            }

            return history;
        }

        private KucoinListDepositAndWithdrawalRecordsResponse GetNativeTransferHistory(string nativeSymbol, CachePolicy cachePolicy)
        {
            var translator = new Func<string, KucoinListDepositAndWithdrawalRecordsResponse>(text =>
                !string.IsNullOrWhiteSpace(text)
                    ? JsonConvert.DeserializeObject<KucoinListDepositAndWithdrawalRecordsResponse>(text)
                    : null
            );

            var validator = new Func<string, bool>(text => translator(text) != null);

            var retriever = new Func<string>(() =>
            {
                try
                {
                    return _nodeServiceUtil.GetKucoinDepositAndWithdrawalHistoryForSymbol(nativeSymbol);
                }
                catch (Exception exception)
                {
                    _log.Error(exception);
                    throw;
                }
            });

            var collectionContext = new MongoCollectionContext(DbContext, $"kucoin--get-deposit-and-withdrawal-history--{nativeSymbol}");

            var cacheResult = _cacheUtil.GetCacheableEx(ThrottleContext, retriever, collectionContext, TimeSpan.FromMinutes(10), cachePolicy, validator);
            return translator(cacheResult?.Contents);
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

        public bool BuyLimit(TradingPair tradingPair, decimal quantity, decimal price)
        {
            return BuyLimit(tradingPair, new QuantityAndPrice { Quantity = quantity, Price = price });
        }

        public bool SellLimit(TradingPair tradingPair, decimal quantity, decimal price)
        {
            return SellLimit(tradingPair, new QuantityAndPrice { Quantity = quantity, Price = price });
        }

        public List<OpenOrderForTradingPair> GetOpenOrders(string symbol, string baseSymbol, CachePolicy cachePolicy)
        {
            var nativeSymbol = _kucoinMap.ToNativeSymbol(symbol);
            var nativeBaseSymbol = _kucoinMap.ToNativeSymbol(baseSymbol);

            var apiKey = _configClient.GetKucoinApiKey();
            // var response = _kucoinClient.GetOpenOrders(apiKey, nativeSymbol, nativeBaseSymbol);
            var response = _nodeServiceUtil.GetNativeOpenOrders(Name, new TradingPair(nativeSymbol, nativeBaseSymbol));
            var items = JsonConvert.DeserializeObject<List<KucoinGetOpenOrdersForTradingPairResponseItem>>(response);

            var sideDictionary = new Dictionary<string, OrderType>(StringComparer.InvariantCultureIgnoreCase)
            {
                { "buy", OrderType.Bid },
                { "sell", OrderType.Ask },
            };

            return items.Select(item =>
            {
                var orderType = sideDictionary.ContainsKey(item.Side)
                    ? sideDictionary[item.Side]
                    : OrderType.Unknown;

                return new OpenOrderForTradingPair
                {
                    OrderId = item.Info.Oid,
                    Symbol = _kucoinMap.ToCanonicalSymbol(item.Info.CoinType),
                    BaseSymbol = _kucoinMap.ToCanonicalSymbol(item.Info.CoinTypePair),
                    OrderType = orderType,
                    Price = item.Price ?? default(decimal),
                    Quantity = item.Remaining ?? default(decimal)
                };
            }).ToList();
        }

        public List<OpenOrdersForTradingPair> GetOpenOrdersV2()
        {
            var cachePolicy = CachePolicy.ForceRefresh;

            var native = GetNativeOpenOrders(cachePolicy);
            if (native == null) { return null; }

            var toCanonicalOpenOrder = new Func<KucoinOpenOrder, OrderType, OpenOrderForTradingPair>((kucoinOpenOrder, orderType) =>
            {
                var symbol = _kucoinMap.ToCanonicalSymbol(kucoinOpenOrder.CoinType);
                var baseSymbol = _kucoinMap.ToCanonicalSymbol(kucoinOpenOrder.CoinTypePair);

                return new OpenOrderForTradingPair
                {
                    OrderId = kucoinOpenOrder.Oid,
                    OrderType = orderType,
                    Price = kucoinOpenOrder.Price ?? 0,
                    Quantity = kucoinOpenOrder.PendingAmount ?? 0,
                    Symbol = symbol,
                    BaseSymbol = baseSymbol
                };
            });

            var openOrders = new List<OpenOrderForTradingPair>();
            if (native.Data?.Data?.Buy != null)
            {
                openOrders.AddRange(
                    native.Data.Data.Buy.Select(queryNativeOpenOrder => toCanonicalOpenOrder(queryNativeOpenOrder, OrderType.Bid)));
            }

            if (native.Data?.Data?.Sell != null)
            { 
                openOrders.AddRange(
                native.Data.Data.Sell.Select(queryNativeOpenOrder => toCanonicalOpenOrder(queryNativeOpenOrder, OrderType.Ask)));
            }

            var groupedOpenOrders = new Dictionary<string, List<OpenOrder>>(StringComparer.InvariantCultureIgnoreCase);
            foreach (var openOrder in openOrders)
            {
                var key = $"{openOrder.Symbol}_{openOrder.BaseSymbol}";
                if (!groupedOpenOrders.ContainsKey(key))
                {
                    groupedOpenOrders[key] = new List<OpenOrder>();
                }

                groupedOpenOrders[key].Add(openOrder);
            }

            return groupedOpenOrders.Keys.Select(key =>
            {
                var pieces = key.Split('_');
                var symbol = pieces[0];
                var baseSymbol = pieces[1];

                var group = new OpenOrdersForTradingPair
                {
                    AsOfUtc = native.AsOfUtc,
                    Symbol = symbol,
                    BaseSymbol = baseSymbol,
                    OpenOrders = groupedOpenOrders[key]
                };

                return group;
            }).ToList();
        }

        private AsOfWrapper<KucoinGetOpenOrdersResponse> GetNativeOpenOrders(CachePolicy cachePolicy)
        {
            var translator = new Func<string, KucoinGetOpenOrdersResponse>(text =>
                !string.IsNullOrWhiteSpace(text)
                ? JsonConvert.DeserializeObject<KucoinGetOpenOrdersResponse>(text)
                : null);

            var validator = new Func<string, bool>(text =>
            {
                if (string.IsNullOrWhiteSpace(text)) { throw new ApplicationException($"Received an empty response when requesting {Name} open orders."); }
                var translated = translator(text);
                if (translated == null) { throw new ApplicationException($"Failed to parse response from request for {Name} open orders."); }

                return true;
            });

            var retriever = new Func<string>(() =>
            {
                try
                {
                    var apiKey = _configClient.GetKucoinApiKey();
                    var responstText = _kucoinClient.GetOpenOrders(apiKey);
                    if (!validator(responstText))
                    {
                        throw new ApplicationException($"Response from request for {Name} open orders for failed validation.");
                    }

                    return responstText;
                }
                catch (Exception exception)
                {
                    _log.Error(exception);
                    throw;
                }
            });

            var collectionContext = new MongoCollectionContext(DbContext, "kucoin--get-open-orders");
            var cacheResult = _cacheUtil.GetCacheableEx(ThrottleContext, retriever, collectionContext, OpenOrdersThreshold, cachePolicy, validator);

            var response = translator(cacheResult?.Contents);
            return new AsOfWrapper<KucoinGetOpenOrdersResponse>
            {
                AsOfUtc = cacheResult?.AsOf,
                Data = response
            };
        }

        public OpenOrdersWithAsOf GetOpenOrdersForTradingPairV2(string symbol, string baseSymbol, CachePolicy cachePolicy)
        {
            var native = GetNativeOpenOrders(cachePolicy);
            if (native == null) { return null; }

            var nativeSymbol = _kucoinMap.ToNativeSymbol(symbol);
            var nativeBaseSymbol = _kucoinMap.ToNativeSymbol(baseSymbol);

            var filteredBids = (native?.Data?.Data?.Buy ?? new List<KucoinOpenOrder>()).Where(item => string.Equals(item.CoinType, nativeSymbol, StringComparison.InvariantCultureIgnoreCase)
                && string.Equals(item.CoinTypePair, nativeBaseSymbol, StringComparison.InvariantCultureIgnoreCase))
                .ToList();

            var filteredAsks = (native?.Data?.Data?.Sell ?? new List<KucoinOpenOrder>()).Where(item => string.Equals(item.CoinType, nativeSymbol, StringComparison.InvariantCultureIgnoreCase)
                && string.Equals(item.CoinTypePair, nativeBaseSymbol, StringComparison.InvariantCultureIgnoreCase))
                .ToList();

            var toCanonicalOpenOrder = new Func<KucoinOpenOrder, OrderType, OpenOrder>((kucoinOpenOrder, orderType) =>
            {
                var combinedOrderId = new KucoinCombinedOrderId
                {
                    OrderId = kucoinOpenOrder.Oid,
                    IsBid = orderType == OrderType.Bid,
                    NativeSymbol = nativeSymbol,
                    NativeBaseSymbol = nativeBaseSymbol                    
                };
                var orderIdText = JsonConvert.SerializeObject(combinedOrderId);

                return new OpenOrder
                {
                    OrderId = orderIdText,
                    OrderType = orderType,
                    Price = kucoinOpenOrder.Price ?? 0,
                    Quantity = kucoinOpenOrder.PendingAmount ?? 0
                };
            });

            var openOrders = new List<OpenOrder>();
            openOrders.AddRange(
                filteredBids.Select(queryNativeOpenOrder => toCanonicalOpenOrder(queryNativeOpenOrder, OrderType.Bid)));
            openOrders.AddRange(
                filteredAsks.Select(queryNativeOpenOrder => toCanonicalOpenOrder(queryNativeOpenOrder, OrderType.Ask)));

            return new OpenOrdersWithAsOf
            {
                AsOfUtc = native.AsOfUtc,
                OpenOrders = openOrders
            };
        }

        private IMongoDatabaseContext DbContext
        {
            get { return new MongoDatabaseContext(_configClient.GetConnectionString(), DatabaseName); }
        }

        public string KucoinGetDepositAddress()
        {
            var apiKey = _configClient.GetKucoinApiKey();
            return _kucoinClient.GetDepositAddress(apiKey, "ETH");
        }

        public void CancelOrder(string orderId)
        {
            if (string.IsNullOrWhiteSpace(orderId)) { throw new ArgumentNullException(nameof(orderId)); }

            var combinedOrderId = JsonConvert.DeserializeObject<KucoinCombinedOrderId>(orderId);
            
            var apiKey = _configClient.GetKucoinApiKey();
            var response = _kucoinClient.CancelOrder(apiKey, combinedOrderId.OrderId, combinedOrderId.IsBid, combinedOrderId.NativeSymbol, combinedOrderId.NativeBaseSymbol);
            _log.Info($"Kucoin response from attempting to cancel order \"{orderId}\":{Environment.NewLine}{response}");

            var parsedResponse = JsonConvert.DeserializeObject<KucoinResponse>(response);

            if (!parsedResponse.Success)
            {
                _log.Error($"Failed to cancel Kucoin order {orderId}.");
            }
        }

        public BalanceWithAsOf GetBalanceForSymbol(string symbol, CachePolicy cachePolicy)
        {
            var nativeSymbol = _kucoinMap.ToNativeSymbol(symbol);

            var nativeAccountsWithAsOf = GetNativeAccounts(cachePolicy);
            if (nativeAccountsWithAsOf == null) { throw new ApplicationException($"{nameof(nativeAccountsWithAsOf)} should not be null."); }

            var nativeAccount = (nativeAccountsWithAsOf?.Data?.Data ?? new List<KucoinClientAccount>())
                .Where(queryNativeAccount => string.Equals(queryNativeAccount.Currency, nativeSymbol, StringComparison.InvariantCultureIgnoreCase))
                .SingleOrDefault();

            return new BalanceWithAsOf
            {
                Symbol = symbol,
                AsOfUtc = nativeAccountsWithAsOf.AsOfUtc,
                Total = nativeAccount?.Balance ?? 0,
                InOrders = nativeAccount?.Holds ?? 0,
                Available = nativeAccount?.Available ?? 0
            };
        }

        // TODO: Get the config client to send the whole key.
        private KucoinApiKey GetApiKey()
        {
            var standardKey = _configClient.GetKucoinApiKey();
            var passPhrase = _configClient.GetKucoinApiPassphrase();

            return new KucoinApiKey
            {
                PublicKey = standardKey.Key,
                PrivateKey = standardKey.Secret,
                Passphrase = passPhrase
            };
        }
    }
}
