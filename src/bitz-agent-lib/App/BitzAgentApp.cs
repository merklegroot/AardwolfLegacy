using binance_lib;
using bit_z_lib;
using cache_lib.Models;
using config_client_lib;
using console_lib;
using log_lib;
using mongo_lib;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using tfa_lib;
using trade_browser_lib;
using trade_model;
using trade_strategy_lib;
using wait_for_it_lib;

namespace bitz_agent_lib.App
{
    public class BitzAgentApp : IBitzAgentApp
    {
        private string ApplicationName = "Bit-Z Agent Console";

        private readonly IBitzIntegration _bitzIntegration;
        private readonly IBitzAgentDriver _bitzAgentDriver;
        private readonly IBinanceIntegration _binanceIntegration;
        private readonly AutoEthBtc _autoEthBtc;
        private readonly IConfigClient _configClient;
        private readonly ITfaUtil _tfaUtil;
        private readonly IWaitForIt _waitForIt;
        private readonly ILogRepo _log;
        private readonly IMongoDatabaseContext _dbContext;

        private const decimal BitzMinimumEth = 0.050m;
        private const decimal BitzMinimumBTC = 0.010m; // <-- this is a guess. need to look up the real value.

        public BitzAgentApp(
            IBitzIntegration bitzIntegration,
            IBitzAgentDriver bitzAgentDriver,
            IBinanceIntegration binanceIntegration,
            IConfigClient configClient,
            ITfaUtil tfaUtil,
            IWaitForIt waitForIt,
            ILogRepo log)
        {
            _bitzIntegration = bitzIntegration;
            _bitzAgentDriver = bitzAgentDriver;
            _binanceIntegration = binanceIntegration;
            _configClient = configClient;
            _tfaUtil = tfaUtil;
            _waitForIt = waitForIt;
            _log = log;
            _dbContext = new MongoDatabaseContext(_configClient.GetConnectionString(), "bitz");

            _autoEthBtc = new AutoEthBtc();
        }

        public void Run()
        {
            Console.WriteLine($"{ApplicationName} - Starting.");

            while (true)
            {
                Console.WriteLine("Beginning iteration.");
                try
                {
                    _bitzAgentDriver.AutoOpenOrder();
                }
                catch (Exception exception)
                {
                    _log.Error(exception);
                }
                
                var sleepAmount = TimeSpan.FromMinutes(2.5);
                ConsoleWrapper.WriteLine($"Iteration complete. Sleeping for {sleepAmount}");
                Thread.Sleep(sleepAmount);
            }
        }

        private void AutoBuy(TradingPair tradingPair)
        {
            var config = _configClient.GetBitzAgentConfig();
            if (config == null || !config.IsAutoTradingEnabled || config.TokenThreshold <= 0) { return; }
            var threshold = config.TokenThreshold;

            Console.WriteLine($"Checking {tradingPair} for auto-buy.");

            var bitzOrderBookTask = Task.Run(() => _bitzIntegration.GetOrderBook(tradingPair, CachePolicy.ForceRefresh));
            var bitzOrderBook = bitzOrderBookTask.Result;

            var binanceOrderBookTask = Task.Run(() => _binanceIntegration.GetOrderBook(tradingPair, CachePolicy.ForceRefresh));
            var binanceOrderBook = binanceOrderBookTask.Result;
            var binanceBestBid = binanceOrderBook.BestBid();
            var binanceBestBidPrice = binanceBestBid.Price;

            var minimumTrade = GetMinimumTrade(tradingPair.BaseSymbol);

            var autoBuyResult = new AutoBuy(_log).Execute(bitzOrderBook.Asks, binanceBestBidPrice, minimumTrade, threshold, tradingPair);

            if (autoBuyResult.Quantity <= 0)
            {
                _log.Info($"Bit-Z -- Nothing to buy for {tradingPair}");
                return;
            }

            _log.Info($"Bit-Z - About to buy {autoBuyResult.Quantity} of {tradingPair} at rate {autoBuyResult.Price}");
            try
            {
                var buyLimitResult = _bitzIntegration.BuyLimit(tradingPair, autoBuyResult.Quantity, autoBuyResult.Price);
            }
            catch (Exception exception)
            {
                var failureText = new StringBuilder()
                    .AppendLine($"Bit-Z - Failed to buy {autoBuyResult.Quantity} of {tradingPair} at rate {autoBuyResult.Price}.")
                    .ToString();
                _log.Error(failureText);
                _log.Error(exception);
            }

            _bitzIntegration.CancelAllOpenOrdersForTradingPair(tradingPair);
        }

