using binance_lib;
using bit_z_lib;
using coss_lib;
using cryptopia_lib;
using hitbtc_lib;
using idex_integration_lib;
using kraken_integration_lib;
using kucoin_lib;
using livecoin_lib;
using mew_integration_lib;
using qryptos_lib;
using yobit_lib;
using service_lib.Handlers;
using trade_contracts.Messages.Exchange;
using System;
using tidex_integration_library;
using System.Collections.Generic;
using trade_lib;
using System.Linq;
using trade_model;
using Newtonsoft.Json;
using trade_contracts;
using exchange_service_lib.Workflows;
using trade_res;
using exchange_service_lib.Extensions;
using cache_lib.Models;
using System.Diagnostics;
using log_lib;
using System.Threading.Tasks;
using parse_lib;
using cache_lib;
using mongo_lib;
using config_connection_string_lib;
using trade_contracts.Messages;
using System.Net;
using trade_contracts.Messages.Exchange.OpenOrders;
using trade_contracts.Models.OpenOrders;
using coinbase_lib;
using trade_contracts.Models;
using reflection_lib;
using task_lib;
using System.IO;
using trade_contracts.Messages.Exchange.Withdraw;
using trade_contracts.Messages.Exchange.Balance;
using System.Threading;
using oex_lib;
using trade_contracts.Messages.Exchange.PlaceOrder;
using service_lib.Exceptions;
using trade_contracts.Messages.Exchange.HitBtc;
using gemini_lib;
using blocktrade_lib;
using trade_contracts.Messages.Exchange.History;

namespace exchange_service_lib.Handlers
{
    public interface IExchangeHandler
        : IRequestResponseHandler<GetTradingPairsForExchangeRequestMessage, GetTradingPairsForExchangeResponseMessage>,
        IRequestResponseHandler<GetWithdrawalFeesRequestMessage, GetWithdrawalFeesResponseMessage>,
        IRequestResponseHandler<GetWithdrawalFeeRequestMessage, GetWithdrawalFeeResponseMessage>,
        IRequestResponseHandler<GetExchangesRequestMessage, GetExchangesResponseMessage>,
        IRequestResponseHandler<GetCryptoCompareSymbolsRequestMessage, GetCryptoCompareSymbolsResponseMessage>,
        IRequestResponseHandler<GetOrderBookRequestMessage, GetOrderBookResponseMessage>,
        IRequestResponseHandler<RefreshOrderBookRequestMessage, RefreshOrderBookResponseMessage>,
        IRequestResponseHandler<GetCommoditiesForExchangeRequestMessage, GetCommoditiesForExchangeResponseMessage>,
        IRequestResponseHandler<GetDetailedCommodityForExchangeRequestMessage, GetDetailedCommodityForExchangeResponseMessage>,
        IRequestResponseHandler<GetDepositAddressRequestMessage, GetDepositAddressResponseMessage>,
        IRequestResponseHandler<GetExchangeHistoryRequestMessage, GetExchangeHistoryResponseMessage>,
        IRequestResponseHandler<GetBalanceRequestMessage, GetBalanceResponseMessage>,
        IRequestResponseHandler<GetBalanceForCommodityAndExchangeRequestMessage, GetBalanceForCommodityAndExchangeResponseMessage>,
        IRequestResponseHandler<GetCommodityDetailsRequestMessage, GetCommodityDetailsResponseMessage>,
        IRequestResponseHandler<GetCommoditiesRequestMessage, GetCommoditiesResponseMessage>,
        IRequestResponseHandler<GetExchangesForCommodityRequestMessage, GetExchangesForCommodityResponseMessage>,
        IRequestResponseHandler<GetCachedOrderBooksRequestMessage, GetCachedOrderBooksResponseMessage>,
        IRequestResponseHandler<GetOpenOrdersRequestMessage, GetOpenOrdersResponseMessage>,
        IRequestResponseHandler<SellLimitRequestMessage, SellLimitResponseMessage>,
        IRequestResponseHandler<BuyLimitRequestMessage, BuyLimitResponseMessage>,
        IRequestResponseHandler<GetOpenOrdersForTradingPairRequestMessage, GetOpenOrdersForTradingPairResponseMessage>,
        IRequestResponseHandler<CancelOrderRequestMessage, CancelOrderResponseMessage>,
        IRequestResponseHandler<GetOpenOrdersRequestMessageV2, GetOpenOrdersResponseMessageV2>,
        IRequestResponseHandler<GetOpenOrdersForTradingPairRequestMessageV2, GetOpenOrdersForTradingPairResponseMessageV2>,
        IRequestResponseHandler<GetAggregateExchangeHistoryRequestMessage, GetAggregateExchangeHistoryResponseMessage>,
        IRequestResponseHandler<WithdrawCommodityRequestMessage, WithdrawCommodityResponseMessage>,
        IRequestResponseHandler<GetBalanceForCommoditiesAndExchangeRequestMessage, GetBalanceForCommoditiesAndExchangeResponseMessage>,
        IRequestResponseHandler<KeepHitbtcHealthFreshRequestMessage, KeepHitbtcHealthFreshResponseMessage>,
        IRequestResponseHandler<GetHistoryForTradingPairRequestMessage, GetHistoryForTradingPairResponseMessage>
    { }

    public class ExchangeHandler : IExchangeHandler
    {
        private const string DatabaseName = "trade-aggregate";
        private const string AddressLookupFileName = @"c:\trade\data\aggregate-addresses.json";

        private class AddressItem
        {
            public string Exchange { get; set; }
            public string Symbol { get; set; }
            public string Address { get; set; }
            public string Memo { get; set; }
        }

        private readonly ICossIntegration _cossIntegration;
        private readonly IBinanceIntegration _binanceIntegration;
        private readonly IBitzIntegration _bitzIntegration;
        private readonly IKucoinIntegration _kucoinIntegration;
        private readonly IHitBtcIntegration _hitBtcIntegration;
        private readonly ILivecoinIntegration _livecoinIntegration;
        private readonly IKrakenIntegration _krakenIntegration;
        private readonly ICoinbaseIntegration _coinbaseIntegration;
        private readonly IMewIntegration _mewIntegration;
        private readonly IIdexIntegration _idexIntegration;
        private readonly ICryptopiaIntegration _cryptopiaIntegration;
        private readonly IYobitIntegration _yobitIntegration;
        private readonly IQryptosIntegration _qryptosIntegration;
        private readonly ITidexIntegration _tidexIntegration;
        private readonly IOexExchange _oexExchange;
        private readonly IGeminiExchange _geminiIntegration;
        private readonly IBlockTradeExchange _blockTradeExchange;

        private readonly IExchangeWorkflow _exchangeWorkflow;

        private readonly IGetConnectionString _getConnectionString;

        private readonly List<ITradeIntegration> _exchanges;
        private readonly ICacheUtil _cacheUtil;

        private readonly ILogRepo _log;

        public ExchangeHandler(
            ICossIntegration cossIntegration,
            IBinanceIntegration binanceIntegration,
            IBitzIntegration bitzIntegration,
            IKucoinIntegration kucoinIntegration,
            IHitBtcIntegration hitBtcIntegration,
            ILivecoinIntegration livecoinIntegration,
            IKrakenIntegration krakenIntegration,
            IMewIntegration mewIntegration,
            IIdexIntegration idexIntegration,
            ICryptopiaIntegration cryptopiaIntegration,
            IYobitIntegration yobitIntegration,
            IQryptosIntegration qryptosIntegration,
            ITidexIntegration tidexIntegration,
            ICoinbaseIntegration coinbaseIntegration,
            IOexExchange oexExchange,
            IGeminiExchange geminiExchange,
            IBlockTradeExchange blockTradeExchange,

            IExchangeWorkflow exchangeWorkflow,

            IGetConnectionString getConnectionString,

            ICacheUtil cacheUtil,

            ILogRepo log)
        {
            _cossIntegration = cossIntegration;
            _binanceIntegration = binanceIntegration;
            _bitzIntegration = bitzIntegration;
            _kucoinIntegration = kucoinIntegration;
            _hitBtcIntegration = hitBtcIntegration;
            _livecoinIntegration = livecoinIntegration;
            _krakenIntegration = krakenIntegration;
            _mewIntegration = mewIntegration;
            _idexIntegration = idexIntegration;
            _cryptopiaIntegration = cryptopiaIntegration;
            _yobitIntegration = yobitIntegration;
            _qryptosIntegration = qryptosIntegration;
            _tidexIntegration = tidexIntegration;
            _coinbaseIntegration = coinbaseIntegration;
            _oexExchange = oexExchange;
            _geminiIntegration = geminiExchange;
            _blockTradeExchange = blockTradeExchange;

            _exchangeWorkflow = exchangeWorkflow;

            _getConnectionString = getConnectionString;

            _cacheUtil = cacheUtil;

            _log = log;

            _exchanges = new List<ITradeIntegration>
            {
                cossIntegration,
                binanceIntegration,
                bitzIntegration,
                kucoinIntegration,
                hitBtcIntegration,
                livecoinIntegration,
                krakenIntegration,
                mewIntegration,
                idexIntegration,
                cryptopiaIntegration,
                yobitIntegration,
                qryptosIntegration,
                // tidexIntegration,
                coinbaseIntegration,
                // oexExchange,
                _geminiIntegration,
                _blockTradeExchange
            };
        }

