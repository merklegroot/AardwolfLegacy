using binance_lib;
using bit_z_lib;
using bitz_data_lib;
using browser_lib;
using config_client_lib;
using console_lib;
using coss_data_lib;
using cryptocompare_lib;
using cryptopia_lib;
using env_config_lib;
using exchange_client_lib;
using hitbtc_lib;
using idex_client_lib;
using idex_data_lib;
using idex_integration_lib;
using kucoin_lib;
using livecoin_lib;
using log_lib;
using mongo_lib;
using qryptos_lib;
using rabbit_lib;
using System;
using System.Diagnostics;
using System.Threading;
using tidex_integration_library;
using trade_email_lib;
using trade_node_integration;
using wait_for_it_lib;
using web_util;
using yobit_lib;

namespace idex_agent_con
{
    class Program
    {
        private static readonly TimeSpan TimeBetweenIterations = TimeSpan.FromMinutes(2.5);

        static void Main(string[] args)
        {
            var waitForIt = new WaitForIt();
            var configClient = new ConfigClient();
            var log = new LogRepo();

            var exchangeClient = new ExchangeClient();
            var connString = configClient.GetConnectionString();
            var dbContext = new MongoDatabaseContext(connString, "idex");
            var webUtil = new WebUtil();

            var nodeUtil = new TradeNodeUtil(configClient, webUtil, log);
            var emailUtil = new TradeEmailUtil(webUtil);
            var envConfig = new EnvironmentConfigRepo();
            var rabbitConnectionFactory = new RabbitConnectionFactory(envConfig);

            var cryptoCompare = new CryptoCompareIntegration(webUtil, configClient);

            var idexOpenOrdersRepo = new IdexOpenOrdersRepo(configClient);
            var idexHoldingsRepo = new IdexHoldingsRepo(configClient);
            var idexHistoryRepo = new IdexHistoryRepo(configClient);
            var idexOrderBookRepo = new IdexOrderBookRepo(configClient);
            var idexClient = new IdexClient(webUtil);
            var idex = new IdexIntegration(webUtil, configClient, idexHoldingsRepo, idexOrderBookRepo, idexOpenOrdersRepo, idexHistoryRepo, idexClient, log);

            var Info = new Action<string>(message =>
            {
                ConsoleWrapper.WriteLine($"{DateTime.Now} (local) - {message}");
                log.Info(message);
            });


            var appFactory = new Func<App>(() =>
            {
                return new App(
                    cryptoCompare,
                    idexHistoryRepo,
                    idex,
                    waitForIt,
                    configClient,
                    new BrowserUtil(waitForIt),
                    idexOpenOrdersRepo,
                    idexOrderBookRepo,
                    exchangeClient,
                    log);
            });

            while (true)
            {
                Info("Starting a new iteration.");

                try
                {
                    using (var app = appFactory())
                    {
                        app.RunAutoOrder();
                        // app.CaptureFrames();
                    }

                    Info($"Sleeping for {TimeBetweenIterations.TotalMinutes} minutes");
                    Sleep(TimeBetweenIterations);                    
                }
                catch (Exception exception)
                {
                    ConsoleWrapper.WriteLine(exception);
                    log.Error(exception);
                }
            }
        }
        
        private static void Sleep(TimeSpan timeSpan)
        {
            var maxSleepTimeSpan = TimeSpan.FromMilliseconds(100);

            var stopWatch = new Stopwatch();
            stopWatch.Start();

            TimeSpan timeRemaining;
            while ((timeRemaining = timeSpan - stopWatch.Elapsed) > TimeSpan.Zero)
            {
                Thread.Sleep(timeRemaining >= maxSleepTimeSpan ? maxSleepTimeSpan : timeRemaining);
            }
        }

        private static void OnMessageReceived(string message)
        {
            //Console.WriteLine($"Received: {message}");
            //if (string.Equals(message, "update-funds", StringComparison.InvariantCultureIgnoreCase))
            //{
            //    Console.WriteLine("Executing the Update-Funds workflow.");
            //    Task.Run(() => UpdateBalancesWorkflow());
            //}
        }
    }
}
