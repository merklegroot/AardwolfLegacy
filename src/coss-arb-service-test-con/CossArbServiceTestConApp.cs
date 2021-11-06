using cache_lib.Models;
using dump_lib;
using client_lib;
using System;
using System.Collections.Generic;
using System.Linq;
using trade_res;
using exchange_client_lib;

namespace coss_arb_service_test_con
{
    public interface ICossArbServiceTestConApp
    {
        void Run();
    }

    public class CossArbServiceTestConApp : ICossArbServiceTestConApp
    {
        private readonly IExchangeClient _exchangeClient;

        public CossArbServiceTestConApp(IExchangeClient exchangeClient)
        {
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
                    new MenuItem("(O)pen orders for ETH-BTC", 'O', OnOpenOrdersSelected),
                    new MenuItem("e(X)it", 'X', OnExitSelected)
                };
            }
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

        private void OnExitSelected()
        {
            _keepRunning = false;
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
    }
}
