using cache_lib.Models;
using client_lib;
using env_config_lib;
using exchange_client_lib.Models;
using log_lib;
using rabbit_lib;
using reflection_lib;
using System;
using System.Collections.Generic;
using System.Linq;
using trade_constants;
using trade_contracts;
using trade_contracts.Messages;
using trade_contracts.Messages.Exchange;
using trade_contracts.Messages.Exchange.Balance;
using trade_contracts.Messages.Exchange.History;
using trade_contracts.Messages.Exchange.HitBtc;
using trade_contracts.Messages.Exchange.OpenOrders;
using trade_contracts.Messages.Exchange.Withdraw;
using trade_contracts.Models;
using trade_contracts.Models.OpenOrders;
using trade_contracts.Payloads;
using trade_model;
using web_util;

namespace exchange_client_lib
{
    public class ExchangeClient : ServiceClient, IExchangeClient
    {
        private const string RabbitOverrideKey = "TRADE_RABBIT_EXCHANGE";

        private readonly ILogRepo _log;

        protected override string QueueName => TradeRabbitConstants.Queues.ExchangeServiceQueue;

        private static Func<IRequestResponse> RequestResponseFactory = new Func<IRequestResponse>(() =>
        {
            // var envConfigRepo = new EnvironmentConfigRepo();
            var envConfigRepo = new EnvironmentConfigRepo(RabbitOverrideKey);

            var rabbitConnectionFactory = new RabbitConnectionFactory(envConfigRepo);
            return new RequestResponse(rabbitConnectionFactory);
        });

        public ExchangeClient(
            IRequestResponse requestResponse,
            ILogRepo log)
            : base(requestResponse)
        {
            _log = log;
        }

        public ExchangeClient()
            : base(RequestResponseFactory())
        {
            var webUtil = new WebUtil();
            _log = new LogRepo();
        }

        public DepositAddress GetDepositAddress(string exchange, string symbol, CachePolicy cachePolicy)
        {
            var req = new GetDepositAddressRequestMessage { Exchange = exchange, Symbol = symbol, CachePolicy = ToContract(cachePolicy) };
            var response = RequestResponse.Execute<GetDepositAddressRequestMessage, GetDepositAddressResponseMessage>(
                req,
                VersionedQueue(2));

            return ToModel(response?.DepositAddress);
        }

        public HistoryContainer GetExchangeHistory(string exchange, int limit, CachePolicy cachePolicy)
        {
            var req = new GetExchangeHistoryRequestMessage
            {
                Exchange = exchange,
                Limit = limit,
                CachePolicy = ToContract(cachePolicy)
            };

            var response = RequestResponse.Execute<GetExchangeHistoryRequestMessage, GetExchangeHistoryResponseMessage>(
                req,
                VersionedQueue(2));

            return new HistoryContainer
            {
                AsOfUtc = response?.Payload?.AsOfUtc,
                History = ToModel(response?.Payload?.History)
            };
        }

        public HistoryContainerWithExchanges GetAggregateHistory(int? limit, CachePolicy cachePolicy)
        {
            var req = new GetAggregateExchangeHistoryRequestMessage
            {
                Payload = new GetAggregateExchangeHistoryRequestMessage.RequestPayload
                {
                    Limit = limit,
                    CachePolicy = ToContract(cachePolicy)
                }
            };

            var response = RequestResponse.Execute<GetAggregateExchangeHistoryRequestMessage, GetAggregateExchangeHistoryResponseMessage>(
                req,
                VersionedQueue(2));

            return new HistoryContainerWithExchanges
            {
                AsOfUtcByExchange = response?.Payload?.AsOfUtcByExchange,
                History = ToModel(response?.Payload?.History)
            };
        }

        public List<CommodityForExchange> GetCommoditiesForExchange(string exchange, CachePolicy cachePolicy)
        {
            try
            {
                var req = new GetCommoditiesForExchangeRequestMessage { Exchange = exchange, CachePolicy = ToContract(cachePolicy) };
                var response = RequestResponse.Execute<GetCommoditiesForExchangeRequestMessage, GetCommoditiesForExchangeResponseMessage>(
                    req,
                    VersionedQueue(2));

                return response?.Commodities != null
                    ? response.Commodities.Select(item => ToModel(item)).ToList()
                    : null;
            }
            catch (Exception exception)
            {
                _log.Error($"Failed to get commodities for exchange {exchange} with cache policy {cachePolicy}.{Environment.NewLine}{exception.Message}");
                _log.Error(exception);
                throw;
            }
        }

