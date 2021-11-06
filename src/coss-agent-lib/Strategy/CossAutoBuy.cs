using cache_lib.Models;
using config_client_lib;
using exchange_client_lib;
using log_lib;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using task_lib;
using trade_lib;
using trade_model;
using trade_res;
using trade_strategy_lib;

namespace coss_agent_lib.Strategy
{
    public class CossAutoBuy : ICossAutoBuy
    {
        private readonly ICossDriver _cossDriver;
        private readonly IConfigClient _configClient;
        private readonly IExchangeClient _exchangeClient;
        private readonly ILogRepo _log;

        public CossAutoBuy(
            ICossDriver cossDriver,
            IConfigClient configClient,
            IExchangeClient exchangeClient,
            ILogRepo log)
        {
            _cossDriver = cossDriver;
            _configClient = configClient;
            _exchangeClient = exchangeClient;
            _log = log;
        }

        public void Execute()
        {
            var autoBuy = new AutoBuy(_log);

            var tradingPairsToCheck = new List<TradingPair>();

            foreach (var symbol in CossAgentRes.SimpleBinanceSymbols)
            {
                tradingPairsToCheck.Add(new TradingPair(symbol, "BTC"));
                tradingPairsToCheck.Add(new TradingPair(symbol, "ETH"));
            }

            // tradingPairsToCheck.Add(new TradingPair("DASH", "BTC"));
            tradingPairsToCheck.Add(new TradingPair("LTC", "BTC"));
            tradingPairsToCheck.Add(new TradingPair("BCH", "BTC"));

            var config = _configClient.GetCossAgentConfig();
            var pairsDisplays = tradingPairsToCheck.Select(item => $"[{item}]").ToList();
            _log.Info($"AutoBuy -- Starting Process with a threshold of {config.TokenThreshold}% with the following trading pairs:{Environment.NewLine}{string.Join(", ", pairsDisplays)}");

            var totalPairsWithPurchases = 0;
            foreach (var tradingPair in tradingPairsToCheck)
            {
                var cossOrderBookTask = LongRunningTask.Run(() => _exchangeClient.GetOrderBook(ExchangeNameRes.Coss, tradingPair.Symbol, tradingPair.BaseSymbol, CachePolicy.ForceRefresh));
                var binanceOrderBookTask = LongRunningTask.Run(() => _exchangeClient.GetOrderBook(ExchangeNameRes.Binance, tradingPair.Symbol, tradingPair.BaseSymbol, CachePolicy.ForceRefresh));

                var cossOrderBook = cossOrderBookTask.Result;
                var binanceOrderBook = binanceOrderBookTask.Result;

                var minimumDictionary = new Dictionary<string, decimal>(StringComparer.InvariantCultureIgnoreCase)
                {
                    { "ETH", CossStrategyConstants.CossMinimumTradeEth },
                    { "BTC", CossStrategyConstants.CossMinimumTradeEth }
                };

                if (!minimumDictionary.ContainsKey(tradingPair.BaseSymbol)) { throw new ArgumentException($"Unexpected base commodity \"{tradingPair.BaseSymbol}\"."); }

                var minimumTrade = minimumDictionary[tradingPair.BaseSymbol];

                var autoBuyResult = autoBuy.Execute(cossOrderBook.Asks, binanceOrderBook.BestBid().Price, minimumTrade, config.TokenThreshold);
                if (autoBuyResult == null || autoBuyResult.Quantity <= 0) { continue; }

                if (autoBuyResult.Price < 0) { throw new ApplicationException($"Auto-buy price should not be less than zero, but it was \"{autoBuyResult.Price}\""); }

                var orderToPlace = new Order { Price = autoBuyResult.Price, Quantity = autoBuyResult.Quantity };

                _cossDriver.CancelAllForTradingPair(tradingPair);

                _cossDriver.NavigateToExchange(tradingPair);

                var logBuilder = new StringBuilder()
                    .AppendLine($"AutoBuy - About to place {tradingPair} order")
                    .AppendLine("Order:")
                    .AppendLine(JsonConvert.SerializeObject(orderToPlace))
                    .AppendLine("Coss Order Book:")
                    .AppendLine(JsonConvert.SerializeObject(cossOrderBook))
                    .AppendLine("Binance Order Book:")
                    .AppendLine(JsonConvert.SerializeObject(binanceOrderBook));

                _log.Info(logBuilder.ToString(), TradeEventType.AboutToPlaceOrder);
                totalPairsWithPurchases++;
                _cossDriver.PlaceOrder(tradingPair, OrderType.Bid, new QuantityAndPrice { Price = orderToPlace.Price, Quantity = orderToPlace.Quantity }, true);

                _cossDriver.CancelAllForTradingPair(tradingPair);
            }

            if (totalPairsWithPurchases == 0)
            {
                _log.Info("AutoBuy Complete. There were no purchases to make.");
            }
            else
            {
                _log.Info($"AutoBuy Complete. Made purchases from {totalPairsWithPurchases} trading pairs.");
            }
        }
    }
}