        public GetTradingPairsForExchangeResponseMessage Handle(GetTradingPairsForExchangeRequestMessage message)
        {
            if (message == null) { throw new ArgumentNullException(nameof(message)); }
            if (string.IsNullOrWhiteSpace(message.Exchange)) { throw new ArgumentNullException(nameof(message.Exchange)); }

            var exchange = GetExchangeFromName(message.Exchange);
            if (exchange == null) { throw new ApplicationException($"Failed to resolve exchange from name \"{message.Exchange}\"."); }

            var tradingPairs = Time(
                () => exchange.GetTradingPairs((CachePolicy)message.CachePolicy),
                $"Get {message.Exchange} trading pairs with cache policy \"{message.CachePolicy}\".");

            var tradingPairContracts = ToContract(tradingPairs);

            return new GetTradingPairsForExchangeResponseMessage
            {
                TradingPairs = tradingPairContracts
            };
        }

        private void Time(Action method, string desc)
        {
            Time(new Func<int>(() => { method(); return 1; }), desc);
        }

        private T Time<T>(Func<T> method, string desc)
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            var result = method();
            stopWatch.Stop();

            _log.Timing($"Action: \"{desc}\"; Elapsed Time: {stopWatch.ElapsedMilliseconds} ms");

            return result;
        }

        public GetWithdrawalFeesResponseMessage Handle(GetWithdrawalFeesRequestMessage message)
        {
            if (message == null) { throw new ArgumentNullException(nameof(message)); }
            if (string.IsNullOrWhiteSpace(message.Exchange)) { throw new ArgumentNullException(nameof(message.Exchange)); }

            //if (message.CachePolicy == CachePolicyContract.OnlyUseCache || message.CachePolicy == CachePolicyContract.OnlyUseCacheUnlessEmpty)
            //{
            //    var cacheResults = _withdrawalFeesCache.Get(message.Exchange, (CachePolicy)message.CachePolicy);
            //    if (_withdrawalFeesValidator(cacheResults))
            //    {
            //        return new GetWithdrawalFeesResponseMessage
            //        {
            //            WithdrawalFees = cacheResults
            //        };
            //    }
            //}

            var exchange = GetExchangeFromName(message.Exchange);
            if (exchange == null) { throw new ApplicationException($"Failed to resolve exchange from name \"{message.Exchange}\"."); }

            var fees = exchange.GetWithdrawalFees((CachePolicy)message.CachePolicy);
            //if (_withdrawalFeesValidator(fees))
            //{
            //    _withdrawalFeesCache.Set(message.Exchange, fees);
            //}

            return new GetWithdrawalFeesResponseMessage
            {
                WithdrawalFees = fees
            };
        }

        public GetExchangesResponseMessage Handle(GetExchangesRequestMessage message)
        {
            return new GetExchangesResponseMessage
            {
                Exchanges = _exchangeWorkflow.GetExchanges()
            };
        }

        public GetCryptoCompareSymbolsResponseMessage Handle(GetCryptoCompareSymbolsRequestMessage message)
        {
            return new GetCryptoCompareSymbolsResponseMessage
            {
                Symbols = CryptoCompareRes.CryptoCompareSymbols
            };
        }

        // private Dictionary<Guid, object> _recentMessages = new Dictionary<Guid, object>();

        public GetOrderBookResponseMessage Handle(GetOrderBookRequestMessage message)
        {
            try
            {
                if (message == null) { throw new ArgumentNullException(nameof(message)); }
                if (string.IsNullOrWhiteSpace(message.Exchange)) { throw new ArgumentNullException(nameof(message.Exchange)); }

                //if (_recentMessages.ContainsKey(message.MessageId))
                //{
                //    asdfasd
                //}

                var exchange = GetExchangeFromName(message.Exchange);
                if (exchange == null) { throw new ApplicationException($"Failed to resolve exchange from name \"{message?.Exchange}\"."); }

                var tradingPair = new TradingPair(message.TradingPair.Symbol, message.TradingPair.BaseSymbol);
                var orderBook = exchange.GetOrderBook(tradingPair, (CachePolicy)message.CachePolicy);
                var orderBookContract = ToContract(orderBook);

                return new GetOrderBookResponseMessage
                {
                    OrderBook = orderBookContract
                };
            }
            catch (Exception exception)
            {
                _log.Error($"GetOrderBook failed for Symbol: {message?.TradingPair?.Symbol}, Base Symbol: {message?.TradingPair?.BaseSymbol}, Exchange: {message?.Exchange}, CachePolicy: {message?.CachePolicy}.");
                _log.Error(exception);
                throw;
            }
        }

        public RefreshOrderBookResponseMessage Handle(RefreshOrderBookRequestMessage message)
        {
            if (message == null) { throw new ArgumentNullException(nameof(message)); }
            if (string.IsNullOrWhiteSpace(message.Exchange)) { throw new ArgumentNullException(nameof(message.Exchange)); }

            var exchange = GetExchangeFromName(message.Exchange);
            if (exchange == null) { throw new ApplicationException($"Failed to resolve exchange from name \"{message.Exchange}\"."); }

            var refreshable = exchange as IRefreshable;
            if (refreshable == null) { throw new ApplicationException($"Exchange {exchange.Name} is not refreshable."); }

            var tradingPair = new TradingPair { Symbol = message.Symbol, BaseSymbol = message.BaseSymbol };
            var result = refreshable.RefreshOrderBook(tradingPair);

            return new RefreshOrderBookResponseMessage
            {
                Result = new RefreshOrderBookResultContract
                {
                    WasRefreshed = result.WasRefreshed,
                    AsOf = result.AsOf,
                    CacheAge = result.CacheAge
                }
            };
        }

        //private static Func<List<ExchangeCommodityContract>, bool> _commoditiesValidator = new Func<List<ExchangeCommodityContract>, bool>(item =>
        //{
        //    return item?.Any() ?? false;
        //});

        //private static MemCacheDictionary<List<ExchangeCommodityContract>> _commoditiesCache = new MemCacheDictionary<List<ExchangeCommodityContract>>(_commoditiesValidator);

        public GetCommoditiesForExchangeResponseMessage Handle(GetCommoditiesForExchangeRequestMessage message)
        {
            if (message == null) { throw new ArgumentNullException(nameof(message)); }
            if (string.IsNullOrWhiteSpace(message.Exchange)) { throw new ArgumentNullException(nameof(message.Exchange)); }


            var commodities = GetExchangeCommodities(message.Exchange, message.CachePolicy);

            return new GetCommoditiesForExchangeResponseMessage
            {
                Commodities = commodities
            };
        }