        public DetailedExchangeCommodity GetCommoditiyForExchange(
            string exchange,
            string symbol,
            string nativeSymbol,
            CachePolicy cachePolicy)
        {
            if (string.IsNullOrWhiteSpace(exchange)) { throw new ArgumentNullException(nameof(exchange)); }
            if (string.IsNullOrWhiteSpace(symbol) && string.IsNullOrWhiteSpace(nativeSymbol))
            {
                throw new ArgumentException($"Both {nameof(symbol)} and {nameof(nativeSymbol)} must not be null/empty.");
            }

            try
            {
                var req = new GetDetailedCommodityForExchangeRequestMessage
                {
                    Exchange = exchange,
                    Symbol = symbol,
                    NativeSymbol = nativeSymbol,
                    CachePolicy = (CachePolicyContract)cachePolicy
                };

                var response = RequestResponse.Execute<GetDetailedCommodityForExchangeRequestMessage, GetDetailedCommodityForExchangeResponseMessage>(
                    req,
                    VersionedQueue(2));

                return ToModel(response?.Commodity);
            }
            catch (Exception exception)
            {
                _log.Error($"Failed to retrieve commodity for exchange {exchange} with native symbol {nativeSymbol} and cache policy {cachePolicy}.{Environment.NewLine}{exception.Message}");
                _log.Error(exception);
                throw;
            }            
        }

        private DetailedExchangeCommodity ToModel(DetailedExchangeCommodityContract contract)
        {
            return contract != null
                ? new DetailedExchangeCommodity
                {
                    CanonicalId = contract.CanonicalId,
                    NativeSymbol = contract.NativeSymbol,
                    Symbol = contract.Symbol,
                    Name = contract.Name,
                    NativeName = contract.NativeName,
                    ContractAddress = contract.ContractAddress,
                    CanDeposit = contract.CanDeposit,
                    CanWithdraw = contract.CanWithdraw,
                    WithdrawalFee = contract.WithdrawalFee,
                    Exchange = contract.Exchange,
                    DepositAddress = contract.DepositAddress,
                    DepositMemo = contract.DepositMemo,
                    LotSize = contract.LotSize,
                    BaseSymbols = contract.BaseSymbols
                }
                : null;
        }

        public List<TradingPair> GetTradingPairs(string exchange, CachePolicy cachePolicy)
        {
            try
            {
                var req = new GetTradingPairsForExchangeRequestMessage { Exchange = exchange, CachePolicy = (CachePolicyContract)cachePolicy };
                var response = RequestResponse.Execute<GetTradingPairsForExchangeRequestMessage, GetTradingPairsForExchangeResponseMessage>(
                    req,
                    VersionedQueue(2));

                return ToModel(response?.TradingPairs);
            }
            catch
            {
                _log.Error($"Failed to get trading pairs for exchange {exchange} with cache policy {cachePolicy.ToString()}");
                throw;
            }
        }

        private static List<Exchange> _cachedExchanges = null;
        private static DateTime? _cachedExchangesTimeStamp = null;

        public List<Exchange> GetExchanges()
        {
            if (_cachedExchanges != null && _cachedExchangesTimeStamp.HasValue 
                && DateTime.UtcNow - _cachedExchangesTimeStamp.Value < TimeSpan.FromMinutes(5))
            {
                return _cachedExchanges;
            }

            var req = new GetExchangesRequestMessage();
            var response = RequestResponse.Execute<GetExchangesRequestMessage, GetExchangesResponseMessage>(
                req, 
                VersionedQueue(0));

            if (response != null && response.WasSuccessful && response.Exchanges != null && response.Exchanges.Any())
            {
                _cachedExchanges = ToModel(response.Exchanges);
                _cachedExchangesTimeStamp = DateTime.UtcNow;
            }

            return ToModel(response?.Exchanges);
        }

        public Exchange GetExchange(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) { throw new ArgumentNullException(nameof(name)); }

