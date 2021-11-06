using System;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium.Chrome;

namespace trade_browser_lib_tests
{
    [TestClass]
    public class SeleniumStuff
    {
        private ChromeDriver _driver;

        [TestInitialize]
        public void Setup()
        {
            _driver = new ChromeDriver();
        }

        [TestCleanup]
        public void Teardown()
        {
            if (_driver != null) { _driver.Dispose(); }
        }

        [TestMethod]
        public void Selenium__click_buy_toggle_test()
        {
            const string symbol = "coss";
            const string baseSymbol = "eth";

            NavigateToExchange(symbol, baseSymbol);

            for (var i = 0; i < 20; i++)
            {
                BuyToggle(symbol);
                Thread.Sleep(250);
                SellToggle(symbol);
                Thread.Sleep(250);
            }
        }

        private void NavigateToExchange(string symbol, string baseSymbol)
        {
            _driver.Navigate().GoToUrl($"https://exchange.coss.io/exchange/{symbol.Trim().ToLower()}-{baseSymbol.Trim().ToLower()}");
        }
        
        private bool BuyToggle(string symbol)
        {
            var query = "#mat-button-toggle-5 > label.mat-button-toggle-label > div.mat-button-toggle-label-content";
            var button = _driver.FindElementByCssSelector(query);
            if (button == null) { return false; }
            if (!string.Equals((button.Text?? string.Empty).Trim(), $"BUY {symbol.Trim().ToUpper()}", StringComparison.InvariantCultureIgnoreCase))
            {
                return false;
            }

            button.Click();

            return true;
        }

        private bool PerformBuy()
        {
            var query = "//div[6]/div/div/div/button";
            var button = _driver.FindElementByCssSelector(query);
            if (button == null) { return false; }
            if (!string.Equals((button.Text ?? string.Empty).Trim(), "SELL", StringComparison.InvariantCultureIgnoreCase))
            {
                return false;
            }

            button.Click();

            return true;
        }

        private bool SellToggle(string symbol)
        {
            var query = "#mat-button-toggle-6 > label.mat-button-toggle-label > div.mat-button-toggle-label-content";
            var button = _driver.FindElementByCssSelector(query);
            if (button == null) { return false; }
            if (!string.Equals((button.Text ?? string.Empty).Trim(), $"SELL {symbol.Trim().ToUpper()}", StringComparison.InvariantCultureIgnoreCase))
            {
                return false;
            }

            button.Click();

            return true;
        }
    }
}