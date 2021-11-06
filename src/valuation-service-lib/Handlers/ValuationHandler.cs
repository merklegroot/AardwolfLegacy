using cache_lib.Models;
using cryptocompare_lib;
using service_lib.Handlers;
using System;
using trade_contracts;
using trade_contracts.Messages.Valuation;

namespace valuation_service_lib.Handlers
{
    public interface IValuationHandler
        : IRequestResponseHandler<GetValuationRequestMessage, GetValuationResponseMessage>
    {
    }

    public class ValuationHandler : IValuationHandler
    {
        private readonly ICryptoCompareIntegration _cryptoCompareIntegration;

        public ValuationHandler(ICryptoCompareIntegration cryptoCompareIntegration)
        {
            _cryptoCompareIntegration = cryptoCompareIntegration;
        }

        public GetValuationResponseMessage Handle(GetValuationRequestMessage message)
        {
            if (message == null) { throw new ArgumentNullException(nameof(message)); }
            var usdValue = _cryptoCompareIntegration.GetUsdValue(message.Symbol, ToModel(message.CachePolicy));
            
            return new GetValuationResponseMessage
            {
                Payload = new ValuationResult
                {
                    UsdValue = usdValue
                }
            };
        }

        private CachePolicy ToModel(CachePolicyContract contract)
        {
            return (CachePolicy)contract;
        }
    }
}
