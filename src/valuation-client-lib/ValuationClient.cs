using cache_lib.Models;
using client_lib;
using env_config_lib;
using iridium_lib;
using rabbit_lib;
using System;
using trade_constants;
using trade_contracts;
using trade_contracts.Messages.Valuation;

namespace valuation_client_lib
{
    public class ValuationClient : ServiceClient, IValuationClient
    {
        private static Func<IRequestResponse> RequestResponseFactory = new Func<IRequestResponse>(() =>
        {
            var envConfigRepo = new EnvironmentConfigRepo();
            var rabbitConnectionFactory = new RabbitConnectionFactory(envConfigRepo);
            return new RequestResponse(rabbitConnectionFactory);
        });

        public ValuationClient()
            : base(RequestResponseFactory())
        {
        }

        public ValuationClient(IRequestResponse requestResponse)
            : base(requestResponse)
        {
        }

        public decimal? GetUsdValue(string symbol, CachePolicy cachePolicy)
        {
            var req = new GetValuationRequestMessage
            {
                Symbol = symbol,
                CachePolicy = ToContract(cachePolicy)
            };

            var response = RequestResponse.Execute<GetValuationRequestMessage, GetValuationResponseMessage>(
                req,
                VersionedQueue(1));

            return response?.Payload?.UsdValue;
        }

        private string _overriddenQueue = null;
        public override void OverrideQueue(string queue)
        {
            _overriddenQueue = queue;
        }

        protected override string QueueName => TradeRabbitConstants.Queues.ValuationServiceQueue;
    }
}