            var effectiveName = name.Trim().Replace("-", string.Empty);

            var allExchanges = GetExchanges();
            return allExchanges.SingleOrDefault(queryExchange =>
                string.Equals(queryExchange.Name.Replace("-", string.Empty), effectiveName, StringComparison.InvariantCultureIgnoreCase));
        }

        // TODO: This should move to the valuation service.
        public List<string> GetCryptoCompareSymbols()
        {
            var req = new GetCryptoCompareSymbolsRequestMessage();
            var response = RequestResponse.Execute<GetCryptoCompareSymbolsRequestMessage, GetCryptoCompareSymbolsResponseMessage>(
                req,
                VersionedQueue(3));

            return response.Symbols;
        }

        public Dictionary<string, decimal> GetWithdrawalFees(string exchange, CachePolicy cachePolicy)
        {
            try
            {
                var req = new GetWithdrawalFeesRequestMessage { Exchange = exchange, CachePolicy = ToContract(cachePolicy) };
                var response = RequestResponse.Execute<GetWithdrawalFeesRequestMessage, GetWithdrawalFeesResponseMessage>(
                    req,
                    VersionedQueue(1));

                return response?.WithdrawalFees;
            }
            catch(Exception exception)
            {
                _log.Error($"Exchange client failed to get withdrawal from exchange {exchange} with cache policy {cachePolicy}.{Environment.NewLine}{exception.Message}");
                throw;
            }
        }

        public decimal? GetWithdrawalFee(string exchange, string symbol, CachePolicy cachePolicy)
        {
            var req = new GetWithdrawalFeeRequestMessage
            {
                Exchange = exchange,
                Symbol = symbol,
                CachePolicy = (CachePolicyContract)cachePolicy
            };

            var response = RequestResponse.Execute<GetWithdrawalFeeRequestMessage, GetWithdrawalFeeResponseMessage>(
                req,
                VersionedQueue(1));

            return response?.Payload?.WithdrawalFee;
        }

        public OrderBook GetOrderBook(string exchange, string symbol, string baseSymbol, CachePolicy cachePolicy)
        {
            try
            {
                var req = new GetOrderBookRequestMessage { Exchange = exchange, TradingPair = new TradingPairContract { Symbol = symbol, BaseSymbol = baseSymbol }, CachePolicy = (CachePolicyContract)cachePolicy };
                var response = RequestResponse.Execute<GetOrderBookRequestMessage, GetOrderBookResponseMessage>(
                    req,
                    VersionedQueue(1));

                return ToModel(response?.OrderBook);
            }
            catch (Exception exception)
            {
                _log.Error($"ExchangeClient.GetOrderBook() failed for exchange {exchange ?? "(null)"}, symbol: {symbol ?? "(null)"}, baseSymbol: {baseSymbol ?? "(null)"}{Environment.NewLine}{exception.Message}");
                throw;
            }
        }

        public RefreshOrderBookResultContract RefreshOrderBook(string exchange, string symbol, string baseSymbol)
        {
            var req = new RefreshOrderBookRequestMessage { Exchange = exchange, Symbol = symbol, BaseSymbol = baseSymbol };
            var response = RequestResponse.Execute<RefreshOrderBookRequestMessage, RefreshOrderBookResponseMessage>(
                req,
                VersionedQueue(1));

            return response?.Result;
        }

        public string GetExchangeName(string exchangeName)
        {
            if (string.IsNullOrWhiteSpace(exchangeName)) { throw new ArgumentNullException(nameof(exchangeName)); }

            var effectiveExchangeName = exchangeName.Trim().ToUpper().Replace("-", string.Empty);

            var exchanges = GetExchanges();
            var match = exchanges.SingleOrDefault(queryExchange =>
                string.Equals(effectiveExchangeName, queryExchange.Name != null ? queryExchange.Name.Trim().Replace("-", string.Empty) : null, StringComparison.InvariantCultureIgnoreCase));

            if (match == null) { throw new ApplicationException($"No matching exchange found by name \"{exchangeName}\""); }

            return match.Name;
        }

