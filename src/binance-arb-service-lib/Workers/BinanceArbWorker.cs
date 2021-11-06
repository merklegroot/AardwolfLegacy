using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using arb_service_lib;
using arb_workflow_lib;
using cache_lib.Models;
using config_client_lib;
using exchange_client_lib;
using log_lib;
using math_lib;
using trade_constants;
using trade_model;

namespace binance_arb_service_lib.Workers
{
    public interface IBinanceArbWorker : IArbWorker
    {
    }

    public class BinanceArbWorker : ArbWorker, IBinanceArbWorker
    {
        private readonly IArbWorkflowUtil _arbWorkflowUtil;
        private readonly IExchangeClient _exchangeClient;
        private readonly IConfigClient _configClient;
        private readonly ILogRepo _log;

        public BinanceArbWorker(
            IArbWorkflowUtil arbWorkflowUtil,
            IExchangeClient exchangeClient,
            IConfigClient configClient,
            ILogRepo log)
            : base(log)
        {
            _arbWorkflowUtil = arbWorkflowUtil;
            _exchangeClient = exchangeClient;
            _configClient = configClient;
            _log = log;
        }

        protected override List<Action> Jobs => new List<Action>
        {
            ProcessTusd,
            ProcessArk,
            ProcessEth,
            ProcessLtc,
            ProcessWaves,
            ProcessNeo,
            ProcessBtc
            //ProcessConvertEthToBtc
        };

        private void ProcessTusd()
        {
            var binanceArbConfig = _configClient.GetBinanceArbConfig();
            if (!binanceArbConfig.IsEnabled) { return; }

            var validTargets = new List<string> { "ETH", "BTC", "BNB" };
            if (!string.IsNullOrWhiteSpace(binanceArbConfig.EthSaleTarget) && validTargets.Any(queryTarget => string.Equals(binanceArbConfig.EthSaleTarget, queryTarget, StringComparison.InvariantCultureIgnoreCase)))
            {
                SlowSell("TUSD", binanceArbConfig.TusdSaleTarget);
            }
        }

        private void ProcessArk()
        {
            var binanceArbConfig = _configClient.GetBinanceArbConfig();
            if (!binanceArbConfig.IsEnabled) { return; }

            var validTargets = new List<string> { "ETH", "BTC", "BNB" };
            if (!string.IsNullOrWhiteSpace(binanceArbConfig.EthSaleTarget) && validTargets.Any(queryTarget => string.Equals(binanceArbConfig.EthSaleTarget, queryTarget, StringComparison.InvariantCultureIgnoreCase)))
            {
                SlowSell("ARK", binanceArbConfig.ArkSaleTarget);
            }
        }

        private void ProcessLtc()
        {
            var binanceArbConfig = _configClient.GetBinanceArbConfig();
            if (!binanceArbConfig.IsEnabled) { return; }

            var validTargets = new List<string> { "ETH", "BTC", "BNB", "USDT" };
            if (!string.IsNullOrWhiteSpace(binanceArbConfig.LtcSaleTarget) && validTargets.Any(queryTarget => string.Equals(binanceArbConfig.LtcSaleTarget, queryTarget, StringComparison.InvariantCultureIgnoreCase)))
            {
                SlowSell("LTC", binanceArbConfig.LtcSaleTarget);
            }
        }

        private void ProcessWaves()
        {
            const string Symbol = "WAVES";
            var validTargets = new List<string> { "ETH", "BTC", "BNB" };

            var binanceArbConfig = _configClient.GetBinanceArbConfig();
            if (!binanceArbConfig.IsEnabled) { return; }

            var saleTarget = binanceArbConfig.WavesSaleTarget;           
            
            if (!string.IsNullOrWhiteSpace(saleTarget) && validTargets.Any(queryTarget => string.Equals(saleTarget, queryTarget, StringComparison.InvariantCultureIgnoreCase)))
            {
                SlowSell(Symbol, saleTarget);
            }
        }

        private void ProcessNeo()
        {
            const string Symbol = "NEO";
            var validTargets = new List<string> { "ETH", "BTC", "USDT", "BNB" };

            var binanceArbConfig = _configClient.GetBinanceArbConfig();
            if (!binanceArbConfig.IsEnabled) { return; }

            var saleTarget = binanceArbConfig.SaleTargetDictionary[Symbol];

            if (!string.IsNullOrWhiteSpace(saleTarget) && validTargets.Any(queryTarget => string.Equals(saleTarget, queryTarget, StringComparison.InvariantCultureIgnoreCase)))
            {
                SlowSell(Symbol, saleTarget);
            }
        }

