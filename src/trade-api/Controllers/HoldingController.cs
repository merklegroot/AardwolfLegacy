using balance_lib;
using binance_lib;
using bit_z_lib;
using cache_lib.Models;
using coss_lib;
using cryptocompare_lib;
using cryptopia_lib;
using hitbtc_lib;
using idex_integration_lib;
using integration_workflow_lib;
using client_lib;
using kraken_integration_lib;
using kucoin_lib;
using livecoin_lib;
using log_lib;
using mew_integration_lib;
using Newtonsoft.Json;
using qryptos_lib;
using rabbit_lib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using token_balance_lib;
using trade_api.ServiceModels;
using trade_constants;
using trade_contracts;
using trade_lib;
using trade_model;
using trade_res;
using yobit_lib;
using exchange_client_lib;

namespace trade_api.Controllers
{
    public class HoldingController : ApiController
    {
        private readonly ICossIntegration _cossIntegration;
        private readonly IBinanceIntegration _binanceIntegration;
        private readonly IHitBtcIntegration _hitBtcIntegration;
        private readonly IKrakenIntegration _krakenIntegration;
        private readonly IKucoinIntegration _kucoinIntegration;
        private readonly IBitzIntegration _bitzIntegration;
        private readonly ILivecoinIntegration _livecoinIntegration;
        private readonly IMewIntegration _mewIntegration;
        private readonly IQryptosIntegration _qryptosIntegration;

        private readonly ICryptopiaIntegration _cryptopiaIntegration;
        private readonly IIdexIntegration _idexIntegration;
        private readonly IYobitIntegration _yobitIntegration;

        private readonly ITransferFundsWorkflow _transferFundsWorkflow;
        private readonly ITokenBalanceIntegration _tokenBalanceIntegration;
        private readonly ICryptoCompareIntegration _cryptoCompareIntegration;

        private readonly IBalanceAggregator _balanceAggregator;
        private readonly IValuationWorkflow _valuationWorkflow;

        private readonly List<ITradeIntegration> _integrations;

        private readonly IRabbitConnectionFactory _rabbitConnectionFactory;
        private readonly IExchangeClient _exchangeClient;

        private readonly ILogRepo _log;

        public HoldingController(
            ICossIntegration cossIntegration,
            IBinanceIntegration binanceIntegration,
            IHitBtcIntegration hitBtcIntegration,
            IKrakenIntegration krakenIntegration,
            IKucoinIntegration kucoinIntegration,
            IBitzIntegration bitzIntegration,
            ILivecoinIntegration livecoinIntegration,
            IIdexIntegration idexIntegration,
            IMewIntegration mewIntegration,
            IYobitIntegration yobitIntegration,
            IQryptosIntegration qryptosIntegration,
        
            ICryptoCompareIntegration cryptoCompareIntegration,
            ICryptopiaIntegration cryptopiaIntegration,
            ITransferFundsWorkflow transferFundsWorkflow,
            IValuationWorkflow valuationWorkflow,
            ITokenBalanceIntegration tokenBalanceIntegration,
            IBalanceAggregator balanceAggregator,

            IExchangeClient exchangeClient,

            IRabbitConnectionFactory rabbitConnectionFactory,

            ILogRepo log)
        {
            _cossIntegration = cossIntegration;
            _binanceIntegration = binanceIntegration;
            _hitBtcIntegration = hitBtcIntegration;
            _krakenIntegration = krakenIntegration;
            _kucoinIntegration = kucoinIntegration;
            _bitzIntegration = bitzIntegration;
            _livecoinIntegration = livecoinIntegration;
            _cryptopiaIntegration = cryptopiaIntegration;
            _idexIntegration = idexIntegration;
            _mewIntegration = mewIntegration;
            _yobitIntegration = yobitIntegration;
            _qryptosIntegration = qryptosIntegration;

            _cryptoCompareIntegration = cryptoCompareIntegration;
            _transferFundsWorkflow = transferFundsWorkflow;
            _tokenBalanceIntegration = tokenBalanceIntegration;
            _balanceAggregator = balanceAggregator;
            _valuationWorkflow = valuationWorkflow;
            _cryptopiaIntegration = cryptopiaIntegration;

            _exchangeClient = exchangeClient;
            _rabbitConnectionFactory = rabbitConnectionFactory;

            _log = log;

            _integrations = new List<ITradeIntegration>
            {
                _cossIntegration,
                _binanceIntegration,
                _hitBtcIntegration,
                _kucoinIntegration,
                _bitzIntegration,
                _krakenIntegration,
                _livecoinIntegration,
                _idexIntegration,
                _mewIntegration,
                _cryptopiaIntegration,
                _qryptosIntegration,
                // _yobitIntegration
            };
        }