        public List<BalanceWithAsOf> GetBalances(string exchange, List<string> symbols, CachePolicy cachePolicy)
        {
            var req = new GetBalanceForCommoditiesAndExchangeRequestMessage
            {
                Payload = new GetBalanceForCommoditiesAndExchangeRequestMessage.RequestPayload
                {
                    Exchange = exchange,
                    Symbols = symbols,
                    CachePolicy = ToContract(cachePolicy)
                }
            };

            var response = RequestResponse.Execute<GetBalanceForCommoditiesAndExchangeRequestMessage, GetBalanceForCommoditiesAndExchangeResponseMessage>(
                req,
                VersionedQueue(2));

            return response.Payload.Balances.Select(queryBalance =>
            {
                return new BalanceWithAsOf
                {
                    Available = queryBalance.Available,
                    AdditionalBalanceItems = queryBalance.AdditionalBalances,
                    AsOfUtc = queryBalance.AsOfUtc,
                    InOrders = queryBalance.InOrders,
                    Symbol = queryBalance.Symbol,
                    Total = queryBalance.Total
                };
            }).ToList();
        }

        public HoldingInfo GetBalances(string exchange, CachePolicy cachePolicy)
        {
            var req = new GetBalanceRequestMessage { Exchange = exchange, CachePolicy = ToContract(cachePolicy) };
            var response = RequestResponse.Execute<GetBalanceRequestMessage, GetBalanceResponseMessage>(
                req,
                VersionedQueue(2));

            return ToModel(response?.BalanceInfo);
        }

        public Holding GetBalance(string exchange, string symbol, CachePolicy cachePolicy)
        {
            var req = new GetBalanceForCommodityAndExchangeRequestMessage { Exchange = exchange, Symbol = symbol, CachePolicy = ToContract(cachePolicy) };
            var response = RequestResponse.Execute<GetBalanceForCommodityAndExchangeRequestMessage, GetBalanceForCommodityAndExchangeResponseMessage>(
                req,
                VersionedQueue(2));

            return ToModel(response?.Balance);
        }

        // TODO: Return a client-model instead of the contract.
        // TODO: The rest of the app shouldn't have to reference the contracts assembly.
        public CommodityDetailsContract GetCommodityDetails(string symbol, CachePolicy cachePolicy)
        {
            if (string.IsNullOrWhiteSpace(symbol)) { throw new ArgumentNullException(nameof(symbol)); }

            var req = new GetCommodityDetailsRequestMessage
            {
                Symbol = symbol.Trim(),
                CachePolicy = (CachePolicyContract)cachePolicy
            };

            var response = RequestResponse.Execute<GetCommodityDetailsRequestMessage, GetCommodityDetailsResponseMessage>(req, VersionedQueue(2));

            return response?.Payload;
        }

        public List<CommodityWithExchangesContract> GetCommodities(CachePolicy cachePolicy)
        {
            try
            {
                var req = new GetCommoditiesRequestMessage { CachePolicy = ToContract(cachePolicy) };
                var response = RequestResponse.Execute<GetCommoditiesRequestMessage, GetCommoditiesResponseMessage>(req, VersionedQueue(2));

                return response?.Payload;
            }
            catch(Exception exception)
            {
                _log.Error($"ExchangeClient.GetCommodities() failed with cache policy {cachePolicy}.");
                _log.Error(exception);
                throw;
            }
        }

        public List<string> GetExchangesForCommodity(string symbol, CachePolicyContract cachePolicy)
        {
            var req = new GetExchangesForCommodityRequestMessage
            {
                Symbol = symbol,
                CachePolicy = cachePolicy
            };

            var response = RequestResponse.Execute<GetExchangesForCommodityRequestMessage, GetExchangesForCommodityResponseMessage>(req, VersionedQueue(2));

            return response?.Payload;
        }

        public List<HitBtcHealthStatusItemContract> GetHitBtcHealth(CachePolicyContract cachePolicy)
        {
            throw new NotImplementedException();

            //var req = new GetHitBtcHealthStatusRequestMessage
            //{
            //    CachePolicy = cachePolicy
            //};

            //var response = RequestResponse.Execute<GetHitBtcHealthStatusRequestMessage, GetHitBtcHealthStatusResponseMessage>(req, VersionedQueue(2));

            //return response?.Payload;
        }

