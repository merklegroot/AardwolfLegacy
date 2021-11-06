using cache_lib.Models;
using cryptocompare_client_lib;
using integration_workflow_lib.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using task_lib;
using trade_model;
using trade_res;
using exchange_client_lib;

namespace integration_workflow_lib
{
    public class ArbitrageWorkflow : IArbitrageWorkflow
    {
        private class OrderEx
        {
            public decimal Price { get; set; }
            public decimal Quantity { get; set; }
            public string BaseCommodity { get; set; }
            public decimal CanonicalPrice { get; set; }

            public static OrderEx FromModel(Order model, string baseCommodity, decimal ethBtcRatio)
            {
                return new OrderEx
                {
                    Price = model.Price,
                    Quantity = model.Quantity,
                    BaseCommodity = baseCommodity,
                    CanonicalPrice = string.Equals(baseCommodity, "BTC", StringComparison.InvariantCultureIgnoreCase) ? model.Price : model.Price * ethBtcRatio
                };
            }
        }

        private readonly IExchangeClient _exchangeClient;
        private readonly ICryptoCompareClient _cryptoCompareClient;

        public ArbitrageWorkflow(
            IExchangeClient exchangeClient,
            ICryptoCompareClient cryptoCompareClient)
        {
            _exchangeClient = exchangeClient;
            _cryptoCompareClient = cryptoCompareClient;
        }

        public ArbitrageResult Execute(
            string source,
            string destination,
            Commodity commodity,
            CachePolicy cachePolicy)
        {
            var data = GenerateArbitrageData(source, destination, commodity.Symbol, null, cachePolicy);

            return Execute(data);
        }

        public ArbitrageResult Execute(
            string source,
            string destination,
            string symbol,
            CachePolicy cachePolicy)
        {
            var data = GenerateArbitrageData(source, destination, symbol, null, cachePolicy);

            return Execute(data);
        }

        public ArbitrageResult Execute(
            string source,
            string destination,
            string symbol,
            Dictionary<string, decimal> valuations,
            CachePolicy cachePolicy)
        {
            var data = GenerateArbitrageData(source, destination, symbol, valuations, cachePolicy);

            return Execute(data);
        }