        public GetDetailedCommodityForExchangeResponseMessage Handle(GetDetailedCommodityForExchangeRequestMessage message)
        {
            try
            {
                if (message == null) { throw new ArgumentNullException(nameof(message)); }
                if (string.IsNullOrWhiteSpace(message.Exchange)) { throw new ArgumentNullException(nameof(message.Exchange)); }
                if (string.IsNullOrWhiteSpace(message.Symbol) && string.IsNullOrWhiteSpace(message.NativeSymbol))
                {
                    throw new ArgumentException($"Both {nameof(message.Symbol)} and {nameof(message.NativeSymbol)} must not be null/empty.");
                }

                var exchange = GetExchangeFromName(message.Exchange);
                if (exchange == null) { throw new ApplicationException($"Failed to resolve exchange from name \"{message.Exchange}\"."); }

                var commodities = exchange.GetCommodities((CachePolicy)message.CachePolicy);
                if (commodities == null || !commodities.Any()) { throw new ApplicationException($"Exchange \"{message.Exchange}\" did not return any commodities. Cache Policy: \"{message.CachePolicy}\""); }

                CommodityForExchange matchingCommodity = null;
                if (!string.IsNullOrWhiteSpace(message.NativeSymbol))
                {
                    matchingCommodity = commodities.SingleOrDefault(queryCommodity => string.Equals(message.NativeSymbol, queryCommodity.NativeSymbol, StringComparison.InvariantCultureIgnoreCase));
                }

                if (matchingCommodity == null)
                {
                    matchingCommodity = commodities.SingleOrDefault(queryCommodity => string.Equals(message.Symbol, queryCommodity.Symbol, StringComparison.InvariantCultureIgnoreCase));
                }

                if (matchingCommodity == null)
                {
                    throw new ApplicationException($"Failed to retreive a matching commodity on exchange {message.Exchange} for symbol \"{message.Symbol ?? "(null)"}\", native symbol \"{message.NativeSymbol ?? "(null)"}\".");
                }

                var tradingPairs = exchange.GetTradingPairs((CachePolicy)message.CachePolicy);
                var matchingTradingPairs =
                    tradingPairs.Where(queryTradingPair => string.Equals(matchingCommodity.Symbol, queryTradingPair.Symbol, StringComparison.InvariantCultureIgnoreCase)).ToList();

                var baseSymbols = matchingTradingPairs
                    .Where(item => !string.IsNullOrWhiteSpace(item.BaseSymbol))
                    .Select(item => item.BaseSymbol.Trim()).Distinct().ToList();

                var depositAddress = (matchingCommodity.CanDeposit ?? false) && !string.Equals(exchange.Name, "binance", StringComparison.InvariantCultureIgnoreCase)
                    ? exchange.GetDepositAddress(matchingCommodity.Symbol, (CachePolicy)message.CachePolicy)
                    : null;

                var commodity =
                    ModelConverter.ToDetailedExchangeCommodityContract(matchingCommodity, exchange.Name, depositAddress, baseSymbols);

                return new GetDetailedCommodityForExchangeResponseMessage
                {
                    Commodity = commodity
                };
            }
            catch (Exception exception)
            {
                _log.Error(exception);
                throw;
            }
        }

        public GetDepositAddressResponseMessage Handle(GetDepositAddressRequestMessage message)
        {
            if (message == null) { throw new ArgumentNullException(nameof(message)); }
            if (string.IsNullOrWhiteSpace(message.Exchange)) { throw new ArgumentNullException(nameof(message.Exchange)); }
            if (string.IsNullOrWhiteSpace(message.Symbol)) { throw new ArgumentNullException(nameof(message.Symbol)); }

            var exchange = GetExchangeFromName(message.Exchange);
            if (exchange == null) { throw new ApplicationException($"Failed to resolve exchange from name \"{message.Exchange}\"."); }

            var depositAddress = exchange.GetDepositAddress(message.Symbol, (CachePolicy)message.CachePolicy);

            return new GetDepositAddressResponseMessage
            {
                DepositAddress = ToContract(depositAddress)
            };
        }

        public GetExchangeHistoryResponseMessage Handle(GetExchangeHistoryRequestMessage message)
        {
            if (message == null) { throw new ArgumentNullException(nameof(message)); }
            if (string.IsNullOrWhiteSpace(message.Exchange)) { throw new ArgumentNullException(nameof(message.Exchange)); }

            var exchange = GetExchangeFromName(message.Exchange);
            if (exchange == null) { throw new ApplicationException($"Failed to resolve exchange from name \"{message.Exchange}\"."); }

            var historyWithAsOf = _exchangeWorkflow.GetExchangeHistory(exchange, ToModel(message.CachePolicy), message.Limit);

            var aggregateAddresses = ReadAggregateAddressItems();
            foreach (var historyItem in historyWithAsOf.History ?? new List<HistoricalTrade>())
            {
                if (string.IsNullOrWhiteSpace(historyItem.WalletAddress)) { continue; }
                var matchingAddress = aggregateAddresses.FirstOrDefault(item => string.Equals(item.Address, historyItem.WalletAddress, StringComparison.InvariantCultureIgnoreCase));
                if (matchingAddress == null) { continue; }

                historyItem.WalletAddress = $"{historyItem.WalletAddress} ({matchingAddress.Exchange})";
            }

            return new GetExchangeHistoryResponseMessage
            {
                Payload = new GetExchangeHistoryResponseMessage.ResponsePayload
                {
                    History = ToContract(historyWithAsOf.History),
                    AsOfUtc = historyWithAsOf.AsOfUtc
                }
            };
        }        

        private List<AddressItem> ReadAggregateAddressItems()
        {
            var contents = File.Exists(AddressLookupFileName)
                ? File.ReadAllText(AddressLookupFileName)
                : null;

            return !string.IsNullOrWhiteSpace(contents)
                ? JsonConvert.DeserializeObject<List<AddressItem>>(contents)
                : new List<AddressItem>();
        }

        public GetAggregateExchangeHistoryResponseMessage Handle(GetAggregateExchangeHistoryRequestMessage message)
        {
            if (message == null) { throw new ArgumentNullException(nameof(message)); }
            if (message.Payload == null) { throw new ArgumentNullException(nameof(message.Payload)); }

            var exchanges = new List<ITradeIntegration> { _coinbaseIntegration, _krakenIntegration };

            var asOfUtcByExchange = new Dictionary<string, DateTime?>();
            var allHistoryExContracts = new List<HistoryItemWithExchangeContract>();
            var exchangesAndHistories =
                exchanges.Select(queryExchange =>                
                    new
                    {
                        Exchange = queryExchange,
                        HistoryTask = LongRunningTask.Run(() => _exchangeWorkflow.GetExchangeHistory(queryExchange, ToModel(message.Payload.CachePolicy), message.Payload.Limit))
                    }
                ).ToList();

            foreach (var exchangeAndHistory in exchangesAndHistories)
            {
                var exchange = exchangeAndHistory.Exchange;

                var historyWithAsOfUtc = exchangeAndHistory.HistoryTask.Result;
                asOfUtcByExchange[exchange.Name] = historyWithAsOfUtc.AsOfUtc;
                var historyContracts = ToContract(historyWithAsOfUtc.History);
                if (historyContracts != null && historyContracts.Any())
                {
                    var historyExContracts = historyContracts != null
                        ? historyContracts.Select(queryContract =>
                        {
                            var historyExContract = ReflectionUtil.CloneToType<HistoryItemContract, HistoryItemWithExchangeContract>(queryContract);
                            historyExContract.Exchange = exchange.Name;

                            return historyExContract;
                        })
                        .ToList()
                        : null;

                    allHistoryExContracts.AddRange(historyExContracts);
                }
            }

            var additionalHistories = ReadAdditionalHistory();
            if (additionalHistories != null)
            {
                allHistoryExContracts.AddRange(additionalHistories);
            }

            var aggregateAddresses = ReadAggregateAddressItems();
            foreach (var historyItem in allHistoryExContracts)
            {
                if (string.IsNullOrWhiteSpace(historyItem.WalletAddress)) { continue; }
                var matchingAddress = aggregateAddresses.FirstOrDefault(item => string.Equals(item.Address, historyItem.WalletAddress, StringComparison.InvariantCultureIgnoreCase));
                if (matchingAddress == null) { continue; }

                historyItem.DestinationExchange = matchingAddress.Exchange;
                historyItem.WalletAddress = $"{historyItem.WalletAddress} ({matchingAddress.Exchange})";
            }
                
            return new GetAggregateExchangeHistoryResponseMessage
            {
                Payload = new GetAggregateExchangeHistoryResponseMessage.ResponsePayload
                {
                    AsOfUtcByExchange = asOfUtcByExchange,
                    History = allHistoryExContracts
                }
            };
        }

        private List<HistoryItemWithExchangeContract> ReadAdditionalHistory()
        {
            const string FileName = @"C:\trade\data\additional-history.json";
            var contents = File.Exists(FileName)
                ? File.ReadAllText(FileName)
                : null;

            return !string.IsNullOrWhiteSpace(contents)
                ? JsonConvert.DeserializeObject<List<HistoryItemWithExchangeContract>>(contents)
                : new List<HistoryItemWithExchangeContract>();
        }