        public List<OrderBookAndTradingPairContract> GetCachedOrderBooks(string exchange)
        {
            try
            {
                var req = new GetCachedOrderBooksRequestMessage
                {
                    Exchange = exchange
                };

                var response = RequestResponse.Execute<GetCachedOrderBooksRequestMessage, GetCachedOrderBooksResponseMessage>(req, VersionedQueue(2));

                return response?.Payload;
            }
            catch (Exception exception)
            {
                _log.Error($"Failed to retrieve cached order books for exchange \"{exchange}\"{Environment.NewLine}{exception.Message}");
                throw;
            }
        }

        private static TimeSpan PlaceOrderTimeout = TimeSpan.FromSeconds(30);

        public bool BuyLimit(string exchange, string symbol, string baseSymbol, QuantityAndPrice quantityAndPrice)
        {
            return BuyLimitV2(exchange, symbol, baseSymbol, quantityAndPrice)?.WasSuccessful ?? false;
        }

        public LimitOrderResult BuyLimitV2(string exchange, string symbol, string baseSymbol, QuantityAndPrice quantityAndPrice)
        {
            var req = new BuyLimitRequestMessage
            {
                Exchange = exchange,
                Symbol = symbol,
                BaseSymbol = baseSymbol,
                Quantity = quantityAndPrice.Quantity,
                Price = quantityAndPrice.Price
            };

            _log.Info($"ExchangeClient -- Buy Limit on {exchange} for {quantityAndPrice.Quantity} {symbol} at {quantityAndPrice.Price} {baseSymbol}. ");
            var response = RequestResponse.Execute<BuyLimitRequestMessage, BuyLimitResponseMessage>(
                req,
                VersionedQueue(2),
                PlaceOrderTimeout);

            return new LimitOrderResult
            {
                WasSuccessful = response?.WasSuccessful ?? false,
                FailureReason = response?.FailureReason,
                OrderId = response?.Payload?.OrderId,
                Price = response?.Payload?.Price,
                Quantity = response?.Payload?.Quantity,
                Executed = response?.Payload?.Executed
            };
        }

        public bool Withdraw(string source, string symbol, decimal quantity, DepositAddress address)
        {
            var request = new WithdrawCommodityRequestMessage
            {
                Payload = new WithdrawCommodityRequestMessage.RequestPayload
                {
                    SourceExchange = source,
                    Symbol = symbol,
                    Quantity = quantity,
                    DepositAddress = ToContract(address)
                }
            };

            var response = RequestResponse.Execute<WithdrawCommodityRequestMessage, WithdrawCommodityResponseMessage>(
                    request,
                    VersionedQueue(2));

            return true;
        }

        public List<OpenOrderForTradingPair> GetOpenOrders(string exchange, CachePolicy cachePolicy)
        {
            try
            {
                var req = new GetOpenOrdersRequestMessage
                {
                    Exchange = exchange,
                    CachePolicy = ToContract(cachePolicy)
                };

                var response = RequestResponse.Execute<GetOpenOrdersRequestMessage, GetOpenOrdersResponseMessage>(req, VersionedQueue(2));

                return ToModel(response?.Payload);
            }
            catch (Exception exception)
            {
                _log.Error($"Failed to retrieve {exchange} open orders with cache policy {cachePolicy}.{Environment.NewLine}{exception.Message}");
                throw;
            }
        }

        public List<OpenOrdersForTradingPair> GetOpenOrdersV2(string exchange)
        {
            var req = new GetOpenOrdersRequestMessageV2
            {
                Exchange = exchange
            };

            var response = RequestResponse.Execute<GetOpenOrdersRequestMessageV2, GetOpenOrdersResponseMessageV2>(req, VersionedQueue(2));

            return response?.Payload != null
                ? response?.Payload?.OpenOrdersForTradingPairs.Select(ToModel).ToList()
                : null;
        }

