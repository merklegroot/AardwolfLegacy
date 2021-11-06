using client_lib;
using coss_browser_client_lib;
using env_config_lib;
using rabbit_lib;
using System;
using trade_constants;
using trade_contracts.Messages.Browser;

namespace coss_browser_service_client
{
    public class CossBrowserClient : ServiceClient, ICossBrowserClient
    {
        private const string RabbitOverrideKey = "TRADE_RABBIT_COSS_BROWSER";

        private static Func<IRequestResponse> RequestResponseFactory = new Func<IRequestResponse>(() =>
        {
            var envConfigRepo = new EnvironmentConfigRepo(RabbitOverrideKey);
            var rabbitConnectionFactory = new RabbitConnectionFactory(envConfigRepo);

            return new RequestResponse(rabbitConnectionFactory);
        });

        public CossBrowserClient()
            : base(RequestResponseFactory())
        {
        }

        protected override string QueueName => TradeRabbitConstants.Queues.CossBrowserServiceQueue;

        public CossCookieContainer GetCookies()
        {
            var req = new GetCossCookiesRequestMessage();

            var response = RequestResponse.Execute<GetCossCookiesRequestMessage, GetCossCookiesResponseMessage>(
                req,
                VersionedQueue(1));

            return response?.Payload != null
                ? new CossCookieContainer
                {
                    TimeStampUtc = response?.Payload?.TimeStampUtc,
                    SessionToken = response?.Payload?.SessionToken,
                    XsrfToken = response?.Payload?.XsrfToken
                }
                : null;
        }
    }
}