        public GetBalanceResponseMessage Handle(GetBalanceRequestMessage message)
        {
            if (message == null) { throw new ArgumentNullException(nameof(message)); }
            if (string.IsNullOrWhiteSpace(message.Exchange)) { throw new ArgumentNullException(nameof(message.Exchange)); }

            var exchange = GetExchangeFromName(message.Exchange);
            if (exchange == null) { throw new ApplicationException($"Failed to resolve exchange from name \"{message.Exchange}\"."); }

            var holdingInfo = exchange.GetHoldings((CachePolicy)message.CachePolicy);

            return new GetBalanceResponseMessage
            {
                BalanceInfo = new BalanceInfoContract
                {
                    AsOfUtc = holdingInfo?.TimeStampUtc,
                    Balances = holdingInfo?.Holdings != null
                    ? holdingInfo.Holdings.Select(queryHolding => ToContract(queryHolding)).ToList()
                    : null
                }
            };
        }

        public GetBalanceForCommodityAndExchangeResponseMessage Handle(GetBalanceForCommodityAndExchangeRequestMessage message)
        {
            if (message == null) { throw new ArgumentNullException(nameof(message)); }
            if (string.IsNullOrWhiteSpace(message.Symbol)) { throw new ArgumentNullException(nameof(message.Symbol)); }
            if (string.IsNullOrWhiteSpace(message.Exchange)) { throw new ArgumentNullException(nameof(message.Exchange)); }

            var exchange = GetExchangeFromName(message.Exchange);
            if (exchange == null) { throw new ApplicationException($"Failed to resolve exchange from name \"{message.Exchange}\"."); }

            if (exchange is IBalanceIntegration balanceIntegration)
            {
                var balanceForSymbol = balanceIntegration.GetBalanceForSymbol(message.Symbol, ToModel(message.CachePolicy));
                return new GetBalanceForCommodityAndExchangeResponseMessage
                {
                    Balance = new BalanceContract
                    {
                        Symbol = message.Symbol,
                        Available = balanceForSymbol.Available ?? 0,
                        InOrders = balanceForSymbol.InOrders ?? 0,
                        Total = balanceForSymbol.Total ?? 0
                    }
                };
            }

            var balance = exchange.GetHolding(message.Symbol, (CachePolicy)message.CachePolicy);

            return new GetBalanceForCommodityAndExchangeResponseMessage
            {
                Balance = ToContract(balance)
            };
        }

        public GetBalanceForCommoditiesAndExchangeResponseMessage Handle(GetBalanceForCommoditiesAndExchangeRequestMessage message)
        {
            if (message == null) { throw new ArgumentNullException(nameof(message)); }
            if (message.Payload == null) { throw new ArgumentNullException(nameof(message.Payload)); }
            if (string.IsNullOrWhiteSpace(message.Payload.Exchange)) { throw new ArgumentNullException(nameof(message.Payload.Exchange)); }
            if (message.Payload.Symbols == null) { throw new ArgumentNullException(nameof(message.Payload.Symbols)); }

            var exchange = GetExchangeFromName(message.Payload.Exchange);
            if (exchange == null) { throw new ApplicationException($"Failed to resolve exchange from name \"{message.Payload.Exchange}\"."); }

            var symbols = message.Payload.Symbols.Select(querySymbol => querySymbol.ToUpper()).Distinct();

            if (exchange is IBalanceIntegration balanceIntegration)
            {
                var balancesWithAsOf = new List<BalanceWithAsOf>();
                foreach (var symbol in symbols)
                {
                    var balance = balanceIntegration.GetBalanceForSymbol(symbol, ToModel(message.Payload.CachePolicy));
                    balancesWithAsOf.Add(balance);
                }

                return new GetBalanceForCommoditiesAndExchangeResponseMessage
                {
                    Payload = new GetBalanceForCommoditiesAndExchangeResponseMessage.ResponsePayload
                    {
                        Balances = balancesWithAsOf.Select(queryBalance =>
                        {
                            return new BalanceContractWithAsOf
                            {
                                AsOfUtc = queryBalance.AsOfUtc,
                                Available = queryBalance.Available ?? 0,
                                InOrders = queryBalance.InOrders ?? 0,
                                AdditionalBalances = queryBalance.AdditionalBalanceItems,
                                Symbol = queryBalance.Symbol,
                                Total = queryBalance.Total ?? 0
                            };
                        }).ToList()
                    }
                };
            }

            var holdingInfo = exchange.GetHoldings(ToModel(message.Payload.CachePolicy));

            var balances = holdingInfo.Holdings
                .Where(queryHolding => symbols.Any(querySymbol => string.Equals(querySymbol, queryHolding.Symbol, StringComparison.InvariantCultureIgnoreCase)))
                .Select(queryHolding =>
            {
                return new BalanceContractWithAsOf
                {
                    Symbol = queryHolding.Symbol,
                    Available = queryHolding.Available,
                    InOrders = queryHolding.InOrders,
                    Total = queryHolding.Total,
                    AdditionalBalances = queryHolding.AdditionalHoldings,
                    AsOfUtc = holdingInfo.TimeStampUtc
                };
            }).ToList();

            return new GetBalanceForCommoditiesAndExchangeResponseMessage
            {
                Payload = new GetBalanceForCommoditiesAndExchangeResponseMessage.ResponsePayload
                {
                    Balances = balances
                }
            };
        }

        public GetCachedOrderBooksResponseMessage Handle(GetCachedOrderBooksRequestMessage message)
        {
            if (message == null) { throw new ArgumentNullException(nameof(message)); }
            if (string.IsNullOrWhiteSpace(message.Exchange)) { throw new ArgumentNullException(nameof(message.Exchange)); }

            var exchange = GetExchangeFromName(message.Exchange);
            if (exchange == null) { throw new ApplicationException($"Failed to resolve exchange from name \"{message.Exchange}\"."); }
            
            if (!(exchange is ITradeGetCachedOrderBooks exchangeWithCachedOrderBooks))
            {
                throw new ApplicationException($"Exchange \"{exchange.Name}\" does not support cached order books.");
            }

            var cachedOrderBooks = exchangeWithCachedOrderBooks.GetCachedOrderBooks();

            var response = new GetCachedOrderBooksResponseMessage();
            if (cachedOrderBooks != null) {
                response.Payload = cachedOrderBooks.Select(item => ToContract(item)).ToList();
            }

            return response;
        }