        public OpenOrdersWithAsOf GetOpenOrdersForTradingPairV2(string exchange, string symbol, string baseSymbol, CachePolicy cachePolicy)
        {
            try
            {
                var req = new GetOpenOrdersForTradingPairRequestMessageV2
                {
                    Exchange = exchange,
                    Symbol = symbol,
                    BaseSymbol = baseSymbol,
                    CachePolicy = ToContract(cachePolicy)
                };

                var response = RequestResponse.Execute<GetOpenOrdersForTradingPairRequestMessageV2, GetOpenOrdersForTradingPairResponseMessageV2>(req, VersionedQueue(2));

                return new OpenOrdersWithAsOf
                {
                    AsOfUtc = response?.Payload?.AsOfUtc,
                    OpenOrders = response?.Payload?.OpenOrders != null
                        ? response?.Payload?.OpenOrders.Select(ToModel).ToList()
                        : null
                };
            }
            catch (Exception exception)
            {
                _log.Error($"Failed to get open orders from {exchange} for {symbol}-{baseSymbol} with cache policy {cachePolicy}.{Environment.NewLine}{exception.Message}");
                throw;
            }
        }

        public bool SellMarket(string exchange, string symbol, string baseSymbol, decimal quantity)
        {
            throw new NotImplementedException();
        }

        public bool SellLimit(string exchange, string symbol, string baseSymbol, QuantityAndPrice quantityAndPrice)
        {
            return SellLimitV2(exchange, symbol, baseSymbol, quantityAndPrice)?.WasSuccessful ?? false;
        }

        public LimitOrderResult SellLimitV2(string exchange, string symbol, string baseSymbol, QuantityAndPrice quantityAndPrice)
        {
            var req = new SellLimitRequestMessage
            {
                Payload = new LimitRequestPayload
                {
                    Exchange = exchange,
                    Symbol = symbol,
                    BaseSymbol = baseSymbol,
                    Quantity = quantityAndPrice.Quantity,
                    Price = quantityAndPrice.Price
                }
            };

            _log.Info($"ExchangeClient -- Sell Limit on {exchange} for {quantityAndPrice.Quantity} {symbol} at {quantityAndPrice.Price} {baseSymbol}. ");
            var response = RequestResponse.Execute<SellLimitRequestMessage, SellLimitResponseMessage>(
                req,
                VersionedQueue(2),
                PlaceOrderTimeout);

            return new LimitOrderResult
            {
                WasSuccessful = response?.WasSuccessful ?? false,
                FailureReason = response?.FailureReason,
                OrderId = response?.Payload?.OrderId,
                Executed = response?.Payload?.Executed,
                Price = response?.Payload?.Price,
                Quantity = response?.Payload?.Quantity
            };
        }

        public List<OpenOrderForTradingPair> GetOpenOrders(string exchange, string symbol, string baseSymbol, CachePolicy cachePolicy)
        {
            try
            {
                var req = new GetOpenOrdersForTradingPairRequestMessage
                {
                    Exchange = exchange,
                    Symbol = symbol,
                    BaseSymbol = baseSymbol,
                    CachePolicy = ToContract(cachePolicy)
                };

                var response = RequestResponse.Execute<GetOpenOrdersForTradingPairRequestMessage, GetOpenOrdersForTradingPairResponseMessage>(req, VersionedQueue(2));

                return ToModel(response?.Payload);
            }
            catch
            {
                _log.Error($"Failed to get {exchange} open orders for {symbol}-{baseSymbol} with cache policy {cachePolicy}.");
                throw;
            }
        }

        public void CancelOrder(string exchange, string orderId)
        {
            var req = new CancelOrderRequestMessage
            {
                Exchange = exchange,
                OrderId = orderId
            };

            var response = RequestResponse.Execute<CancelOrderRequestMessage, CancelOrderResponseMessage>(req,
                VersionedQueue(2),
                PlaceOrderTimeout);
        }

        public void CancelOrder(string exchange, OpenOrder openOrder)
        {
            CancelOrder(exchange, openOrder.OrderId);
        }

        private OrderBook ToModel(OrderBookContract orderBook)
        {
            if (orderBook == null) { return null; }
            return new OrderBook
            {
                Asks = orderBook?.Asks != null ? orderBook.Asks.Select(queryOrder => ToModel(queryOrder)).ToList() : null,
                Bids = orderBook?.Bids != null ? orderBook.Bids.Select(queryOrder => ToModel(queryOrder)).ToList() : null,
                AsOf = orderBook?.AsOf
            };
        }

