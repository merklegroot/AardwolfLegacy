using cache_lib.Models;
using integration_workflow_lib;
using log_lib;
using service_lib.Handlers;
using System;
using trade_contracts;
using trade_contracts.Messages.CryptoCompare;
using trade_contracts.Messages.Exchange;
using trade_contracts.Messages.Valuation;
using trade_model;

namespace workflow_service_lib.Handlers
{
    public interface IWorkflowHandler : IHandler,
        IRequestResponseHandler<GetArbRequestMessage, GetArbResponseMessage>,
        IRequestResponseHandler<GetUsdValueRequestMessage, GetUsdValueResponseMessage>,
        IRequestResponseHandler<GetValuationDictionaryRequestMessage, GetValuationDictionaryResponseMessage>
    {
        void SetSynchronousWorkflow(bool shouldEnable);
    }

    public class WorkflowHandler : IWorkflowHandler
    {
        private readonly IArbitrageWorkflow _arbitrageWorkflow;
        private readonly IValuationWorkflow _valuationWorkflow;
        private readonly ILogRepo _log;

        public WorkflowHandler(
            IArbitrageWorkflow arbitrageWorkflow,
            IValuationWorkflow valuationWorkflow,
            ILogRepo log)
        {
            _arbitrageWorkflow = arbitrageWorkflow;
            _valuationWorkflow = valuationWorkflow;
            _log = log;
        }

        public void SetSynchronousWorkflow(bool shouldEnable)
        {
            _arbitrageWorkflow.SetSynchronousWorkflow(shouldEnable);
        }

        public GetArbResponseMessage Handle(GetArbRequestMessage message)
        {
            try
            {
                if (message == null) { throw new ArgumentNullException(nameof(message)); }
                var result = _arbitrageWorkflow.Execute(message.ExchangeA, message.ExchangeB, message.Symbol, ToModel(message.CachePolicy));

                return new GetArbResponseMessage
                {
                    Result = ToContract(result)
                };
            }
            catch
            {
                _log.Error($"Handle(GetArbRequestMessage) failed for ExchangeA: {message?.ExchangeA}, ExchangeB: {message?.ExchangeB}, Symbol: {message?.Symbol}, CachePolicy: {message?.CachePolicy}");
                throw;
            }
        }

        public GetUsdValueResponseMessage Handle(GetUsdValueRequestMessage message)
        {
            var result = _valuationWorkflow.GetUsdValueV2(message.Symbol, ToModel(message.CachePolicy));

            return new GetUsdValueResponseMessage
            {
                Payload = new UsdValueResult
                {
                    UsdValue = result.Data,
                    AsOfUtc = result.AsOfUtc
                }
            };
        }

        private ArbitrageResultContract ToContract(ArbitrageResult model)
        {
            return model != null
                ? new ArbitrageResultContract
                {
                    BtcNeeded = model.BtcNeeded,
                    BtcPrice = model.BtcPrice,
                    BtcQuantity = model.BtcQuantity,
                    EthNeeded = model.EthNeeded,
                    EthPrice = model.EthPrice,
                    EthQuantity = model.EthQuantity,
                    ExpectedProfitRatio = model.ExpectedProfitRatio,
                    ExpectedUsdCost = model.ExpectedUsdCost,
                    ExpectedUsdProfit = model.ExpectedUsdProfit,
                    TotalQuantity = model.TotalQuantity
                }
                : null;
        }

        private CachePolicy ToModel(CachePolicyContract contract)
        {
            return (CachePolicy)contract;
        }

        public GetValuationDictionaryResponseMessage Handle(GetValuationDictionaryRequestMessage message)
        {
            return new GetValuationDictionaryResponseMessage
            {
                Payload = new GetValuationDictionaryResponseMessage.ResponsePayload
                {
                    ValuationDictionary = _valuationWorkflow.GetValuationDictionary((CachePolicy)message.Payload.CachePolicy)
                }
            };
        }
    }
}