        public GetCommodityDetailsResponseMessage Handle(GetCommodityDetailsRequestMessage message)
        {
            if (message == null) { throw new ArgumentNullException(nameof(message)); }
            if (string.IsNullOrWhiteSpace(message.Symbol)) { throw new ArgumentNullException(nameof(message.Symbol)); }
            var symbol = message.Symbol.Trim();

            Commodity canon = null;
            List<Commodity> canons = null;

            var parsedId = ParseUtil.GuidTryParse(symbol);
            if (parsedId.HasValue && parsedId.Value != default(Guid))
            {
                canon = CommodityRes.ById(parsedId.Value);
                if (canon == null) { throw new ApplicationException($"Failed to resolve canon by id {parsedId.Value}."); }
                canons = new List<Commodity> { canon };
            }
            else
            {
                canons = CommodityRes.BySymbolAllowMultiple(symbol);
                canon = canons.OrderByDescending(item => item.IsDominant).FirstOrDefault();
            }

            var items = new List<(ITradeIntegration exchange, Task<List<CommodityForExchange>> commoditiesTask, Task<List<TradingPair>> tradingPairsTask)>();
            foreach (var exchange in _exchanges)
            {
                var commoditiesTask = LongRunningTask.Run(() => exchange.GetCommodities(CachePolicy.OnlyUseCache));
                var tradingPairsTask = LongRunningTask.Run(() => exchange.GetTradingPairs(CachePolicy.OnlyUseCache));

                items.Add((exchange, commoditiesTask, tradingPairsTask));
            }

            var matchingExchanges = new List<(string exchangeName, List<string> baseCommodities)>();
            foreach (var (exchange, commoditiesTask, tradingPairsTask) in items)
            {
                try { commoditiesTask.Wait(); }
                catch (Exception exception)
                {
                    _log.Error($"Failed to retrieve commodities for exchange \"{exchange.Name}\".");
                    _log.Error(exception);
                    continue;
                }

                var commoditiesForExchange = commoditiesTask.Result;

                List<TradingPair> tradingPairsForExchange = null;
                try
                {
                    tradingPairsForExchange = tradingPairsTask.Result;
                }
                catch (Exception exception)
                {
                    tradingPairsForExchange = new List<TradingPair>();
                    _log.Error(exception);
                }

                if (commoditiesForExchange != null && commoditiesForExchange.Any(queryCommodity =>
                {
                    if (queryCommodity == null) { return false; }
                    if (queryCommodity.CanonicalId.HasValue && canon != null && canon.Id != default(Guid))
                    {
                        return queryCommodity.CanonicalId == canon.Id;
                    }

                    return string.Equals(symbol, queryCommodity.Symbol, StringComparison.InvariantCultureIgnoreCase);
                }))
                {
                    List<string> baseSymbols =
                        (tradingPairsTask.Result ?? new List<TradingPair>()).Where(queryTradingPair => string.Equals(queryTradingPair.Symbol, symbol, StringComparison.InvariantCultureIgnoreCase))
                        .Select(queryTradingPair => queryTradingPair.BaseSymbol)
                        .ToList();

                    matchingExchanges.Add((exchange.Name, baseSymbols));
                }
            }

            var recessiveCanons = canons.Where(item => item.Id != canon.Id).ToList();

            var exchangesWithBaseSymbols = new Dictionary<string, List<string>>();
            foreach (var matchingExchange in matchingExchanges)
            {
                exchangesWithBaseSymbols[matchingExchange.exchangeName]
                    = matchingExchange.baseCommodities;
            }

            var exchangesWithDetails = new List <CommodityDetailsContract.ExchangeDetails>();
            foreach (var (exchange, commoditiesTask, tradingPairsTask) in items)
            {
                var commodities = commoditiesTask.Result;
                List<TradingPair> tradingPairs = null;
                try
                {
                    tradingPairs = tradingPairsTask.Result;
                }
                catch (Exception exception)
                {
                    tradingPairs = new List<TradingPair>();
                    _log.Error(exception);
                }

                var matchingCommodity = commodities.SingleOrDefault(queryCommodity =>
                {
                    if (queryCommodity == null) { return false; }
                    if (queryCommodity.CanonicalId.HasValue && canon != null && canon.Id != default(Guid))
                    {
                        return queryCommodity.CanonicalId == canon.Id;
                    }

                    return string.Equals(symbol, queryCommodity.Symbol, StringComparison.InvariantCultureIgnoreCase);
                });

                var matchingTradingPairs = (tradingPairs ?? new List<TradingPair>()).Where(queryTradingPair =>
                {
                    if (queryTradingPair == null) { return false; }
                    if (queryTradingPair.CanonicalCommodityId.HasValue && canon != null && canon.Id != default(Guid))
                    {
                        return queryTradingPair.CanonicalCommodityId == canon.Id;
                    }

                    return string.Equals(symbol, queryTradingPair.Symbol, StringComparison.InvariantCultureIgnoreCase);
                });

                if (matchingCommodity != null)
                {
                    var detail = new CommodityDetailsContract.ExchangeDetails
                    {
                        Exchange = exchange.Name,

                        CanonicalId = matchingCommodity?.CanonicalId,
                        Symbol = matchingCommodity?.Symbol,
                        NativeSymbol = matchingCommodity?.NativeSymbol,
                        Name = matchingCommodity?.Name,
                        NativeName = matchingCommodity?.NativeName,

                        CanDeposit = matchingCommodity?.CanDeposit,
                        CanWithdraw = matchingCommodity?.CanWithdraw
                    };

                    exchangesWithDetails.Add(detail);
                }
            }

            var payload = new CommodityDetailsContract
            {
                CanonicalId = canon?.Id,
                Recessives = recessiveCanons != null
                    ? recessiveCanons.Where(item => item != null)
                        .Select(item => ToContract(item)).ToList()
                    : null,
                CanonicalName = canon?.Name,
                Symbol = symbol,
                Exchanges = exchangesWithBaseSymbols,
                ExchangesWithDetails = exchangesWithDetails,
                Website = canon?.Website,
                Telegram = canon?.Telegram
            };

            return new GetCommodityDetailsResponseMessage
            {
                Payload = payload
            };
        }

        private class ExhangeWithCommodities
        {
            public ITradeIntegration Exchange { get; set; }
            public List<ExchangeCommodityContract> Commodities { get; set; }
        }

        private static TimeSpan CommoditiesAggregateThreshold = TimeSpan.FromMinutes(5);

        private static ThrottleContext CommoditiesAggregateThrottleContext = new ThrottleContext
        {
            Locker = new object(),
            ThrottleThreshold = TimeSpan.Zero
        };

        public GetCommoditiesResponseMessage Handle(GetCommoditiesRequestMessage message)
        {
            var cachePolicy = message.CachePolicy != CachePolicyContract.Unknown
                ? message.CachePolicy
                : CachePolicyContract.OnlyUseCacheUnlessEmpty;

            var contractRetriever = new Func<List<CommodityWithExchangesContract>>(() =>
            {
                var exchangesWithCommoditiesTasks = _exchanges.Select(exchange =>
                {
                    var task = LongRunningTask.Run<ExhangeWithCommodities>(() =>
                        new ExhangeWithCommodities
                        {
                            Exchange = exchange,
                            Commodities = GetExchangeCommodities(exchange.Name, cachePolicy)
                        });


                    return task;
                }).ToList();

                // add a try-catch to the exchange call.
                var exchangesWithCommodities = exchangesWithCommoditiesTasks.Select(task => task.Result).ToList();

                var allCommodities = CommodityRes.All;
                var commodityIdDictionary = new Dictionary<Guid, Commodity>();
                foreach (var commodity in allCommodities)
                {
                    commodityIdDictionary[commodity.Id] = commodity;
                }

                var commodityViewModels = new List<CommodityWithExchangesContract>();
                foreach (var exchangeWithCommodities in exchangesWithCommodities)
                {
                    var exchange = exchangeWithCommodities.Exchange;
                    var commodities = exchangeWithCommodities.Commodities;
                    foreach (var item in commodities)
                    {
                        var canon = item.CanonicalId.HasValue && commodityIdDictionary.ContainsKey(item.CanonicalId.Value)
                            ? commodityIdDictionary[item.CanonicalId.Value]
                            : null;

                        if (canon != null)
                        {
                            var match = commodityViewModels.SingleOrDefault(cvm => cvm.Id == canon.Id);
                            if (match != null)
                            {
                                match.Exchanges.Add(exchange.Name);
                                continue;
                            }
                        }

                        var commodityViewModel = new CommodityWithExchangesContract
                        {
                            Id = item.CanonicalId,
                            Symbol = item.Symbol,
                            Name = canon?.Name ?? item.Name,
                            Decimals = canon?.Decimals,
                            Contract = canon?.ContractId,
                            Exchanges = new List<string> { exchange.Name }
                        };

                        commodityViewModels.Add(commodityViewModel);
                    }
                }

                return commodityViewModels
                    .OrderBy(item => item.Symbol)
                    .ToList();
            });

            var validator = new Func<string, bool>(text =>
            {
                return !string.IsNullOrWhiteSpace(text);
            });

            var textRetriever = new Func<string>(() =>
            {
                try
                {
                    var data = contractRetriever();
                    var text = JsonConvert.SerializeObject(data);
                    if (!validator(text))
                    {
                        throw new ApplicationException("ExchangeHandler.GetCommodities() - validation failed.");
                    }

                    return text;
                }
                catch(Exception exception)
                {
                    _log.Error(exception);
                    throw;
                }
            });

            var context = new MongoCollectionContext(DbContext, "exchange-service--get-commodities");

            var cacheResult = _cacheUtil.GetCacheableEx(
                CommoditiesAggregateThrottleContext,
                textRetriever,
                context,
                CommoditiesAggregateThreshold,
                ToModel(cachePolicy),
                validator);

            var payload = !string.IsNullOrWhiteSpace(cacheResult?.Contents)
                ? JsonConvert.DeserializeObject<List<CommodityWithExchangesContract>>(cacheResult.Contents)
                : null;            

            return new GetCommoditiesResponseMessage
            {
                Payload = payload
            };
        }