        private Order ToModel(OrderContract contract)
        {
            return contract != null
                ? new Order { Price = contract.Price, Quantity = contract.Quantity }
                : null;
        }

        private List<TradingPair> ToModel(List<TradingPairContract> contract)
        {
            return contract != null
                ? contract.Select(item => ToModel(item)).ToList()
                : null;
        }

        private TradingPair ToModel(TradingPairContract contract)
        {
            if (contract == null) { return null; }
            return new TradingPair
            {
                Symbol = contract.Symbol,
                BaseSymbol = contract.BaseSymbol,
                BaseCommodityName = contract.BaseCommodityName,
                CanonicalBaseCommodityId = contract.CanonicalBaseCommodityId,
                CanonicalCommodityId = contract.CanonicalCommodityId,
                CommodityName = contract.CommodityName,
                CustomBaseCommodityValues = contract.CustomBaseCommodityValues,
                CustomCommodityValues = contract.CustomCommodityValues,
                NativeBaseCommodityName = contract.NativeBaseCommodityName,
                NativeBaseSymbol = contract.NativeBaseSymbol,
                NativeCommodityName = contract.NativeCommodityName,
                NativeSymbol = contract.NativeSymbol,
                LotSize = contract.LotSize,
                PriceTick = contract.PriceTick,
                MinimumTradeBaseSymbolValue = contract.MinimumTradeBaseSymbolValue,
                MinimumTradeQuantity = contract.MinimumTradeQuantity
            };
        }

        private List<Exchange> ToModel(List<ExchangeContract> contract)
        {
            return contract != null
                ? contract.Select(item => ToModel(item)).ToList()
                : null;
        }