        private void ProcessEth()
        {
            var binanceArbConfig = _configClient.GetBinanceArbConfig();
            if (!binanceArbConfig.IsEnabled) { return; }

            var validTargets = new List<string> { "BTC", "TUSD", "BNB", "WAVES", "USDC" };
            if (!string.IsNullOrWhiteSpace(binanceArbConfig.EthSaleTarget) && validTargets.Any(queryTarget => string.Equals(binanceArbConfig.EthSaleTarget, queryTarget, StringComparison.InvariantCultureIgnoreCase)))
            {
                if (string.Equals(binanceArbConfig.EthSaleTarget, "TUSD"))
                {
                    SlowBuy(binanceArbConfig.EthSaleTarget, "ETH");
                }
                if (string.Equals(binanceArbConfig.EthSaleTarget, "WAVES"))
                {
                    SlowSell(binanceArbConfig.EthSaleTarget, "ETH");
                }
                else
                {
                    SlowSell("ETH", binanceArbConfig.EthSaleTarget);
                }                
            }

            Thread.Sleep(TimeSpan.FromSeconds(10));
        }

        private void ProcessBtc()
        {
            var binanceArbConfig = _configClient.GetBinanceArbConfig();
            if (!binanceArbConfig.IsEnabled) { return; }

            var target = binanceArbConfig.BtcSaleTarget;
            var validTargets = new List<string> { "USDC", "ETH" };
            if (!string.IsNullOrWhiteSpace(target) && validTargets.Any(queryTarget => string.Equals(target, queryTarget, StringComparison.InvariantCultureIgnoreCase)))
            {
                var buyTargets = new List<string> { "USDC", "ETH" };
                if (buyTargets.Any(queryBuyTarget => string.Equals(queryBuyTarget, target, StringComparison.InvariantCultureIgnoreCase)))
                {
                    SlowBuy(target, "BTC");
                }
                else
                {
                    SlowSell("BTC", target);
                }
            }

            Thread.Sleep(TimeSpan.FromSeconds(10));
        }

        private void SlowBuy(string symbol, string baseSymbol)
        {
            var exchange = IntegrationNameRes.Binance;

            var minBalanceForBaseSymbolDictionary = new Dictionary<string, decimal>(StringComparer.InvariantCultureIgnoreCase)
            {
                { "ETH", 0.01m },
                { "BTC", 0.005m },
            };

            var minBalanceForBid = minBalanceForBaseSymbolDictionary[baseSymbol];

            var tradingPairs = _exchangeClient.GetTradingPairs(exchange, CachePolicy.AllowCache);
            var tradingPair = tradingPairs.Single(item => string.Equals(item.Symbol, symbol, StringComparison.InvariantCultureIgnoreCase)
                && string.Equals(item.BaseSymbol, baseSymbol, StringComparison.InvariantCultureIgnoreCase));

            var priceTick = tradingPair.PriceTick.Value;
            var lotSize = tradingPair.LotSize.Value;            

            var openOrdersResult = _exchangeClient.GetOpenOrdersForTradingPairV2(exchange, symbol, baseSymbol, CachePolicy.ForceRefresh);
            var openOrders = openOrdersResult?.OpenOrders ?? new List<OpenOrder>();
            foreach (var openOrder in openOrders)
            {
                _exchangeClient.CancelOrder(exchange, openOrder);
            }

            var baseSymbolBalance = _exchangeClient.GetBalance(exchange, baseSymbol, CachePolicy.ForceRefresh);
            var baseSymbolAvailable = baseSymbolBalance?.Available ?? 0;

            var orderBook = _exchangeClient.GetOrderBook(exchange, symbol, baseSymbol, CachePolicy.ForceRefresh);
            var bestBidPrice = orderBook.BestBid().Price;
            if (bestBidPrice <= 0) { throw new ApplicationException($"{exchange}'s best bid price for {symbol}-{baseSymbol} must be > 0."); }

            var priceToBid = bestBidPrice + priceTick;
            var quantityToBid = MathUtil.ConstrainToMultipleOf(baseSymbolAvailable / priceToBid / 1.01m, lotSize);

            if (quantityToBid >= minBalanceForBid)
            {
                var price = priceToBid;
                var quantity = quantityToBid;

                _log.Info($"About to place a bid on {exchange} for {quantity} {symbol} at {price} {baseSymbol}.");
                try
                {
                    var orderResult = _exchangeClient.BuyLimitV2(exchange, symbol, baseSymbol, new QuantityAndPrice
                    {
                        Price = price,
                        Quantity = quantity
                    });

                    if (orderResult.WasSuccessful)
                    {
                        _log.Info($"Successfully placed a bid on {exchange} for {quantity} {symbol} at {price} {baseSymbol}.");
                    }
                    else
                    {
                        _log.Error($"Failed to place a bid on {exchange} for {quantity} {symbol} at {price} {baseSymbol}.");
                    }
                }
                catch (Exception exception)
                {
                    _log.Error($"Failed to place a bid on {exchange} for {quantity} {symbol} at {price} {baseSymbol}.{Environment.NewLine}{exception.Message}");
                    _log.Error(exception);
                }
            }
        }

