using cache_lib.Models;
using cryptocompare_lib;
using service_lib.Handlers;
using System;
using trade_contracts;
using trade_contracts.Messages.CryptoCompare;

namespace cryptocompare_service_lib.Handlers
{
    public interface ICryptoCompareHandler
        : IRequestResponseHandler<GetUsdValueRequestMessage, GetUsdValueResponseMessage>,
        IRequestResponseHandler<GetPricesRequestMessage, GetPricesResponseMessage>
    {
    }

    public class CryptoCompareHandler : ICryptoCompareHandler
    {
        private readonly ICryptoCompareIntegration _cryptoCompareIntegration;

        public CryptoCompareHandler(ICryptoCompareIntegration cryptoCompareIntegration)
        {
            _cryptoCompareIntegration = cryptoCompareIntegration;
        }

        public GetUsdValueResponseMessage Handle(GetUsdValueRequestMessage message)
        {
            if (message == null) { throw new ArgumentNullException(nameof(message)); }
            var result = _cryptoCompareIntegration.GetUsdValueV2(message.Symbol, ToModel(message.CachePolicy));
            
            return new GetUsdValueResponseMessage
            {
                Payload = new UsdValueResult
                {
                    UsdValue = result.UsdValue,
                    AsOfUtc = result.AsOfUtc
                }
            };
        }

        public GetPricesResponseMessage Handle(GetPricesRequestMessage message)
        {
            var result = _cryptoCompareIntegration.GetPrices(message.Symbol, ToModel(message.CachePolicy));

            return new GetPricesResponseMessage
            {
                Payload = result
            };
        }

        private CachePolicy ToModel(CachePolicyContract contract)
        {
            return (CachePolicy)contract;
        }
    }
}