        private void AutoSell(TradingPair tradingPair)
        {
            var config = _configClient.GetBitzAgentConfig();
            if (config == null || !config.IsAutoTradingEnabled) { return; }

            Console.WriteLine($"Checking {tradingPair} for auto-sell.");

            var bitzHoldings = _bitzIntegration.GetHoldings(CachePolicy.ForceRefresh);

            var holding = bitzHoldings.Holdings.SingleOrDefault(item => string.Equals(item.Asset, tradingPair.Symbol, StringComparison.InvariantCultureIgnoreCase));
            if (holding == null || holding.Available <= 0) { return; }

            var bitzOrderBookTask = Task.Run(() => _bitzIntegration.GetOrderBook(tradingPair, CachePolicy.ForceRefresh));
            
            var binanceOrderBookTask = Task.Run(() => _binanceIntegration.GetOrderBook(tradingPair, CachePolicy.ForceRefresh));
            var bitzOrderBook = bitzOrderBookTask.Result;
            var binanceOrderBook = binanceOrderBookTask.Result;
            var binanceBestBid = binanceOrderBook.BestBid();
            var binanceBestBidPrice = binanceBestBid.Price;

            var minimumTrade = GetMinimumTrade(tradingPair.BaseSymbol);
            
            var autoSellResult = new AutoSellStrategy().Execute(holding.Available, bitzOrderBook, binanceOrderBook, minimumTrade);
            if (autoSellResult.Quantity <= 0)
            {
                _log.Info($"Bit-Z -- Nothing to sell for {tradingPair}");
                return;
            }

            _log.Info($"Bit-Z - About to sell {autoSellResult.Quantity} of {tradingPair} at rate {autoSellResult.Price}");
            try
            {
                var sellLimitResult = _bitzIntegration.SellLimit(tradingPair, autoSellResult.Quantity, autoSellResult.Price);
            }
            catch (Exception exception)
            {
                var failureText = new StringBuilder()
                    .AppendLine($"Bit-Z - Failed to sell {autoSellResult.Quantity} of {tradingPair} at rate {autoSellResult.Price}.")
                    .ToString();
                _log.Error(failureText);
                _log.Error(exception);
            }

            _bitzIntegration.CancelAllOpenOrdersForTradingPair(tradingPair);
        }

        private decimal GetMinimumTrade(string baseCommodity)
        {
            var minimumValueDictionary = new Dictionary<string, decimal>(StringComparer.InvariantCultureIgnoreCase)
            {
                { "BTC", BitzMinimumBTC },
                { "ETH", BitzMinimumEth }
            };

            if (!minimumValueDictionary.ContainsKey(baseCommodity))
            {
                throw new ApplicationException($"Minimum trade quantity not specified for base commodity \"{baseCommodity}\".");
            }

            return minimumValueDictionary[baseCommodity];
        }
        
        private void AutoEthBtc()
        {
            var agentConfig = _configClient.GetBitzAgentConfig();
            if (agentConfig != null && agentConfig.IsAutoTradingEnabled || agentConfig.EthThreshold <= 0) { return; }

            decimal profitPercentageThreshold = agentConfig.EthThreshold;

            var tradingPair = new TradingPair("ETH", "BTC");
            var cachePolicy = CachePolicy.ForceRefresh;

            Console.WriteLine($"{DateTime.Now} - Beginning Iteration");

            var binanceOrderBookTask = Task.Run(() => _binanceIntegration.GetOrderBook(new TradingPair("ETH", "BTC"), cachePolicy));
            var bitzOrderBookTask = Task.Run(() => _bitzIntegration.GetOrderBook(new TradingPair("ETH", "BTC"), cachePolicy));

            var binanceOrderBook = binanceOrderBookTask.Result;
            var bitzOrderBook = bitzOrderBookTask.Result;

            var strategyAction = _autoEthBtc.Execute(bitzOrderBook, binanceOrderBook, profitPercentageThreshold, BitzMinimumEth);
            Console.WriteLine($"{DateTime.Now} -   {JsonConvert.SerializeObject(strategyAction, Formatting.Indented)}.");

            if (strategyAction.ActionType == StrategyActionEnum.DoNothing)
            {
                Console.WriteLine($"{DateTime.Now} -   Did not find any good orders.");
            }
            else if (strategyAction.ActionType == StrategyActionEnum.PlaceAsk)
            {
                Console.WriteLine($"{DateTime.Now} -   About to sell {strategyAction.Quantity} ETH/BTC at {strategyAction.Price}.");
                _log.Info($"Bit-Z - About to sell {strategyAction.Quantity} ETH/BTC at {strategyAction.Price}.");

                try
                {
                    var result = _bitzIntegration.SellLimit(tradingPair, strategyAction.Quantity, strategyAction.Price);
                    if (result) { Console.WriteLine($"{DateTime.Now} -   Success!"); }
                    else { Console.WriteLine($"{DateTime.Now} -   Failed!"); }
                }
                catch(Exception exception)
                {
                    Console.WriteLine(exception);
                }
            }
            else if (strategyAction.ActionType == StrategyActionEnum.PlaceBid)
            {
                Console.WriteLine($"{DateTime.Now} -   About to buy {strategyAction.Quantity} ETH/BTC at {strategyAction.Price}.");
                _log.Info($"Bit-Z - About to buy {strategyAction.Quantity} ETH/BTC at {strategyAction.Price}.");

                try
                {
                    var result = _bitzIntegration.BuyLimit(tradingPair, strategyAction.Quantity, strategyAction.Price);
                    if (result) { Console.WriteLine($"{DateTime.Now} -   Success!"); }
                    else { Console.WriteLine($"{DateTime.Now} -   Failed!"); }
                }
                catch (Exception exception)
                {
                    Console.WriteLine(exception);
                }
            }

            if (strategyAction.ActionType != StrategyActionEnum.DoNothing)
            {
                _bitzIntegration.CancelAllOpenOrdersForTradingPair(tradingPair);
            }
        }

        public void Dispose()
        {
        }
    }
}