        public GetExchangesForCommodityResponseMessage Handle(GetExchangesForCommodityRequestMessage message)
        {
            if (message == null) { throw new ArgumentNullException(nameof(message)); }
            if (string.IsNullOrWhiteSpace(message.Symbol)) { throw new ArgumentNullException(nameof(message.Symbol)); }

            var symbol = message.Symbol.Trim();

            var tasks = _exchanges.Select(exchange =>
            {
                var task = new Task<List<ExchangeCommodityContract>>(() => 
                    GetExchangeCommodities(exchange, message.CachePolicy),
                    TaskCreationOptions.LongRunning);

                task.Start();

                return new ExchangeAndCommoditiesTask
                {
                    Name = exchange.Name,
                    Task = task
                };
            });

            var matches = new List<string>();

            foreach (var item in tasks)
            {
                var exchangeName = item.Name;
                var task = item.Task;
                
                try
                {
                    var commodities = item.Task.Result;
                    if (commodities != null && commodities.Any(commodity =>
                         commodity != null
                         && !string.IsNullOrWhiteSpace(commodity.Symbol)
                         && string.Equals(commodity.Symbol.Trim(), symbol, StringComparison.InvariantCultureIgnoreCase)))
                    {
                        matches.Add(item.Name);
                    }
                }
                catch (Exception exception)
                {
                    _log.Error(exception);
                }
            }

            return new GetExchangesForCommodityResponseMessage
            {
                Payload = matches
            };
        }

        public GetWithdrawalFeeResponseMessage Handle(GetWithdrawalFeeRequestMessage message)
        {
            var exchange = GetExchangeFromName(message.Exchange);
            if (exchange == null) { throw new ApplicationException($"Failed to resolve exchange from name \"{message.Exchange}\"."); }

            var result = exchange.GetWithdrawalFee(message.Symbol, ToModel(message.CachePolicy));

            return new GetWithdrawalFeeResponseMessage
            {
                Payload = new GetWithdrawalFeeResponseMessage.GetWithdrawalFeePayload
                {
                    WithdrawalFee = result
                }
            };
        }

        public GetOpenOrdersResponseMessage Handle(GetOpenOrdersRequestMessage message)
        {
            var exchange = GetExchangeFromName(message.Exchange);
            if (exchange == null) { throw new ApplicationException($"Failed to resolve exchange from name \"{message.Exchange}\"."); }

            if (!(exchange is IExchangeWithOpenOrders exchangeWithOpenOrders)) { throw new ApplicationException($"Exchange {exchange.Name} does not support getting open orders."); }
            try
            {
                var openOrders = exchangeWithOpenOrders.GetOpenOrders(ToModel(message.CachePolicy));
                return new GetOpenOrdersResponseMessage
                {
                    Payload = ToContract(openOrders)
                };
            }
            catch(Exception exception)
            {
                _log.Error(exception);
                throw;
            }
        }

        public GetOpenOrdersResponseMessageV2 Handle(GetOpenOrdersRequestMessageV2 message)
        {
            var exchange = GetExchangeFromName(message.Exchange);
            if (exchange == null) { throw new ApplicationException($"Failed to resolve exchange from name \"{message.Exchange}\"."); }

            if (!(exchange is IExchangeGetOpenOrdersV2 exchangeV2))
            {
                throw new ApplicationException($"Exchange {exchange.Name} does not support GetOpenOrdersV2.");
            }

            var openOrders = exchangeV2.GetOpenOrdersV2();

            var contract = openOrders != null
                ? openOrders.Select(ToContract).ToList()
                : null;

            return new GetOpenOrdersResponseMessageV2
            {
                Payload = new GetOpenOrdersResponseMessageV2.ResponsePayload
                {
                    OpenOrdersForTradingPairs = contract
                }
            };
        }

        public class ManualResetEventSlimEx
        {
            private readonly TimeSpan _maxWaitTime;
            private readonly TimeSpan _forbidEnqueueRunningTime;

            private readonly ManualResetEventSlim _slim;
            private DateTime? _executionStartTime;

            public ManualResetEventSlimEx(
                TimeSpan maxWaitTime,
                TimeSpan forbidEnqueueRunningTime)
            {
                _maxWaitTime = maxWaitTime;
                _forbidEnqueueRunningTime = forbidEnqueueRunningTime;

                _slim = new ManualResetEventSlim(true);
            }

            public T Execute<T>(Func<T> method)
            {
                var timeSince = DateTime.UtcNow - _executionStartTime;
                if (timeSince.HasValue && timeSince >= _forbidEnqueueRunningTime)
                {
                    throw new ApplicationException("The existing process has been running too long. No more requests can be enqueued until it has completed.");
                }

                if (!_slim.Wait(_maxWaitTime)) { throw new ApplicationException("Failed to get slim."); }
                try
                {
                    _executionStartTime = DateTime.UtcNow;
                    var result = method();
                    return result;
                }
                finally
                {
                    _executionStartTime = null;
                    _slim.Set();
                }
            }
        }

        private static Dictionary<string, ManualResetEventSlimEx> ConcernLocker = new Dictionary<string, ManualResetEventSlimEx>();

        public GetOpenOrdersForTradingPairResponseMessageV2 Handle(GetOpenOrdersForTradingPairRequestMessageV2 message)
        {
            var exchange = GetExchangeFromName(message.Exchange);
            if (exchange == null) { throw new ApplicationException($"Failed to resolve exchange from name \"{message.Exchange}\"."); }

            if (!(exchange is IExchangeGetOpenOrdersForTradingPairV2 exchangeV2))
            {
                throw new ApplicationException($"Exchange {exchange.Name} does not support GetOpenOrdersV2.");
            }

            var key = $"{exchange.Name}_HandleGetOpenOrdersForTradingPairRequestMessageV2";

            var maxWaitTime = TimeSpan.FromSeconds(30);
            var forbidEnqueueRunningTime = TimeSpan.FromMinutes(2);
            var locker = ConcernLocker.ContainsKey(key)
                ? ConcernLocker[key]
                : (ConcernLocker[key] = new ManualResetEventSlimEx(maxWaitTime, forbidEnqueueRunningTime));

            return locker.Execute(() =>
            {
                var result = exchangeV2.GetOpenOrdersForTradingPairV2(message.Symbol, message.BaseSymbol, (CachePolicy)message.CachePolicy);

                var openOrdersContract = result?.OpenOrders != null
                    ? result.OpenOrders.Select(ToContract).ToList()
                    : null;

                return new GetOpenOrdersForTradingPairResponseMessageV2
                {
                    Payload = new GetOpenOrdersForTradingPairResponseMessageV2.ResponsePayload
                    {
                        AsOfUtc = result?.AsOfUtc,
                        OpenOrders = openOrdersContract
                    }
                };
            });
        }

        public GetOpenOrdersForTradingPairResponseMessage Handle(GetOpenOrdersForTradingPairRequestMessage message)
        {
            var exchange = GetExchangeFromName(message.Exchange);
            if (exchange == null) { throw new ApplicationException($"Failed to resolve exchange from name \"{message.Exchange}\"."); }

            if (exchange is IExchangeWithOpenOrdersForTradingPair exchangeWithOpenOrdersForTradingPair)
            {
                try
                {
                    var result = exchangeWithOpenOrdersForTradingPair.GetOpenOrders(message.Symbol, message.BaseSymbol, ToModel(message.CachePolicy));
                    return new GetOpenOrdersForTradingPairResponseMessage
                    {
                        Payload = ToContract(result)
                    };
                }
                catch (Exception exception)
                {
                    if (exception is AggregateException
                        && exception.InnerException != null
                        && exception.InnerException is WebException webException)
                    {
                        throw webException;
                    }

                    throw;
                }
            }

            if (!(exchange is IExchangeWithOpenOrders exchangeWithOpenOrders)) { throw new ApplicationException($"Exchange {exchange.Name} does not support getting open orders."); }
            var openOrders = exchangeWithOpenOrders.GetOpenOrders(ToModel(message.CachePolicy));
            var matchingOpenOrders = openOrders != null
                ? openOrders.Where(queryOpenOrder => 
                    string.Equals(queryOpenOrder.Symbol, message.Symbol, StringComparison.InvariantCultureIgnoreCase)
                    && string.Equals(queryOpenOrder.BaseSymbol, message.BaseSymbol, StringComparison.InvariantCultureIgnoreCase))
                  .ToList()
                : null;

            return new GetOpenOrdersForTradingPairResponseMessage
            {
                Payload = ToContract(matchingOpenOrders)
            };
        }