        private void SlowSell(string symbol, string targetBaseSymbol)
        {
            var minBalanceForSymbol = new Dictionary<string, decimal>(StringComparer.InvariantCultureIgnoreCase)
            {
                { "TUSD", 0.5m },
                { "USDC", 0.5m },
                { "ARK", 0.1m },
                { "ETH", 0.01m },
                { "LTC", 0.01m },
                { "WAVES", 0.01m },
                { "NEO", 0.01m },
            };

            var symbolBalance = _exchangeClient.GetBalance(IntegrationNameRes.Binance, symbol, CachePolicy.ForceRefresh);
            var totalSymbolBalance = symbolBalance?.Total ?? 0;
            if (totalSymbolBalance >= minBalanceForSymbol[symbol])
            {
                _arbWorkflowUtil.AutoSell(IntegrationNameRes.Binance, symbol, targetBaseSymbol);
                return;
            }

            // If there's less than a dollar, don't waste our rate limit on it.
            // Just cancel the open orders and sleep.
            var openOrders = _exchangeClient.GetOpenOrders(IntegrationNameRes.Binance, symbol, targetBaseSymbol, CachePolicy.ForceRefresh);
            if (openOrders != null && openOrders.Any())
            {
                foreach (var openOrder in openOrders)
                {
                    _exchangeClient.CancelOrder(IntegrationNameRes.Binance, openOrder.OrderId);
                }
            }

            Thread.Sleep(TimeSpan.FromMinutes(2.5));
        }

        private void SlowSellTusd(string targetBaseSymbol)
        {
            var tusdBalance = _exchangeClient.GetBalance(IntegrationNameRes.Binance, "TUSD", CachePolicy.ForceRefresh);
            var totalTusdBalance = tusdBalance?.Total ?? 0;
            if (totalTusdBalance >= 1.0m)
            {
                _arbWorkflowUtil.AutoSell(IntegrationNameRes.Binance, "TUSD", targetBaseSymbol);
                return;
            }            

            // If there's less than a dollar, don't waste our rate limit on it.
            // Just cancel the open orders and sleep.
            var openOrders = _exchangeClient.GetOpenOrders(IntegrationNameRes.Binance, "TUSD", targetBaseSymbol, CachePolicy.ForceRefresh);
            if (openOrders != null && openOrders.Any())
            {
                foreach (var openOrder in openOrders)
                {
                    _exchangeClient.CancelOrder(IntegrationNameRes.Binance, openOrder.OrderId);
                }
            }

            Thread.Sleep(TimeSpan.FromMinutes(2.5));
        }

        private void AcquireQuantity(string exchange, string symbol, string baseSymbol, decimal desiredQuantity)
        {
            var balances = _exchangeClient.GetBalances(IntegrationNameRes.Binance, CachePolicy.ForceRefresh);
            var totalForSymbol = balances.GetTotalForSymbol(symbol);
            var remainingQuantity = desiredQuantity - totalForSymbol;
            var ratioRemaining = remainingQuantity / desiredQuantity;
            var percentRemaining = 100.0m * ratioRemaining;

            if (percentRemaining <= 1.0m) { return; }

            var commodity = _exchangeClient.GetCommoditiyForExchange(IntegrationNameRes.Binance, symbol, null, CachePolicy.AllowCache);
            var tradingPairs = _exchangeClient.GetTradingPairs(IntegrationNameRes.Binance, CachePolicy.AllowCache);
            var tradingPair = tradingPairs.Single(item => string.Equals(item.Symbol, symbol, StringComparison.InvariantCultureIgnoreCase)
                && string.Equals(item.BaseSymbol, baseSymbol, StringComparison.InvariantCultureIgnoreCase));

            var lotSize = tradingPair.LotSize.Value;
            if (lotSize <= 0) { throw new ApplicationException($"Binance's lot size for {symbol}-{baseSymbol} must be > 0."); }

            var priceTick = tradingPair.PriceTick.Value;
            if (priceTick <= 0) { throw new ApplicationException($"Binance's price tick for {symbol}-{baseSymbol} must be > 0."); }

            var openOrdersResult = _exchangeClient.GetOpenOrdersForTradingPairV2(IntegrationNameRes.Binance, symbol, baseSymbol, CachePolicy.ForceRefresh);
            foreach(var openOrder in openOrdersResult?.OpenOrders ?? new List<OpenOrder>())
            {
                _exchangeClient.CancelOrder(IntegrationNameRes.Binance, openOrder);
            }

            var orderBook = _exchangeClient.GetOrderBook(IntegrationNameRes.Binance, symbol, baseSymbol, CachePolicy.ForceRefresh);

            var bestBidPrice = orderBook.BestBid().Price;
            if (bestBidPrice <= 0) { throw new ApplicationException($"Binance's {symbol}-{baseSymbol} best bid price should be > 0."); }

            var bidPriceToPlace = bestBidPrice + priceTick;
            var quantity = MathUtil.ConstrainToMultipleOf(remainingQuantity, lotSize);

            var price = bidPriceToPlace;

            var orderResult = _exchangeClient.BuyLimitV2(IntegrationNameRes.Binance, symbol, baseSymbol, new QuantityAndPrice
            {
                Price = price,
                Quantity = quantity
            });
        }
    }
}