        public ArbitrageResult Execute(ArbitrageData data)
        {
            if (data == null) { throw new ArgumentNullException(nameof(data)); }

            if ((!data?.SourceOrderBooks?.Any(queryOrderBook => queryOrderBook?.OrderBook?.Asks?.Any() ?? false) ?? false)
                || (!data?.DestOrderBooks?.Any(queryOrderBook => queryOrderBook?.OrderBook?.Bids?.Any() ?? false) ?? false))
            { return new ArbitrageResult(); }

            const decimal MinimumRatio = 0.0075m;

            var withdrawalFee = data.SourceWithdrawalFee;
            var ethBtcRatio = data.EthToBtcRatio;

            var sourceEthOrderBook = data.SourceEthOrderBook;
            var sourceBtcOrderBook = data.SourceBtcOrderBook;

            var destEthOrderBook = data.DestEthOrderBook;
            var destBtcOrderBook = data.DestBtcOrderBook;

            var sourceAsks = new List<OrderEx>();
            if (sourceEthOrderBook != null)
            { sourceAsks.AddRange(sourceEthOrderBook.Asks.Select(queryOrder => OrderEx.FromModel(queryOrder, "ETH", ethBtcRatio))); }

            if (sourceBtcOrderBook != null)
            { sourceAsks.AddRange(sourceBtcOrderBook.Asks.Select(queryOrder => OrderEx.FromModel(queryOrder, "BTC", ethBtcRatio))); }

            var destBids = new List<OrderEx>();
            if (destEthOrderBook != null)
            { destBids.AddRange(destEthOrderBook.Bids.Select(queryOrder => OrderEx.FromModel(queryOrder, "ETH", ethBtcRatio))); }

            if (destBtcOrderBook != null)
            { destBids.AddRange(destBtcOrderBook.Bids.Select(queryOrder => OrderEx.FromModel(queryOrder, "BTC", ethBtcRatio))); }

            var asks = sourceAsks.OrderBy(item => item.CanonicalPrice).ToList();
            var bids = destBids.OrderByDescending(item => item.CanonicalPrice).ToList();

            decimal totalEthQuantityToTake = 0;
            decimal totalEthToSpend = 0;
            decimal totalBtcQuantityToTake = 0;
            decimal totalBtcToSpend = 0;
            decimal? highestEthPriceToTake = null;
            decimal? highestBtcPriceToTake = null;
            var takeQuantity = new Action<decimal>(quantity =>
            {
                var ask = asks.First();
                if (ask.BaseCommodity == "ETH")
                {
                    highestEthPriceToTake = ask.Price;
                    totalEthQuantityToTake += quantity;
                    totalEthToSpend += quantity * ask.Price;
                }
                else
                {
                    highestBtcPriceToTake = ask.Price;
                    totalBtcQuantityToTake += quantity;
                    totalBtcToSpend += quantity * ask.Price;
                }

                if (ask.Quantity <= quantity)
                {
                    asks.RemoveAt(0);
                }
                else
                {
                    ask.Quantity -= quantity;
                }

                var bid = bids.First();
                if (bid.Quantity <= quantity)
                {
                    bids.RemoveAt(0);
                }
                else
                {
                    bid.Quantity -= quantity;
                }
            });

            var firstAskPrice = asks.First().CanonicalPrice;
            decimal canonicalProfit = 0;
            while (asks.Any() && bids.Any())
            {
                var bid = bids.First();
                var ask = asks.First();
                var diff = (bid.CanonicalPrice - ask.CanonicalPrice);
                if (diff < 0) { break; }
                var ratio = diff / ask.CanonicalPrice;
                if (ratio < MinimumRatio) { break; }

                var quantityToTake = GetLowerValue(ask.Quantity, bid.Quantity);
                canonicalProfit += quantityToTake * diff;

                takeQuantity(quantityToTake);
            }
            
            // the withdrawal fee if it were in BTC.
            var canonicalWithdrawalFee = withdrawalFee * firstAskPrice;

            // a value of 0 means we'd break even.
            var canonicalProfitAfterFee = canonicalProfit - (canonicalWithdrawalFee ?? 0);
            if (canonicalWithdrawalFee > 0)
            {
                var profitAfterFeeToFeeRatio = canonicalProfitAfterFee / canonicalWithdrawalFee;

                const decimal MinimumProfitAfterFeeToFeeRatio = 1.0m;
                if (profitAfterFeeToFeeRatio <= MinimumProfitAfterFeeToFeeRatio)
                {
                    return new ArbitrageResult();
                }
            }

            // we'd probably be better off taking slightly less
            // than the total, but this is a good enough
            // algorithm for now.
            return new ArbitrageResult
            {
                ExpectedUsdCost = 
                    totalEthToSpend * data.EthToBtcRatio * data.BtcToUsdRatio
                    + totalBtcToSpend * data.BtcToUsdRatio,
                ExpectedUsdProfit = canonicalProfitAfterFee * data.BtcToUsdRatio,
                BtcPrice = highestBtcPriceToTake,
                BtcQuantity = totalBtcQuantityToTake,
                EthPrice = highestEthPriceToTake,
                EthQuantity = totalEthQuantityToTake
            };
        }

        //public BidirectionalArbitrageResult ExecuteBidirectional(ITradeIntegration source,
        //    ITradeIntegration destination,
        //    string symbol,
        //    CachePolicy cachePolicy)
        //{
        //    var data = GenerateArbitrageData(source, destination, symbol, cachePolicy);
        //    var resultA = Execute(data);

        //    var reversedData = new ArbitrageData
        //    {
        //        BtcToUsdRatio = data.BtcToUsdRatio,
        //        EthToBtcRatio = data.EthToBtcRatio,
        //        SourceEthOrderBook = data.DestEthOrderBook,
        //        Source
        //    };
        //}

        private bool _isSynchronousWorkflowEnabled = false;
        public void SetSynchronousWorkflow(bool shouldEnable)
        {
            _isSynchronousWorkflowEnabled = shouldEnable;
        }