        public SellLimitResponseMessage Handle(SellLimitRequestMessage message)
        {
            if (message.Payload == null) { throw new ArgumentNullException(nameof(message.Payload)); }
            if (string.IsNullOrWhiteSpace(message.Payload.Symbol)) { throw new ArgumentNullException(nameof(message.Payload.Symbol)); }
            if (string.IsNullOrWhiteSpace(message.Payload.BaseSymbol)) { throw new ArgumentNullException(nameof(message.Payload.BaseSymbol)); }
            if (message.Payload.Price <= 0) { throw new ArgumentOutOfRangeException(nameof(message.Payload.Price)); }
            if (message.Payload.Quantity <= 0) { throw new ArgumentOutOfRangeException(nameof(message.Payload.Quantity)); }

            var exchange = GetExchangeFromName(message.Payload.Exchange);
            if (exchange == null) { throw new ApplicationException($"Failed to resolve exchange from name \"{message.Payload.Exchange}\"."); }

            if (exchange is ILimitIntegrationWithResult detailedIntegration)
            {
                var detailedResult = detailedIntegration.SellLimit(message.Payload.Symbol, message.Payload.BaseSymbol, new QuantityAndPrice { Quantity = message.Payload.Quantity, Price = message.Payload.Price });
                if (detailedResult == null) { throw new ApplicationException($"{message.Payload.Exchange} returned a null result when attempting to sell {message.Payload.Quantity} {message.Payload.Symbol} at {message.Payload.Price} {message.Payload.BaseSymbol}"); }

                if (!detailedResult.WasSuccessful)
                {
                    throw new HandlerException(detailedResult.FailureReason);
                }

                return new SellLimitResponseMessage
                {
                    WasSuccessful = detailedResult.WasSuccessful,
                    FailureReason = detailedResult.FailureReason,
                    Payload = new LimitResponseMessage.ResponsePayload
                    {
                        OrderId = detailedResult?.OrderId,
                        Executed = detailedResult?.Executed,
                        Price = detailedResult?.Price,
                        Quantity = detailedResult?.Quantity
                    }
                };
            }

            if (!(exchange is ISellLimitIntegration sellLimitIntegration)) { throw new ApplicationException($"Exchange {exchange.Name} does not support limit sell."); }

            var result = sellLimitIntegration.SellLimit(new TradingPair(message.Payload.Symbol, message.Payload.BaseSymbol), message.Payload.Quantity, message.Payload.Price);

            if (!result)
            {
                throw new ApplicationException($"{exchange} returned a failure result when attempting to sell {message.Payload.Quantity} {message.Payload.Symbol} at {message.Payload.Price} {message.Payload.BaseSymbol}");
            }

            return new SellLimitResponseMessage();
        }

        public BuyLimitResponseMessage Handle(BuyLimitRequestMessage message)
        {
            if (string.IsNullOrWhiteSpace(message.Symbol)) { throw new ArgumentNullException(nameof(message.Symbol)); }
            if (string.IsNullOrWhiteSpace(message.BaseSymbol)) { throw new ArgumentNullException(nameof(message.BaseSymbol)); }
            if (message.Price <= 0) { throw new ArgumentOutOfRangeException(nameof(message.Price)); }
            if (message.Quantity <= 0) { throw new ArgumentOutOfRangeException(nameof(message.Quantity)); }

            var exchange = GetExchangeFromName(message.Exchange);
            if (exchange == null) { throw new ApplicationException($"Failed to resolve exchange from name \"{message.Exchange}\"."); }

            if (exchange is ILimitIntegrationWithResult detailedIntegration)
            {
                var detailedResult = detailedIntegration.BuyLimit(message.Symbol, message.BaseSymbol, new QuantityAndPrice { Quantity = message.Quantity, Price = message.Price });
                if (detailedResult == null) { throw new ApplicationException($"{message.Exchange} returned a null result when attempting to buy {message.Quantity} {message.Symbol} at {message.Price} {message.BaseSymbol}"); }

                if (!detailedResult.WasSuccessful)
                {
                    throw new HandlerException(detailedResult.FailureReason);
                }

                return new BuyLimitResponseMessage
                {
                    WasSuccessful = detailedResult.WasSuccessful,
                    FailureReason = detailedResult.FailureReason,
                    Payload = new LimitResponseMessage.ResponsePayload
                    {
                        OrderId = detailedResult?.OrderId,
                        Executed = detailedResult?.Executed,
                        Price = detailedResult?.Price,
                        Quantity = detailedResult?.Quantity
                    }
                };
            }

            if (!(exchange is IBuyLimitIntegration buyLimitIntegration)) { throw new ApplicationException($"Exchange {exchange.Name} does not support limit buy."); }

            var tradingPair = new TradingPair(message.Symbol, message.BaseSymbol);
            var result = buyLimitIntegration.BuyLimit(tradingPair, message.Quantity, message.Price);

            if (!result)
            {
                throw new ApplicationException($"{exchange} returned a failure result when attempting to buy {message.Quantity} {message.Symbol} at {message.Price} {message.BaseSymbol}");
            }

            return new BuyLimitResponseMessage();
        }

        public CancelOrderResponseMessage Handle(CancelOrderRequestMessage message)
        {
            var exchange = GetExchangeFromName(message.Exchange);
            if (exchange == null) { throw new ApplicationException($"Failed to resolve exchange from name \"{message.Exchange}\"."); }
            if(string.IsNullOrWhiteSpace(message.OrderId)) { throw new ArgumentNullException(nameof(message.OrderId)); }

            if (!(exchange is ICancelOrderIntegration cancelOrderIntegration))
            { throw new ApplicationException($"Exchange {message.Exchange} does not implement {typeof(ICancelOrderIntegration).Name}."); }

            cancelOrderIntegration.CancelOrder(message.OrderId);

            return new CancelOrderResponseMessage();
        }

        public WithdrawCommodityResponseMessage Handle(WithdrawCommodityRequestMessage message)
        {
            var exchange = GetExchangeFromName(message.Payload.SourceExchange);
            if (exchange == null) { throw new ApplicationException($"Failed to resolve exchange from name \"{message.Payload.SourceExchange}\"."); }

            var withdrawableExchange = exchange as IWithdrawableTradeIntegration;
            if (withdrawableExchange == null)
            {
                throw new ApplicationException($"Exchange \"{exchange.Name}\" is not setup for withdrawals.");
            }

            var commodity = new Commodity
            {
                Symbol = message.Payload.Symbol
            };

            var depositAddress = ToModel(message.Payload.DepositAddress);

            var result = withdrawableExchange.Withdraw(commodity, message.Payload.Quantity, depositAddress);
            if (!result)
            {
                throw new ApplicationException($"Failed to withdraw {message.Payload.Quantity} {message.Payload.Symbol} from {exchange.Name}");
            }

            return new WithdrawCommodityResponseMessage();
        }

        private class ExchangeAndCommoditiesTask
        {
            public string Name { get; set; }
            public Task<List<ExchangeCommodityContract>> Task { get; set; }
        }

        private IMongoDatabaseContext DbContext =>
            new MongoDatabaseContext(_getConnectionString.GetConnectionString(), DatabaseName);


        private List<ExchangeCommodityContract> GetExchangeCommodities(ITradeIntegration exchange, CachePolicyContract cachePolicy)
        {
            if (exchange == null) { throw new ArgumentNullException(nameof(exchange)); }

            var commodities = Time(() => exchange.GetCommodities((CachePolicy)cachePolicy),
                $"Get {exchange.Name} commodities with cache policy \"{cachePolicy}\".");

            var translated = commodities != null
                ? commodities.Select(model => ToContract(model)).ToList()
                : null;

            return translated;
        }

        private List<ExchangeCommodityContract> GetExchangeCommodities(string exchangeName, CachePolicyContract cachePolicy)
        {
            if (string.IsNullOrWhiteSpace(exchangeName)) { throw new ArgumentNullException(nameof(exchangeName)); }

            var exchange = GetExchangeFromName(exchangeName);
            if (exchange == null) { throw new ApplicationException($"Failed to retrieve exchange from name \"{exchangeName}\"."); }

            return GetExchangeCommodities(exchange, cachePolicy);
        }

        private ITradeIntegration GetExchangeFromName(string exchangeName)
        {
            if (string.IsNullOrWhiteSpace(exchangeName)) { throw new ArgumentNullException(nameof(exchangeName)); }

            var effectiveExchangeName = exchangeName.Trim().ToUpper().Replace("-", "");
            return _exchanges.SingleOrDefault(queryExchange =>
            {
                var compExchangeName = queryExchange.Name.Trim().ToUpper().Replace("-", "");
                return string.Equals(compExchangeName, effectiveExchangeName, StringComparison.InvariantCultureIgnoreCase);
            });
        }

