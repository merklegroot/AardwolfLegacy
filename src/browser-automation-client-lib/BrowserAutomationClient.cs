using client_lib;
using env_config_lib;
using log_lib;
using rabbit_lib;
using System;
using trade_constants;
using trade_contracts.Messages.Browser;

namespace browser_automation_client_lib
{
    public interface IBrowserAutomationClient : IServiceClient
    {
        string NavigateAndGetContents(string url);
        string GetHitBtcHealthStatusContents();
    }

    public class BrowserAutomationClient : ServiceClient, IBrowserAutomationClient
    {
        private const string RabbitOverrideKey = "TRADE_RABBIT_BROWSER_AUTOMATION";

        private readonly ILogRepo _log;

        protected override string QueueName => TradeRabbitConstants.Queues.BrowserAutomationServiceQueue;

        private static Func<IRequestResponse> RequestResponseFactory = new Func<IRequestResponse>(() =>
        {
            var envConfigRepo = new EnvironmentConfigRepo(RabbitOverrideKey);
            var rabbitConnectionFactory = new RabbitConnectionFactory(envConfigRepo);
            return new RequestResponse(rabbitConnectionFactory);
        });

        public BrowserAutomationClient(
            IRequestResponse requestResponse,
            ILogRepo log)
        : base(requestResponse)
        {
            _log = log;
        }

        public BrowserAutomationClient()
            : base(RequestResponseFactory())
        {
            _log = new LogRepo();
        }

        public string NavigateAndGetContents(string url)
        {
            var req = new NavigateAndGetContentsRequestMessage { Payload = new NavigateAndGetContentsRequestMessage.RequestPayload { Url = url } };
            var response = RequestResponse.Execute<NavigateAndGetContentsRequestMessage, NavigateAndGetContentsResponseMessage>(
                req,
                VersionedQueue(1));

            return response?.Payload?.Contents;
        }

        public string GetHitBtcHealthStatusContents()
        {
            var req = new GetHitBtcHealthStatusPageContentsRequestMessage();
            var response = RequestResponse.Execute<GetHitBtcHealthStatusPageContentsRequestMessage, GetHitBtcHealthStatusPageContentsResponseMessage>(
                req,
                VersionedQueue(1),
                TimeSpan.FromSeconds(30));

            return response?.Payload?.Contents;
        }
    }
}
