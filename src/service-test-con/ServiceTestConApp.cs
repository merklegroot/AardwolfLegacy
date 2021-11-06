using cache_lib.Models;
using dump_lib;
using System;
using System.Collections.Generic;
using System.Linq;
using trade_res;
using exchange_client_lib;
using env_config_lib;
using config_client_lib;
using workflow_client_lib;
using System.Diagnostics;

namespace service_test_con
{
    public interface IServiceTestConApp
    {
        void Run();
    }

    public class ServiceTestConApp : IServiceTestConApp
    {
        private readonly IEnvironmentConfigRepo _environmentConfigRepo;

        private readonly IConfigClient _configClient;        
        private readonly IExchangeClient _exchangeClient;
        private readonly IWorkflowClient _workflowClient;

        public ServiceTestConApp(
            IEnvironmentConfigRepo environmentConfigRepo,
            IConfigClient configClient,
            IExchangeClient exchangeClient,
            IWorkflowClient workflowClient)
        {
            _environmentConfigRepo = environmentConfigRepo;

            _configClient = configClient;
            _exchangeClient = exchangeClient;
            _workflowClient = workflowClient;            
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
                if (!string.IsNullOrWhiteSpace(menuItem.Desc))
                {
                    Console.WriteLine($"Starting {menuItem.Desc}");
                }

                var stopWatch = new Stopwatch();

                try
                {

                    stopWatch.Start();
                    menuItem.Method();

                    stopWatch.Stop();
                    Console.WriteLine($"Elapsed time: {stopWatch.Elapsed.ToString()}");
                }
                catch (Exception exception)
                {
                    Console.WriteLine(exception);
                }

                Console.WriteLine("");
                ShowMenu();
            }
        }

        private void ShowMenu()
        {
            foreach (var menuItem in Menu)
            {
                Console.WriteLine(menuItem.DisplayText);
            }

            Console.WriteLine();
        }

        private List<MenuItem> Menu
        {
            get
            {
                return new List<MenuItem>
                {
                    new MenuItem("Get (C)onfig", 'C', OnGetEnvConfigSelected),
                    new MenuItem("Get (E)xchanges", 'E', OnGetExchangesSelected),
                    new MenuItem("Get Coss (O)pen orders for ETH-BTC", 'O', OnOpenOrdersSelected),
                    new MenuItem("Get Coss (W)allet", 'W', OnGetCossWalletSelected),
                    new MenuItem("(1) Ping config service", '1', OnPingConfigServiceSelected),
                    new MenuItem("(2) Ping exchange service", '2', OnPingExchangeServiceSelected),
                    new MenuItem("(3) Ping workflow service", '3', OnPingWorkflowServiceSelected),

                    new MenuItem("e(X)it", 'X', OnExitSelected)
                };
            }
        }

        private class MenuItem
        {
            public MenuItem() { }
            public MenuItem(string displayText, char key, Action method, string desc = null)
            {
                DisplayText = displayText;
                Key = key;
                Method = method;
                Desc = desc;
            }

            public string DisplayText { get; set; }
            public char Key { get; set; }
            public Action Method { get; set; }
            public string Desc { get; set; }
        }

        private void OnExitSelected()
        {
            _keepRunning = false;
        }

        private void OnGetExchangesSelected()
        {
            var results = _exchangeClient.GetExchanges();
            results.Dump();
        }

        private void OnOpenOrdersSelected()
        {
            const string Symbol = "ETH";
            const string BaseSymbol = "BTC";
            const CachePolicy CachePolicy = CachePolicy.ForceRefresh;
            Console.WriteLine($"Getting Coss open orders for {Symbol}-{BaseSymbol}...");
            var results = _exchangeClient.GetOpenOrders(ExchangeNameRes.Coss, Symbol, BaseSymbol, CachePolicy);

            results.Dump();
        }

        private void OnGetEnvConfigSelected()
        {
            var config = _environmentConfigRepo.GetRabbitClientConfig();
            config.Dump();
        }

        private void OnPingConfigServiceSelected()
        {
            var result = _configClient.Ping();
            result.Dump();
        }

        private void OnPingExchangeServiceSelected()
        {
            var result = _exchangeClient.Ping();
            result.Dump();
        }

        private void OnPingWorkflowServiceSelected()
        {
            var result = _workflowClient.Ping();
            result.Dump();
        }

        private void OnGetCossWalletSelected()
        {
            var result = _exchangeClient.GetBalances(ExchangeNameRes.Coss, CachePolicy.ForceRefresh);
            result.Dump();
        }
    }
}
