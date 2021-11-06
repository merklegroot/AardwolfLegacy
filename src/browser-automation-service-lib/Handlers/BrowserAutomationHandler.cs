using browser_automation_service_lib.Workflow;
using OpenQA.Selenium.Chrome;
using service_lib.Handlers;
using System;
using trade_contracts.Messages.Browser;
using wait_for_it_lib;

namespace browser_automation_service_lib.Handlers
{
    public interface IBrowserAutomationHandler :
        IRequestResponseHandler<NavigateAndGetContentsRequestMessage, NavigateAndGetContentsResponseMessage>,
        IRequestResponseHandler<GetHitBtcHealthStatusPageContentsRequestMessage, GetHitBtcHealthStatusPageContentsResponseMessage>,
        IHandler
    {
    }

    public class BrowserAutomationHandler : IBrowserAutomationHandler
    {
        private readonly IBrowserAutomationWorkflow _browserAutomationWorkflow;

        public BrowserAutomationHandler(IBrowserAutomationWorkflow browserAutomationWorkflow)
        {
            _browserAutomationWorkflow = browserAutomationWorkflow;
        }

        private const string HitBtcHealthStatusUrl = "https://hitbtc.com/system-health";

        private readonly IWaitForIt _waitForIt = new WaitForIt();

        public NavigateAndGetContentsResponseMessage Handle(NavigateAndGetContentsRequestMessage message)
        {
            if (message == null) { throw new ArgumentNullException(nameof(message)); }
            if (string.IsNullOrWhiteSpace(message?.Payload?.Url)) { throw new ArgumentNullException(nameof(message.Payload.Url)); }

            string contents = null;
            if (string.Equals(message.Payload.Url.Trim(), HitBtcHealthStatusUrl, StringComparison.InvariantCultureIgnoreCase))
            {
                contents = _browserAutomationWorkflow.GetHitBtcStatusPageContents();
            }
            else
            {
                var driver = new ChromeDriver();
                driver.Navigate().GoToUrl(message.Payload.Url);
                contents = driver.PageSource;

                driver.Close();
            }

            return new NavigateAndGetContentsResponseMessage
            {
                Payload = new NavigateAndGetContentsResponseMessage.ResponsePayload
                {
                    Contents = contents
                }
            };
        }

        public GetHitBtcHealthStatusPageContentsResponseMessage Handle(GetHitBtcHealthStatusPageContentsRequestMessage message)
        {
            var contents = _browserAutomationWorkflow.GetHitBtcStatusPageContents();
            return new GetHitBtcHealthStatusPageContentsResponseMessage
            {
                Payload = new GetHitBtcHealthStatusPageContentsResponseMessage.ResponsePayload
                {
                    Contents = contents
                }
            };
        }
    }
}