        private Exchange ToModel(ExchangeContract contract)
        {
            return contract != null
                ? new Exchange
                {
                    Id = new Guid(contract.Id),
                    Name = contract.Name,
                    IsRefreshable = contract.IsRefreshable,
                    IsWithdrawable = contract.IsWithdrawable,
                    CanBuyMarket = contract.CanBuyMarket,
                    CanSellMarket = contract.CanSellMarket,
                    HasOrderBooks = contract.HasOrderBooks
                }
                : null;
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

        private HoldingInfo ToModel(BalanceInfoContract contract)
        {
            return contract != null
                ? new HoldingInfo
                {
                    Holdings = contract.Balances != null
                        ? contract.Balances.Select(item => ToModel(item)).ToList()
                        : null,
                    TimeStampUtc = contract.AsOfUtc
                }
                : null;
        }

        private Holding ToModel(BalanceContract contract)
        {
            return contract != null
                ? new Holding
                {
                    Symbol = contract.Symbol,
                    Available = contract.Available,
                    AdditionalHoldings = contract.AdditionalBalances,
                    InOrders = contract.InOrders,
                    Total = contract.Total
                }
                : null;
        }

        private OpenOrderForTradingPair ToModel(OpenOrderForTradingPairContract contract)
        {
            return contract != null
                ? new OpenOrderForTradingPair
                {
                    OrderId = contract.OrderId,
                    BaseSymbol = contract.BaseSymbol,
                    OrderType = (OrderType)contract.OrderType,
                    Price = contract.Price,
                    Quantity = contract.Quantity,
                    Symbol = contract.Symbol
                }
                : null;
        }

        private List<OpenOrderForTradingPair> ToModel(List<OpenOrderForTradingPairContract> contract)
        {
            return contract != null
                ? contract.Select(item => ToModel(item)).ToList()
                : null;
        }

        public List<DepositAddressWithSymbol> GetDepositAddresses(string exchange, CachePolicy cachePolicy)
        {
            throw new NotImplementedException();
        }

        private List<HistoricalTrade> ToModel(List<HistoryItemContract> contract)
        {
            return contract != null
                ? contract.Select(item => ToModel(item)).ToList()
                : null;
        }

        private HistoricalTrade ToModel(HistoryItemContract contract)
        {
            return contract != null
                ? new HistoricalTrade
                {
                    NativeId = contract.NativeId,
                    Comments = contract.Comments,

                    TradingPair = (contract.TradeType != TradeTypeEnumContract.Deposit
                        && contract.TradeType != TradeTypeEnumContract.Withdraw)
                    ? new TradingPair { Symbol = contract.Symbol, BaseSymbol = contract.BaseSymbol }
                    : null,
                    
                    Symbol = contract.Symbol,

                    BaseSymbol = contract.BaseSymbol,

                    TimeStampUtc = contract.TimeStampUtc,

                    SuccessTimeStampUtc = contract.SuccessTimeStampUtc,

                    Price = contract.Price,

                    Quantity = contract.Quantity,

                    FeeQuantity = contract.FeeQuantity,

                    FeeCommodity = contract.FeeCommodity,

                    TradeType = (TradeTypeEnum)contract.TradeType,

                    TradeStatus = (TradeStatusEnum)contract.TradeStatus,

                    WalletAddress = contract.WalletAddress,

                    DestinationExchange = contract.DestinationExchange,

                    TransactionHash = contract.TransactionHash
                }
                : null;
        }

        private HistoricalTradeWithExchange ToModel(HistoryItemWithExchangeContract contract)
        {
            if (contract == null) { return null; }
            var baseModel = ToModel((HistoryItemContract)contract);
            var model = ReflectionUtil.CloneToType<HistoricalTrade, HistoricalTradeWithExchange>(baseModel);
            model.Exchange = contract.Exchange;

            return model;
        }

        private List<HistoricalTradeWithExchange> ToModel(List<HistoryItemWithExchangeContract> contract)
        {
            return contract != null
                ? contract.Select(item => ToModel(item)).ToList()
                : null;
        }

        private OpenOrdersForTradingPair ToModel(OpenOrdersForTradingPairContract contract)
        {
            return contract != null
                ? new OpenOrdersForTradingPair
                {
                    AsOfUtc = contract.AsOfUtc,
                    Symbol = contract.Symbol,
                    BaseSymbol = contract.BaseSymbol,
                    OpenOrders = contract.OpenOrders != null
                        ? contract.OpenOrders.Select(queryOpenOrder => ToModel(queryOpenOrder)).ToList()
                        : null
                }
                : null;
        }

        private OpenOrder ToModel(OpenOrderContract contract)
        {
            return contract != null
                ? new OpenOrder
                {
                    OrderId = contract.OrderId,
                    OrderType = (OrderType)contract.OrderType,
                    Price = contract.Price,
                    Quantity = contract.Quantity
                }
                : null;
        }

        private CommodityForExchange ToModel(ExchangeCommodityContract contract)
        {
            return contract != null
                ? new CommodityForExchange
                {
                    CanDeposit = contract.CanDeposit,
                    CanonicalId = contract.CanonicalId,
                    CanWithdraw = contract.CanWithdraw,
                    ContractAddress = contract.ContractAddress,
                    CustomValues = contract.CustomValues,
                    LotSize = contract.LotSize,
                    MinimumTradeQuantity = contract.MinimumTradeQuantity,
                    MinimumTradeBaseSymbolValue = contract.MinimumTradeBaseSymbolValue,
                    Name = contract.Name,
                    NativeName = contract.NativeName,
                    NativeSymbol = contract.NativeSymbol,
                    Symbol = contract.Symbol,
                    WithdrawalFee = contract.WithdrawalFee
                }
                : null;
        }

        private DepositAddressContract ToContract(DepositAddress model)
        {
            return model != null
                ? new DepositAddressContract
                {
                    DepositAddress = model.Address,
                    DepositMemo = model.Memo
                }
                : null;
        }

        public HistoryForTradingPairResult GetUserTradeHistoryForTradingPair(string exchange, string symbol, string baseSymbol, CachePolicy cachePolicy)
        {
            var req = new GetHistoryForTradingPairRequestMessage
            {
                Payload = new GetHistoryForTradingPairRequestMessage.RequestPayload
                {
                    Exchange = exchange,
                    Symbol = symbol,
                    BaseSymbol = baseSymbol,
                    CachePolicy = ToContract(cachePolicy)
                }
            };

            var response = RequestResponse.Execute<GetHistoryForTradingPairRequestMessage, GetHistoryForTradingPairResponseMessage>(
                            req,
                            VersionedQueue(2),
                            PlaceOrderTimeout);

            return new HistoryForTradingPairResult { };
        }
    }
}