        [HttpPost]
        [Route("api/get-holding-exchanges")]
        public HttpResponseMessage GetHoldingExchanges()
        {
            try
            {
                var vm = _integrations.Select(item =>
                {
                    return new { Id = item.Id, Name = item.Name };
                }).ToList();
                return Request.CreateResponse(HttpStatusCode.OK, vm);
            }
            catch (Exception exception)
            {
                _log.Error(exception);
                throw;
            }
        }

        public class HoldingWithValueViewModel
        {
            public string Asset { get; set; }
            public string AccountType { get; set; }
            public string DispayName
            {
                get
                {
                    var accountTypeSection = !string.IsNullOrWhiteSpace(AccountType)
                        ? $"({AccountType})"
                        : string.Empty;
                    return $"{Asset ?? string.Empty} {accountTypeSection}".Trim();
                }
            }
            public decimal Available { get; set; }
            public decimal InOrders { get; set; }
            public decimal Total { get; set; }
            public decimal? Value { get; set; }
        }

        public class HoldingInfoViewModel
        {
            public string Exchange { get; set; }

            public DateTime? TimeStampUtc { get; set; }

            public List<HoldingWithValueViewModel> Holdings { get; set; }

            public decimal TotalValue { get; set; }
        }

        [HttpPost]
        [Route("api/get-holdings-for-exchange-without-valuation")]
        public HttpResponseMessage GetHoldingsForExchangeWithoutValuation(GetHoldingsForExchangeServiceModel serviceModel)
        {
            try
            {
                if (serviceModel == null) { throw new ArgumentNullException(nameof(serviceModel)); }

                var integration = _integrations.Single(item => item.Id == serviceModel.Id || 
                (!string.IsNullOrWhiteSpace(item.Name) && !string.IsNullOrWhiteSpace(serviceModel.Name) && string.Equals(item.Name.Trim(), serviceModel.Name.Trim(), StringComparison.InvariantCultureIgnoreCase)));
                var holdingInfo = integration.GetHoldings(serviceModel.ForceRefresh
                    ? CachePolicy.ForceRefresh
                    : CachePolicy.OnlyUseCache);

                if (holdingInfo.Holdings != null)
                {
                    holdingInfo.Holdings = holdingInfo.Holdings.OrderBy(item => item.Asset).ToList();
                }

                if (serviceModel.ForceRefresh)
                {
                    if (integration is IMewIntegration)
                    {
                        _mewIntegration.GetHoldings(CachePolicy.ForceRefresh);
                    }
                    else if (integration is IIdexIntegration)
                    {
                        ConnectionContainer.Connection.Publish(TradeRabbitConstants.Queues.IdexAgentQueue, TradeRabbitConstants.Messages.UpdateFunds, TimeSpan.FromMinutes(10));
                    }
                }

                var holdingVm = JsonConvert.DeserializeObject<HoldingInfoViewModel>(JsonConvert.SerializeObject(holdingInfo));

                return Request.CreateResponse(HttpStatusCode.OK, holdingVm);
            }
            catch (Exception exception)
            {
                _log.Error(exception);
                throw;
            }
        }

        [HttpPost]
        [Route("api/get-holdings-for-exchange")]
        public HttpResponseMessage GetHoldingsForExchange(GetHoldingsForExchangeServiceModel serviceModel)
        {
            return Request.CreateResponse(HttpStatusCode.OK, _balanceAggregator.GetHoldingsForExchange(serviceModel));
        }
        
