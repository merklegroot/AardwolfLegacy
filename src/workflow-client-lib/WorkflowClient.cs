using cache_lib.Models;
using client_lib;
using env_config_lib;
using rabbit_lib;
using System;
using System.Collections.Generic;
using trade_constants;
using trade_contracts;
using trade_contracts.Messages.CryptoCompare;
using trade_contracts.Messages.Exchange;
using trade_contracts.Messages.Valuation;
using trade_model;

namespace workflow_client_lib
{
    public class WorkflowClient : ServiceClient, IWorkflowClient
    {
        private static Func<IRequestResponse> RequestResponseFactory = new Func<IRequestResponse>(() =>
        {
            var envConfigRepo = new EnvironmentConfigRepo();
            var rabbitConnectionFactory = new RabbitConnectionFactory(envConfigRepo);
            return new RequestResponse(rabbitConnectionFactory);
        });

        public WorkflowClient()
            : base(RequestResponseFactory())
        {
        }

        public WorkflowClient(IRequestResponse requestResponse)
            : base(requestResponse)
        {
        }

        public ArbitrageResult GetArb(string exchangeA, string exchangeB, string symbol, CachePolicy cachePolicy)
        {
            var req = new GetArbRequestMessage { ExchangeA = exchangeA, ExchangeB = exchangeB, Symbol = symbol, CachePolicy = ToContract(cachePolicy) };
            var response = RequestResponse.Execute<GetArbRequestMessage, GetArbResponseMessage>(req, VersionedQueue(1));

            return ToModel(response?.Result);
        }

        public decimal? GetUsdValue(string symbol, CachePolicy cachePolicy)
        {
            var req = new GetUsdValueRequestMessage { Symbol = symbol, CachePolicy = ToContract(cachePolicy) };
            var response = RequestResponse.Execute<GetUsdValueRequestMessage, GetUsdValueResponseMessage>(req, VersionedQueue(1));

            return response?.Payload?.UsdValue;
        }

        private ArbitrageResult ToModel(ArbitrageResultContract contract)
        {
            return contract != null
                ? new ArbitrageResult
                {
                    BtcPrice = contract.BtcPrice,
                    BtcQuantity = contract.BtcQuantity,
                    EthPrice = contract.EthPrice,
                    EthQuantity = contract.EthQuantity,
                    ExpectedUsdCost = contract.ExpectedUsdCost,
                    ExpectedUsdProfit = contract.ExpectedUsdProfit
                }
                : null;
        }

        public (decimal? UsdValue, DateTime? AsOfUtc) GetUsdValueV2(string symbol, CachePolicy cachePolicy)
        {
            var req = new GetUsdValueRequestMessage { Symbol = symbol, CachePolicy = ToContract(cachePolicy) };
            var response = RequestResponse.Execute<GetUsdValueRequestMessage, GetUsdValueResponseMessage>(req, VersionedQueue(1));

            return (response?.Payload?.UsdValue, response?.Payload?.AsOfUtc);
        }

        public Dictionary<string, decimal> GetValuationDictionary(CachePolicy cachePolicy)
        {
            var req = new GetValuationDictionaryRequestMessage
            {
                Payload = new GetValuationDictionaryRequestMessage.RequestPayload
                {
                    CachePolicy = (CachePolicyContract)cachePolicy
                }
            };


            var response = RequestResponse.Execute<GetValuationDictionaryRequestMessage, GetValuationDictionaryResponseMessage>(req, VersionedQueue(1));

            return response?.Payload?.ValuationDictionary;
        }

        protected override string QueueName => TradeRabbitConstants.Queues.WorkflowServiceQueue;
    }
}
