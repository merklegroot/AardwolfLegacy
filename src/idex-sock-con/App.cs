using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Remote;
using res_util_lib;
using System;
using System.Threading;

namespace idex_sock_con
{
    internal class App
    {
        private RemoteWebDriver _driver;

        public void Run()
        {
            _driver = new ChromeDriver();
            // _driver.Navigate().GoToUrl("about:blank");

            Thread.Sleep(TimeSpan.FromSeconds(2.5));
            ExecuteScript(Script);

            while (true)
            {
                Thread.Sleep(100);
            }
        }

        private string Script => ResUtil.Get("xhr.js", GetType().Assembly)
            .Replace("[CLIENT_MACHINE_NAME]", Environment.MachineName);

        private void ExecuteScript(string script)
        {
            ((IJavaScriptExecutor)_driver).ExecuteScript(script);
        }
    }
}