        public class SellFundsServiceModel
        {
            public string Symbol { get; set; }
            public string BaseSymbol { get; set; }
            public decimal Quantity { get; set; }
            public string Exchange { get; set; }
        }

        [HttpPost]
        [Route("api/sell-funds")]
        public HttpResponseMessage SellFunds(SellFundsServiceModel serviceModel)
        {
            if (serviceModel == null) { throw new ArgumentNullException(nameof(serviceModel)); }

            if (string.IsNullOrWhiteSpace(serviceModel.Symbol)) { throw new ArgumentNullException(nameof(serviceModel.Symbol)); }
            if (string.IsNullOrWhiteSpace(serviceModel.BaseSymbol)) { throw new ArgumentNullException(nameof(serviceModel.BaseSymbol)); }
            if (string.IsNullOrWhiteSpace(serviceModel.Exchange)) { throw new ArgumentNullException(nameof(serviceModel.Exchange)); }
            if (serviceModel.Quantity <= 0) { throw new ArgumentOutOfRangeException(nameof(serviceModel.Quantity)); }

            var effectiveExchange = serviceModel.Exchange.Trim();
            var effectiveSymbol = serviceModel.Symbol.Trim();
            var effectiveBaseSymbol = serviceModel.BaseSymbol.Trim();

            if (string.Equals(effectiveSymbol, effectiveBaseSymbol, StringComparison.InvariantCultureIgnoreCase))
            {
                throw new ArgumentException($"{serviceModel.Symbol} must not be the same as {serviceModel.BaseSymbol}");
            }

            var tradingPair = new TradingPair(effectiveSymbol, effectiveBaseSymbol);

            
            var result = _exchangeClient.SellMarket(effectiveExchange, tradingPair.Symbol, tradingPair.BaseSymbol, serviceModel.Quantity);

            return Request.CreateResponse(HttpStatusCode.OK);
        }

        [HttpPost]
        [Route("api/refresh-deposit-addresses")]
        public HttpResponseMessage RefreshDepositAddresses(ExchangeServiceModel serviceModel)
        {
            if (serviceModel == null) { throw new ArgumentNullException(nameof(serviceModel)); }
            if (string.IsNullOrWhiteSpace(serviceModel.Name)) { throw new ArgumentNullException(nameof(serviceModel.Name)); }

            var effectiveExchange = serviceModel.Name.Trim();
            var exchange = _exchangeClient.GetExchange(effectiveExchange);

            if (exchange == null) { throw new ApplicationException($"Could not match integration by name \"{effectiveExchange}\""); }

            if (string.Equals(exchange.Name.Replace("-", string.Empty), ExchangeNameRes.Bitz.Replace("-", string.Empty), StringComparison.InvariantCultureIgnoreCase))
            {
                RefreshBitzDepositAddresses();
                return Request.CreateResponse(HttpStatusCode.OK, "Sent request to Bit-Z Browser Agent.");
            }

            if (string.Equals(exchange.Name, ExchangeNameRes.KuCoin, StringComparison.InvariantCultureIgnoreCase))
            {
                bool gotSlim = false;
                if (!_kucoinSlim.IsSet || !(gotSlim = _kucoinSlim.Wait(TimeSpan.FromSeconds(1))))
                {
                    return Request.CreateResponse(HttpStatusCode.OK, "Kucoin desposit addresses are already being refreshed.");
                }

                Task.Run(() =>
                {
                    try
                    {
                        RefreshKucoinDepositAddresses();
                    }
                    finally
                    {
                        _kucoinSlim.Set();
                    }
                });

                return Request.CreateResponse(HttpStatusCode.OK, "Refreshing KuCoin deposit addresses.");
            }

            if (exchange is IIdexIntegration)
            {
                _idexIntegration.GetDepositAddresses(CachePolicy.ForceRefresh);
                return Request.CreateResponse(HttpStatusCode.OK, "Refreshing...");
            }

            throw new NotImplementedException($"Not implemented for {exchange.Name}.");          
        }

        public class GetTokenBalanceServiceModel
        {
            public string Exchange { get; set; }
            public string Symbol { get; set; }
        }