        private ArbitrageData GenerateArbitrageData(
            string source,
            string destination,
            string symbol,
            Dictionary<string, decimal> valuations,
            CachePolicy cachePolicy)
        {
            if (destination == null) { throw new ArgumentNullException(nameof(destination)); }
            if (string.IsNullOrWhiteSpace(symbol)) { throw new ArgumentNullException(nameof(symbol)); }

            var ethTradingPair = new TradingPair(symbol, "ETH");
            var btcTradingPair = new TradingPair(symbol, "BTC");

            decimal ethToBtcRatio = 0;
            decimal btcToUsdRatio = 0;
            decimal? withdrawalFee = null;
            OrderBook sourceEthOrderBook = null;
            OrderBook sourceBtcOrderBook = null;
            OrderBook destEthOrderBook = null;
            OrderBook destBtcOrderBook = null;

            var tradingPairsCachePolicy = cachePolicy == CachePolicy.ForceRefresh
                ? CachePolicy.AllowCache
                : cachePolicy;

            var valuationTask = LongRunningTask.Run(() =>
            {
                var ethValue = valuations != null && valuations.ContainsKey("ETH")
                    ? valuations["ETH"]
                    : _cryptoCompareClient.GetUsdValue(CommodityRes.Eth.Symbol, cachePolicy);

                if (!ethValue.HasValue) { throw new ApplicationException("Failed to get ETH value from the valuation client."); }

                var btcValue = valuations != null && valuations.ContainsKey("BTC")
                    ? valuations["BTC"]
                    : _cryptoCompareClient.GetUsdValue(CommodityRes.Bitcoin.Symbol, cachePolicy);
                if (!btcValue.HasValue) { throw new ApplicationException("Failed to get BTC value from the valuation client."); }

                ethToBtcRatio = ethValue.Value / btcValue.Value;
                btcToUsdRatio = btcValue.Value;
            });

            if (_isSynchronousWorkflowEnabled) { valuationTask.Wait(); }

            var sourceTask = LongRunningTask.Run(() =>
            {
                var sourceTradingPairs = _exchangeClient.GetTradingPairs(source, tradingPairsCachePolicy);

                if (sourceTradingPairs.Any(queryTradingPair =>
                    string.Equals(queryTradingPair.Symbol, symbol, StringComparison.InvariantCultureIgnoreCase)
                    && string.Equals(queryTradingPair.BaseSymbol, CommodityRes.Eth.Symbol, StringComparison.InvariantCultureIgnoreCase)))
                {
                    sourceEthOrderBook = _exchangeClient.GetOrderBook(source, symbol, CommodityRes.Eth.Symbol, cachePolicy);
                }
                else { sourceEthOrderBook = null; }

                if (sourceTradingPairs.Any(queryTradingPair =>
                    string.Equals(queryTradingPair.Symbol, symbol, StringComparison.InvariantCultureIgnoreCase)
                    && string.Equals(queryTradingPair.Symbol, CommodityRes.Bitcoin.Symbol, StringComparison.InvariantCultureIgnoreCase)))
                {
                    sourceBtcOrderBook = _exchangeClient.GetOrderBook(source, symbol, CommodityRes.Bitcoin.Symbol, cachePolicy);
                }
                else { sourceBtcOrderBook = null; }

                withdrawalFee = _exchangeClient.GetWithdrawalFee(source, symbol, cachePolicy);
            });

            if (_isSynchronousWorkflowEnabled) { sourceTask.Wait(); }

            var destTask = LongRunningTask.Run(() =>
            {
                var destTradingPairs = _exchangeClient.GetTradingPairs(destination, tradingPairsCachePolicy);

                if (destTradingPairs.Any(queryTradingPair =>
                    string.Equals(queryTradingPair.Symbol, symbol, StringComparison.InvariantCultureIgnoreCase)
                    && string.Equals(queryTradingPair.BaseSymbol, CommodityRes.Eth.Symbol, StringComparison.InvariantCultureIgnoreCase)))
                {
                    destEthOrderBook = _exchangeClient.GetOrderBook(destination, symbol, CommodityRes.Eth.Symbol, cachePolicy);
                }
                else { destEthOrderBook = null; }

                if (destTradingPairs.Any(queryTradingPair =>
                    string.Equals(queryTradingPair.Symbol, symbol, StringComparison.InvariantCultureIgnoreCase)
                    && string.Equals(queryTradingPair.BaseSymbol, CommodityRes.Bitcoin.Symbol, StringComparison.InvariantCultureIgnoreCase)))
                {
                    destBtcOrderBook = _exchangeClient.GetOrderBook(destination, symbol, CommodityRes.Bitcoin.Symbol, cachePolicy);
                }
                else { destBtcOrderBook = null; }
            });

            if (_isSynchronousWorkflowEnabled) { destTask.Wait(); }

            valuationTask.Wait();
            sourceTask.Wait();
            destTask.Wait();

            var data = new ArbitrageData
            {
                EthToBtcRatio = ethToBtcRatio,
                BtcToUsdRatio = btcToUsdRatio,
                SourceEthOrderBook = sourceEthOrderBook,
                SourceBtcOrderBook = sourceBtcOrderBook,
                DestEthOrderBook = destEthOrderBook,
                DestBtcOrderBook = destBtcOrderBook,
                SourceWithdrawalFee = withdrawalFee
            };

            return data;
        }

        private decimal GetLowerValue(decimal a, decimal b)
        {
            return a < b ? a : b;
        }
    }
}
