using binance_lib;
using bit_z_lib;
using coss_lib;
using cryptopia_lib;
using hitbtc_lib;
using idex_integration_lib;
using kraken_integration_lib;
using kucoin_lib;
using livecoin_lib;
using log_lib;
using mew_integration_lib;
using Newtonsoft.Json;
using qryptos_lib;
using System;
using System.Collections.Generic;
using System.Linq;
using trade_lib;
using yobit_lib;
using trade_constants;
using rabbit_lib;
using integration_workflow_lib;
using cache_lib.Models;
using exchange_client_lib;

namespace balance_lib
{
    public class BalanceAggregator : IBalanceAggregator
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
        
        private readonly List<ITradeIntegration> _integrations;

        private readonly IRabbitConnectionFactory _rabbitConnectionFactory;

        private readonly IValuationWorkflow _valuationWorkflow;
        private readonly IExchangeClient _exchangeClient;

        private readonly ILogRepo _log;

        public BalanceAggregator(
            IRabbitConnectionFactory rabbitConnectionFactory,
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

            ICryptopiaIntegration cryptopiaIntegration,

            IValuationWorkflow valuationWorkflow,
            IExchangeClient exchangeClient,

            ILogRepo log)
        {
            _rabbitConnectionFactory = rabbitConnectionFactory;
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

            _cryptopiaIntegration = cryptopiaIntegration;
            
            _valuationWorkflow = valuationWorkflow;
            _exchangeClient = exchangeClient;

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
                //_yobitIntegration
            };
        }

        public HoldingInfoViewModel GetHoldingsForExchange(GetHoldingsForExchangeServiceModel serviceModel)
        {
            try
            {
                if (serviceModel == null) { throw new ArgumentNullException(nameof(serviceModel)); }

                var integration = _integrations.Single(item => item.Id == serviceModel.Id ||
                    (!string.IsNullOrWhiteSpace(item.Name) && !string.IsNullOrWhiteSpace(serviceModel.Name) && string.Equals(item.Name.Trim(), serviceModel.Name.Trim(), StringComparison.InvariantCultureIgnoreCase)));

                if (integration == null) { throw new ApplicationException($"Could not find a matching integration. serviceModel: {JsonConvert.SerializeObject(serviceModel, Formatting.Indented)}."); }

                if (serviceModel.ForceRefresh)
                {
                    if (integration is IMewIntegration)
                    {
                        using (var conn = _rabbitConnectionFactory.Connect())
                        {
                            conn.Publish(TradeRabbitConstants.Queues.EtherscanAgentQueue, TradeRabbitConstants.Messages.UpdateFunds);
                        }
                    }
                    else if (integration is IIdexIntegration)
                    {
                        using (var conn = _rabbitConnectionFactory.Connect())
                        {
                            conn.Publish(TradeRabbitConstants.Queues.IdexAgentQueue, TradeRabbitConstants.Messages.UpdateFunds);
                        }
                    }
                }

                var effectiveCachePolicy = serviceModel.ForceRefresh ? CachePolicy.ForceRefresh : CachePolicy.OnlyUseCacheUnlessEmpty;
                var holdings = _exchangeClient.GetBalances(integration.Name, effectiveCachePolicy);

                if (holdings == null)
                {
                    throw new ApplicationException($"{integration.Name} returned a null holdings object with cache policy {effectiveCachePolicy}.");
                }

                var valuationDictionary = _valuationWorkflow.GetValuationDictionary();

                var holdingVm = JsonConvert.DeserializeObject<HoldingInfoViewModel>(JsonConvert.SerializeObject(holdings));

                if (valuationDictionary != null)
                {
                    decimal totalValue = 0;
                    foreach (var holding in holdingVm.Holdings ?? new List<HoldingWithValueViewModel>())
                    {
                        if (holding == null) { continue; }

                        if (string.IsNullOrWhiteSpace(holding.Asset)) { continue; }
                        if (!valuationDictionary.ContainsKey(holding.Asset)) { continue; }
                        var rate = valuationDictionary[holding.Asset];
                        holding.Value = rate * holding.Total;
                        totalValue += (holding.Value ?? 0);
                    }

                    holdingVm.TotalValue = totalValue;
                }

                return holdingVm;
            }
            catch (Exception exception)
            {
                _log.Error(exception);
                throw;
            }
        }

        private ITradeIntegration GetIntegrationFromName(string name)
        {
            var integration = _integrations.SingleOrDefault(item => string.Equals(item.Name, name, StringComparison.InvariantCultureIgnoreCase));
            if(integration == null) { throw new ApplicationException($"Failed to resolve integraiton by name \"{name}\""); }
            return integration;
        }
    }
}