        public class GetTokenBalanceViewModel
        {
            public string Exchange { get; set; }
            public string Symbol { get; set; }
            public decimal Balance { get; set; }
        }
        
        /// <summary>
        /// Gets the token balance of the deposit address.
        /// This is not the same as the balance on the exchange.
        /// </summary>
        [HttpPost]
        [Route("api/get-token-balance")]
        public HttpResponseMessage GetTokenBalance(GetTokenBalanceServiceModel serviceModel)
        {
            if (serviceModel == null) { throw new ArgumentNullException(nameof(serviceModel)); }
            if (string.IsNullOrWhiteSpace(serviceModel.Exchange)) { throw new ArgumentNullException(nameof(serviceModel.Exchange)); }
            if (string.IsNullOrWhiteSpace(serviceModel.Symbol)) { throw new ArgumentNullException(nameof(serviceModel.Symbol)); }

            var exchange = _exchangeClient.GetExchange(serviceModel.Exchange);
            if (exchange == null) { throw new ApplicationException("Failed to resolve integration."); }

            var commodities = _exchangeClient.GetCommoditiesForExchange(serviceModel.Exchange, CachePolicy.AllowCache);
            var commodity = commodities.SingleOrDefault(item => string.Equals(item.Symbol, serviceModel.Symbol.Trim(), StringComparison.InvariantCultureIgnoreCase));

            if (commodity == null)
            {
                throw new ApplicationException($"Failed to resolve symbol {serviceModel.Symbol} on exchange {exchange.Name}.");
            }

            if (!commodity.CanonicalId.HasValue || commodity.CanonicalId.Value == default(Guid))
            {
                throw new ApplicationException($"Symbol {serviceModel.Symbol} on exchange {exchange.Name} does not have a canonical id.");
            }

            var canon = CommodityRes.ById(commodity.CanonicalId.Value);
            if (canon == null)
            {
                throw new ApplicationException($"Failed to retrieve canon for Symbol {serviceModel.Symbol} on exchange {exchange.Name} with canonical id {commodity.CanonicalId.Value}.");
            }

            if (!canon.IsEthToken.HasValue)
            {
                throw new ApplicationException("The canon for Symbol {serviceModel.Symbol} on exchange {exchange.Name} with canonical id {commodity.CanonicalId.Value} does not specify whether or it it's an Eth Token.");
            }

            if (!canon.IsEthToken.Value)
            {
                throw new NotImplementedException("Only implemented for Eth tokens.");
            }

            if (string.IsNullOrWhiteSpace(canon.ContractId))
            {
                throw new ApplicationException("The canon for Symbol {serviceModel.Symbol} on exchange {exchange.Name} with canonical id {commodity.CanonicalId.Value} does not specify a contract.");
            }

            var depositAddress = _exchangeClient.GetDepositAddress(serviceModel.Exchange, serviceModel.Symbol, CachePolicy.AllowCache);
            if (depositAddress == null || string.IsNullOrWhiteSpace(depositAddress.Address)) { throw new ApplicationException($"Failed to retrieve deposit address for symbol {serviceModel.Symbol} in exchnage {exchange.Name}."); }
            
            var balance = _tokenBalanceIntegration.GetTokenBalance(depositAddress.Address.Trim(), canon.ContractId.Trim(), CachePolicy.AllowCache);

            var vm = new GetTokenBalanceViewModel
            {
                Exchange = serviceModel.Exchange,
                Symbol = serviceModel.Symbol,
                Balance = balance
            };

            return Request.CreateResponse(HttpStatusCode.OK, vm);
        }

        private static ManualResetEventSlim _kucoinSlim = new ManualResetEventSlim(true);

        private void RefreshBitzDepositAddresses()
        {
            var message = new UpdateFundsRequestMessage();
            using (var conn = _rabbitConnectionFactory.Connect())
            {
                conn.PublishContract(TradeRabbitConstants.Queues.BitzBrowserAgentQueue, message, TimeSpan.FromMinutes(10));
            }
        }
        
        private void RefreshKucoinDepositAddresses()
        {
            _kucoinIntegration.GetDepositAddresses(CachePolicy.ForceRefresh);
        }
    }
}
