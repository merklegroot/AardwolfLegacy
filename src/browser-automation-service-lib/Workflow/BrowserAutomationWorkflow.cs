using OpenQA.Selenium.Chrome;
using System;
using wait_for_it_lib;

namespace browser_automation_service_lib.Workflow
{
    public interface IBrowserAutomationWorkflow
    {
        string GetHitBtcStatusPageContents();
    }

    public class BrowserAutomationWorkflow : IBrowserAutomationWorkflow
    {
        private readonly IWaitForIt _waitForIt;

        public BrowserAutomationWorkflow(IWaitForIt waitForIt)
        {
            _waitForIt = waitForIt;
        }

        public string GetHitBtcStatusPageContents()
        {
            string url = "https://hitbtc.com/system-health";

            var driver = new ChromeDriver();
            try
            {
                driver.Navigate().GoToUrl(url);

                var maxWaitTime = TimeSpan.FromSeconds(30);
                var interval = TimeSpan.FromMilliseconds(100);
                var waitForItResult = _waitForIt.Wait(() =>
                {
                    try
                    {
                        var title = driver.Title;
                        return title != null
                            && driver.Title.ToUpper()
                            .IndexOf("System Health Status".ToUpper()) != -1;

                    }
                    catch
                    {
                        return false;
                    }
                }, maxWaitTime, interval);

                if (waitForItResult)
                {
                    return driver.PageSource;
                }

                throw new ApplicationException("Never got System Health Status in the title.");
            }
            finally
            {
                driver.Close();
            }
        }
    }
}
