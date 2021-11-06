using cache_lib.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using task_lib;
using trade_constants;
using trade_model;

namespace arb_workflow_lib
{
    public partial class ArbWorkflowUtil
    {
        public void SingleExchangeArb(string exchange, string symbol)
        {
            var tradingPairs = _exchangeClient.GetTradingPairs(exchange, CachePolicy.AllowCache);
            var symbolEthTradingPair = tradingPairs.Single(item => string.Equals(item.Symbol, symbol, StringComparison.InvariantCultureIgnoreCase));
            var matchingTradingPairs = tradingPairs.Where(item => string.Equals(symbol, item.Symbol, StringComparison.InvariantCultureIgnoreCase)).ToList();

            if (!matchingTradingPairs.Any()) { throw new ApplicationException($"Not trading pairs on {exchange} found for {symbol}."); }

            foreach (var tradingPair in matchingTradingPairs)
            {
                var openOrders = _exchangeClient.GetOpenOrdersForTradingPairV2(exchange, tradingPair.Symbol, tradingPair.BaseSymbol, CachePolicy.ForceRefresh);
                foreach (var openOrder in openOrders?.OpenOrders ?? new List<OpenOrder>())
                {
                    _exchangeClient.CancelOrder(exchange, openOrder);
                }
            }

            OrderBook binanceEthBtcOrderBook = null;
            var binanceTask = LongRunningTask.Run(() =>
            {
                _exchangeClient.GetOrderBook(IntegrationNameRes.Binance, "ETH", "BTC", CachePolicy.ForceRefresh);
            });
            

            var symbolEthOrderBook = _exchangeClient.GetOrderBook(exchange, exchange, "ETH", CachePolicy.ForceRefresh);
            var symbolBtcOrderBook = _exchangeClient.GetOrderBook(exchange, exchange, "BTC", CachePolicy.ForceRefresh);

            binanceTask.Wait();
            var ethBtcRatio = new List<decimal> { binanceEthBtcOrderBook.BestBid().Price, binanceEthBtcOrderBook.BestAsk().Price }.Average();

            var symbolEthBestBid = symbolEthOrderBook.BestBid();
            var symbolEthBestBidPrice = symbolEthBestBid.Price;           
            if (symbolEthBestBidPrice <= 0) { throw new ApplicationException($"{exchange}'s {symbol}-ETH best bid price must be > 0."); }

            var symbolEthBestAsk = symbolEthOrderBook.BestAsk();
            var symbolEthBestAskPrice = symbolEthBestAsk.Price;
            if (symbolEthBestAskPrice <= 0) { throw new ApplicationException($"{exchange}'s {symbol}-ETH best ask price must be > 0."); }
            if (symbolEthBestAskPrice <= symbolEthBestBidPrice) { throw new ApplicationException($"{exchange}'s {symbol}-ETH best bid price must be less than its best ask price."); }

            var symbolBtcBestBid = symbolBtcOrderBook.BestBid();
            var symbolBtcBestBidPrice = symbolBtcBestBid.Price;
            if (symbolBtcBestBidPrice <= 0) { throw new ApplicationException($"{exchange}'s {symbol}-BTC best bid price must be > 0."); }

            var symbolBtcBestAsk = symbolBtcOrderBook.BestAsk();
            var symbolBtcBestAskPrice = symbolBtcBestAsk.Price;
            if (symbolBtcBestAskPrice <= 0) { throw new ApplicationException($"{exchange}'s {symbol}-BTC best ask price must be > 0."); }
            if (symbolBtcBestAskPrice <= symbolBtcBestBidPrice) { throw new ApplicationException($"{exchange}'s {symbol}-BTC best bid price must be less than its best ask price."); }

            var symbolEthBestBidPriceAsBtc = symbolEthBestBidPrice * ethBtcRatio;
            var symbolEthBestAskPriceAsBtc = symbolEthBestAskPrice * ethBtcRatio;

            var symbolBtcBestBidPriceAsEth = symbolBtcBestBidPrice / ethBtcRatio;
            var symbolBtcBestAskPriceAsEth = symbolBtcBestAskPrice / ethBtcRatio;

            var optimalEthBidPrice = symbolBtcBestBidPriceAsEth * 0.85m;

            throw new NotImplementedException();
        }
    }
}
