using cache_lib.Models;
using coss_browser_service_client;
using coss_browser_workflow_lib;
using dump_lib;
using env_config_lib;
using exchange_client_lib;
using System;
using System.Collections.Generic;
using System.Linq;
using trade_res;

namespace coss_browser_service_test_con
{
    public interface ICossBrowserServiceTestConApp
    {
        void Run();
    }
    
    public class CossBrowserServiceTestConApp : ICossBrowserServiceTestConApp
    {
        private readonly ICossBrowserWorkflow _cossBrowserWorkflow;
        private readonly ICossBrowserClient _cossBrowserClient;
        private readonly IExchangeClient _exchangeClient;

        public CossBrowserServiceTestConApp(
            ICossBrowserWorkflow cossBrowserWorkflow,
            ICossBrowserClient cossBrowserClient,
            IExchangeClient exchangeClient)
        {
            _cossBrowserWorkflow = cossBrowserWorkflow;
            _cossBrowserClient = cossBrowserClient;
            _exchangeClient = exchangeClient;
        }

        private bool _keepRunning = true;

        public void Run()
        {
            ShowMenu();
            while (_keepRunning)
            {
                var key = Console.ReadKey(true);
                ProcessKey(key.KeyChar);
            }
        }

        private void ProcessKey(char key)
        {
            var menuItem = Menu.SingleOrDefault(queryMenuItem =>
                char.ToUpperInvariant(queryMenuItem.Key) == char.ToUpperInvariant(key));

            if (menuItem != null)
            {
                try
                {
                    menuItem.Method();
                }
                catch(Exception exception)
                {
                    Console.WriteLine(exception);
                }

                Console.WriteLine("");
                ShowMenu();
            }
        }

        private void ShowStatus()
        {
            var envText = _isProd ? "PROD" : "DEFAULT";
            Console.WriteLine($"Target Environment: {envText}");
            Console.WriteLine();
        }

        private void ShowMenu()
        {
            ShowStatus();

            foreach (var menuItem in Menu)
            {
                Console.WriteLine(menuItem.DisplayText);
            }

            Console.WriteLine();
        }

        private void OnPingExchangeServiceSelected()
        {
            Console.WriteLine("Pinging the exchange service...");
            var pingResult = _exchangeClient.Ping();
            pingResult.Dump();
        }

        private void OnGetCossEthBtcSelected()
        {
            Console.WriteLine("Requesting order book...");
            var result = _exchangeClient.GetOrderBook(ExchangeNameRes.Coss, "ETH", "BTC", CachePolicy.ForceRefresh);
            result.Dump();
        }

        private void OnGetOpenOrdersEthBtcSelected()
        {
            Console.WriteLine("Requesting Coss open orders - ETH-BTC (Force Refresh)...");
            var result = _exchangeClient.GetOpenOrders(ExchangeNameRes.Coss, "ETH", "BTC", CachePolicy.ForceRefresh);
            result.Dump();
        }

        private void OnGetCossBalanceSelected()
        {
            Console.WriteLine("Requesting coss balance...");
            var result = _exchangeClient.GetBalances(ExchangeNameRes.Coss, CachePolicy.ForceRefresh);
            result.Dump();
        }

        private void OnPingSelected()
        {
            Console.WriteLine("Sending ping...");
            var pingResult = _cossBrowserClient.Ping();
            pingResult.Dump();
        }

        private void OnDirectGetCookiesSelected()
        {
            Console.WriteLine("Reading cookies directly from Chrome's storage...");

            var cossCookies = _cossBrowserWorkflow.GetCossCookies();            
            Console.WriteLine($"Session Token: {cossCookies.SessionToken}");
            Console.WriteLine($"Xsrf Token: {cossCookies.XsrfToken}");
        }

        private void OnServiceGetCookiesSelected()
        {
            Console.WriteLine("Using the service to read the cookies...");

            var cossCookies = _cossBrowserClient.GetCookies();
            cossCookies.Dump();
        }

        private bool _isProd = false;

        private void OnSwitchEnvironmentSelected()
        {
            // Console.WriteLine("This feature is temporarily disabled.");
            // const string DefaultConfigKey = "TRADE_RABBIT_CONFIG_PROD";
            const string ProdConfigKey = "TRADE_RABBIT_CONFIG_PROD";
            if (_isProd)
            {
                _cossBrowserClient.UseDefaultConfigKey();
                _exchangeClient.UseDefaultConfigKey();
                // EnvironmentConfigRepo.UseDefaultEnvironment();
            }
            else
            {
                _cossBrowserClient.OverrideConfigKey(ProdConfigKey);
                _exchangeClient.OverrideConfigKey(ProdConfigKey);
                // EnvironmentConfigRepo.OverrideEnvironment(ProdConfigKey);
            }

            _isProd = !_isProd;

            Console.WriteLine("Changing environments...");
        }

        private List<MenuItem> Menu
        {
            get
            {
                return new List<MenuItem>
                {
                    new MenuItem("(P)ing Coss Browser Service", 'P', OnPingSelected),
                    new MenuItem("Ping (E)xchange Service", 'E', OnPingExchangeServiceSelected),
                    new MenuItem("Get (O)rder Book - Coss ETH-BTC (ForceRefresh)", 'O', OnGetCossEthBtcSelected),
                    new MenuItem("Get (B)alance - (ForceRefresh)", 'B', OnGetCossBalanceSelected),
                    new MenuItem("Get Ope(N) Orders - ETH-BTC (ForceRefresh)", 'N', OnGetOpenOrdersEthBtcSelected),
                    new MenuItem("(D)irect - Get cookies", 'D', OnDirectGetCookiesSelected),
                    new MenuItem("(S)ervice - Get cookies", 'S', OnServiceGetCookiesSelected),
                    new MenuItem("Switch en(V)ironment", 'V', OnSwitchEnvironmentSelected),
                    new MenuItem("e(X)it", 'X', OnExitSelected)
                };
            }
        }

        private void OnExitSelected()
        {
            _keepRunning = false;
        }

        private class MenuItem
        {
            public MenuItem() { }
            public MenuItem(string displayText, char key, Action method)
            {
                DisplayText = displayText;
                Key = key;
                Method = method;
            }

            public string DisplayText { get; set; }
            public char Key { get; set; }
            public Action Method { get; set; }
        }
    }
}
