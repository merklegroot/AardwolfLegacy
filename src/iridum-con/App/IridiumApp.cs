using cache_lib.Models;
using config_client_lib;
using console_lib;
using dump_lib;
using exchange_client_lib;
using System;
using System.Collections.Generic;
using System.Linq;

namespace iridum_con.App
{
    public class IridiumApp : IIridiumApp
    {
        private const string ApplicationName = "Iridium Client Test Application";

        private class MenuItem
        {
            public MenuItem() { }
            public MenuItem(char key, string text, Action method)
            {
                Key = key;
                Text = text;
                Method = method;
            }

            public char Key { get; set; }
            public string Text { get; set; }
            public Action Method { get; set; }
        }

        private List<MenuItem> Menu => new List<MenuItem>
        {
            new MenuItem('C', "Get (C)onnection String", new Action(() => OnGetConnectionString())),
            new MenuItem('T', "Get CCX(T) Url", new Action(() => OnGetCcxtUrlSelected())),
            new MenuItem('E', "Get (E)xchanges", new Action(() => OnGetExchangesSelected())),
            new MenuItem('H', "Get (H)itBtc FYN Commodity", new Action(() => OnGetHitFynCommoditySelected())),
            new MenuItem('X', "e(X)it", new Action(() => OnExitAppSelected()))
        };

        private bool _keepRunning;

        private readonly IConfigClient _configClient;
        private readonly IExchangeClient _exchangeClient;

        public IridiumApp(
            IConfigClient configClient,
            IExchangeClient exchangeClient)
        {
            _configClient = configClient;
            _exchangeClient = exchangeClient;
        }

        public void Run()
        {
            ConsoleWrapper.WriteLine($"{ApplicationName}");

            ShowMenu();
            _keepRunning = true;
            while (_keepRunning)
            {
                var key = Console.ReadKey(true);
                ProcessKey(key.KeyChar);
            }
        }

        private void ProcessKey(char key)
        {
            foreach (var menuItem in Menu)
            {
                if (char.ToUpper(menuItem.Key) == char.ToUpper(key))
                {
                    menuItem.Method();
                    ShowMenu();

                    return;
                }
            }
        }

        private void ShowMenu()
        {
            ConsoleWrapper.WriteLine();
            foreach (var menuItem in Menu)
            {
                ConsoleWrapper.WriteLine(menuItem.Text);
            }

            ConsoleWrapper.WriteLine();
        }

        private void OnGetConnectionString()
        {
            ConsoleWrapper.WriteLine("Attempting to get connection string...");
            try
            {
                var connectionString = _configClient.GetConnectionString();
                ConsoleWrapper.WriteLine(connectionString);
            }
            catch (Exception exception)
            {
                ConsoleWrapper.WriteLine(exception);
            }
        }

        private void OnGetCcxtUrlSelected()
        {
            ConsoleWrapper.WriteLine("Attempting to get the Ccxt url...");
            try
            {
                var ccxtUrl = _configClient.GetCcxtUrl();
                ConsoleWrapper.WriteLine($"Ccxt Url: {ccxtUrl}");
            }
            catch (Exception exception)
            {
                ConsoleWrapper.WriteLine(exception);
            }
        }

        private void OnGetExchangesSelected()
        {
            ConsoleWrapper.WriteLine("Attempting to get exchanges...");
            try
            {
                var exchanges = _exchangeClient.GetExchanges();
                var exchangesText = string.Join(", ", exchanges.Select(queryExchange => queryExchange.Name));
                ConsoleWrapper.WriteLine(exchangesText);
            }
            catch (Exception exception)
            {
                ConsoleWrapper.WriteLine(exception);
            }
        }

        private void OnGetHitFynCommoditySelected()
        {
            var result = _exchangeClient.GetCommoditiyForExchange("hitbtc", "FYN", null, CachePolicy.AllowCache);
            result.Dump();
        }

        private void OnExitAppSelected()
        {
            _keepRunning = false;
        }
    }
}
