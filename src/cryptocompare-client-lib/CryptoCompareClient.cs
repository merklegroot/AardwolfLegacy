using cache_lib.Models;
using client_lib;
using env_config_lib;
using rabbit_lib;
using System;
using System.Collections.Generic;
using trade_constants;
using trade_contracts.Messages.CryptoCompare;

namespace cryptocompare_client_lib
{
    public class CryptoCompareClient : ServiceClient, ICryptoCompareClient
    {
        private static Func<IRequestResponse> RequestResponseFactory = new Func<IRequestResponse>(() =>
        {
            var envConfigRepo = new EnvironmentConfigRepo();
            var rabbitConnectionFactory = new RabbitConnectionFactory(envConfigRepo);
            return new RequestResponse(rabbitConnectionFactory);
        });

        public CryptoCompareClient()
            : base(RequestResponseFactory())
        {
        }

        public CryptoCompareClient(IRequestResponse requestResponse)
            : base(requestResponse)
        {
        }

        public decimal? GetUsdValue(string symbol, CachePolicy cachePolicy)
        {
            var req = new GetUsdValueRequestMessage
            {
                Symbol = symbol,
                CachePolicy = ToContract(cachePolicy)
            };

            var response = RequestResponse.Execute<GetUsdValueRequestMessage, GetUsdValueResponseMessage>(
                req,
                VersionedQueue(1));

            return response?.Payload?.UsdValue;
        }

        public (decimal? UsdValue, DateTime? AsOfUtc) GetUsdValueV2(string symbol, CachePolicy cachePolicy)
        {
            var req = new GetUsdValueRequestMessage
            {
                Symbol = symbol,
                CachePolicy = ToContract(cachePolicy)
            };

            var response = RequestResponse.Execute<GetUsdValueRequestMessage, GetUsdValueResponseMessage>(
                req,
                VersionedQueue(1));

            return (response?.Payload?.UsdValue, response?.Payload?.AsOfUtc);
        }

        public Dictionary<string, decimal> GetPrices(string symbol, CachePolicy cachePolicy)
        {
            var req = new GetPricesRequestMessage
            {
                Symbol = symbol,
                CachePolicy = ToContract(cachePolicy)
            };

            var response = RequestResponse.Execute<GetPricesRequestMessage, GetPricesResponseMessage>(
                req,
                VersionedQueue(1));

            return response?.Payload;
        }

        protected override string QueueName => TradeRabbitConstants.Queues.CryptoCompareServiceQueue;
    }
}