        private List<TradingPairContract> ToContract(List<TradingPair> models)
        {
            return models != null
                ? models.Select(item => ToContract(item)).ToList()
                : null;
        }

        private TradingPairContract ToContract(TradingPair model)
        {
            return model != null
                ? new TradingPairContract
                {
                    BaseCommodityName = model.BaseCommodityName,
                    BaseSymbol = model.BaseSymbol,
                    CanonicalBaseCommodityId = model.CanonicalBaseCommodityId,
                    CanonicalCommodityId = model.CanonicalCommodityId,
                    CommodityName = model.CommodityName,
                    CustomBaseCommodityValues = model.CustomBaseCommodityValues,
                    CustomCommodityValues = model.CustomCommodityValues,
                    NativeBaseCommodityName = model.NativeBaseCommodityName,
                    NativeBaseSymbol = model.NativeBaseSymbol,
                    NativeCommodityName = model.NativeCommodityName,
                    NativeSymbol = model.NativeSymbol,
                    Symbol = model.Symbol,
                    LotSize = model.LotSize,
                    PriceTick = model.PriceTick,
                    MinimumTradeBaseSymbolValue = model.MinimumTradeBaseSymbolValue,
                    MinimumTradeQuantity = model.MinimumTradeQuantity
                }
                : null;
        }

        private OrderContract ToContract(Order order)
        {
            if (order == null) { return null; }
            return new OrderContract
            {
                Price = order.Price,
                Quantity = order.Quantity
            };
        }

        private OrderBookContract ToContract(OrderBook orderBook)
        {
            if (orderBook == null) { return null; }
            return new OrderBookContract
            {
                Asks = orderBook?.Asks != null ? orderBook.Asks.Select(queryOrder => ToContract(queryOrder)).ToList() : null,
                Bids = orderBook?.Bids != null ? orderBook.Bids.Select(queryOrder => ToContract(queryOrder)).ToList() : null,
                AsOf = orderBook?.AsOf
            };
        }

        private OrderBookAndTradingPairContract ToContract(OrderBookAndTradingPair orderBook)
        {
            if (orderBook == null) { return null; }
            return new OrderBookAndTradingPairContract
            {
                Asks = orderBook?.Asks != null ? orderBook.Asks.Select(queryOrder => ToContract(queryOrder)).ToList() : null,
                Bids = orderBook?.Bids != null ? orderBook.Bids.Select(queryOrder => ToContract(queryOrder)).ToList() : null,
                AsOf = orderBook?.AsOf,
                Symbol = orderBook?.Symbol,
                BaseSymbol = orderBook?.BaseSymbol
            };
        }

        private TradingPair ToModel(TradingPairContract tradingPairContract)
        {
            if (tradingPairContract == null) { return null; }
            return new TradingPair
            {
                Symbol = tradingPairContract.Symbol,
                BaseSymbol = tradingPairContract.BaseSymbol
            };
        }

        private DepositAddress ToModel(DepositAddressContract contract)
        {
            return contract != null
                ? new DepositAddress
                {
                    Address = contract.DepositAddress,
                    Memo = contract.DepositMemo
                }
                : null;
        }

        public ExchangeCommodityContract ToContract(CommodityForExchange model)
        {
            if (model == null) { return null; }
            return new ExchangeCommodityContract
            {
                CanonicalId = model.CanonicalId,
                CanDeposit = model.CanDeposit,
                CanWithdraw = model.CanWithdraw,
                ContractAddress = model.ContractAddress,
                CustomValues = model.CustomValues,
                MinimumTradeQuantity = model.MinimumTradeQuantity,
                MinimumTradeBaseSymbolValue = model.MinimumTradeBaseSymbolValue,
                Name = model.Name,
                NativeName = model.NativeName,
                NativeSymbol = model.NativeSymbol,
                Symbol = model.Symbol,
                WithdrawalFee = model.WithdrawalFee,
                LotSize = model.LotSize
            };
        }

        public DepositAddressContract ToContract(DepositAddress model)
        {
            return model != null
                ? new DepositAddressContract
                {
                    DepositAddress = model.Address,
                    DepositMemo = model.Memo
                }
                : null;
        }

        private BalanceContract ToContract(Holding model)
        {
            return model != null
                ? new BalanceContract
                {
                    Symbol = model.Asset,
                    AdditionalBalances = model.AdditionalHoldings,
                    Available = model.Available,
                    InOrders = model.InOrders,
                    Total = model.Total
                }
                : null;
        }

        private CommodityContract ToContract(Commodity model)
        {
            if (model == null) { return null; }

            return new CommodityContract
            {
                Id = model.Id,
                Name = model.Name,
                Symbol = model.Symbol,
                IsEth = model.IsEth,
                IsEthToken = model.IsEth,
                ContractId = model.ContractId,
                Decimals = model.Decimals,
                IsDominant = model.IsDominant
            };
        }

        private CachePolicy ToModel(CachePolicyContract contract)
        {
            return (CachePolicy)contract;
        }

        private OpenOrderForTradingPairContract ToContract(OpenOrderForTradingPair model)
        {
            return model != null
                ? new OpenOrderForTradingPairContract
                {
                    BaseSymbol = model.BaseSymbol,
                    OrderId = model.OrderId,
                    OrderType = (OrderTypeContractEnum)model.OrderType,
                    Price = model.Price,
                    Quantity = model.Quantity,
                    Symbol = model.Symbol
                }
                : null;
        }

        private List<OpenOrderForTradingPairContract> ToContract(List<OpenOrderForTradingPair> model)
        {
            return model != null
                ? model.Select(item => ToContract(item)).ToList()
                : null;
        }

        private List<HistoryItemContract> ToContract(List<HistoricalTrade> model)
        {
            return model != null
                ? model.Select(item => ToContract(item)).ToList()
                : null;
        }

        private HistoryItemContract ToContract(HistoricalTrade model)
        {
            return model != null
                ? new HistoryItemContract
                {
                    NativeId = model.NativeId,
                    Comments = model.Comments,
                    Symbol = model.Symbol,
                    BaseSymbol = model.BaseSymbol,
                    TimeStampUtc = model.TimeStampUtc,
                    SuccessTimeStampUtc = model.SuccessTimeStampUtc,
                    Price = model.Price,
                    Quantity = model.Quantity,
                    FeeQuantity = model.FeeQuantity,
                    FeeCommodity = model.FeeCommodity,
                    TradeType = (TradeTypeEnumContract)model.TradeType,
                    TradeStatus = (TradeStatusEnumContract)model.TradeStatus,
                    WalletAddress = model.WalletAddress,
                    TransactionHash = model.TransactionHash
                }
                : null;
        }

        private OpenOrdersForTradingPairContract ToContract(OpenOrdersForTradingPair model)
        {
            return model != null
                ? new OpenOrdersForTradingPairContract
                {
                    Symbol = model.Symbol,
                    BaseSymbol = model.BaseSymbol,
                    AsOfUtc = model.AsOfUtc,
                    OpenOrders = model.OpenOrders != null
                        ? model.OpenOrders.Select(queryOpenOrderModel => ToContract(queryOpenOrderModel)).ToList()
                        : null
                }
                : null;
        }

        private OpenOrderContract ToContract(OpenOrder model)
        {
            return model != null
                ? new OpenOrderContract
                {
                    OrderId = model.OrderId,
                    OrderType = (OrderTypeContractEnum)model.OrderType,
                    Price = model.Price,
                    Quantity = model.Quantity
                }
                : null;
        }

        public KeepHitbtcHealthFreshResponseMessage Handle(KeepHitbtcHealthFreshRequestMessage message)
        {
            throw new NotImplementedException();
            //_hitBtcIntegration.KeepHealthFresh();
            //return new KeepHitbtcHealthFreshResponseMessage();
        }

        public GetHistoryForTradingPairResponseMessage Handle(GetHistoryForTradingPairRequestMessage message)
        {
            var exchange = GetExchangeFromName(message.Payload.Exchange);
            var coss = exchange as ICossIntegration;

            if (coss == null) { throw new ApplicationException("Only implemented for Coss."); }
            var history = coss.GetUserTradeHistoryForTradingPair(message.Payload.Symbol, message.Payload.BaseSymbol, ToModel(message.Payload.CachePolicy));

            // TODO: Add the history;
            return new GetHistoryForTradingPairResponseMessage { };
        }
    }
}
