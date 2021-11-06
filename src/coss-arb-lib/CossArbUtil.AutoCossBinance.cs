using cache_lib.Models;
using math_lib;
using System;
using System.Linq;
using System.Threading;
using task_lib;
using trade_constants;
using trade_model;

namespace coss_arb_lib
{
    public partial class CossArbUtil
    {
        public void AutoCossBinance(string symbol, string quoteSymbol)
        {
            const decimal LotSize = 0.001m;
            const decimal PriceTick = 0.00001m;

            var binanceOrderBookTask = LongRunningTask.Run(() => _exchangeClient.GetOrderBook(IntegrationNameRes.Binance, symbol, quoteSymbol, CachePolicy.ForceRefresh));

            var cossOpenOrders = _exchangeClient.GetOpenOrdersForTradingPairV2(IntegrationNameRes.Coss, symbol, quoteSymbol, CachePolicy.ForceRefresh);
            if (cossOpenOrders.OpenOrders?.Any() ?? false)
            {
                Console.WriteLine($"There are already open orders on {symbol}-{quoteSymbol}.{Environment.NewLine}Cancelling them.");
                foreach (var openOrder in cossOpenOrders.OpenOrders)
                {
                    // https://api.coss.io/v1/spec-api
                    // Coss forbids cancelling an order within 10 seconds of placing that order.
                    // This is a safeguard to prevent that scenario.
                    Thread.Sleep(TimeSpan.FromSeconds(12.5));

                    _exchangeClient.CancelOrder(IntegrationNameRes.Coss, openOrder);
                }

                cossOpenOrders = _exchangeClient.GetOpenOrdersForTradingPairV2(IntegrationNameRes.Coss, symbol, quoteSymbol, CachePolicy.ForceRefresh);
                if (cossOpenOrders.OpenOrders?.Any() ?? false)
                {
                    Console.WriteLine($"There are STILL open orders on {symbol}-{quoteSymbol} after cancelling them.{Environment.NewLine}Aborting.");
                    return;
                }
            }

            var cossBalances = _exchangeClient.GetBalances(IntegrationNameRes.Coss, CachePolicy.ForceRefresh);
            var cossSymbolBalance = cossBalances.Holdings.Where(queryItem => string.Equals(queryItem.Symbol, symbol, StringComparison.InvariantCultureIgnoreCase)).SingleOrDefault();
            var cossSymbolAvailable = cossSymbolBalance?.Available ?? 0;
            var cossQuoteSymbolBalance = cossBalances.Holdings.Where(queryItem => string.Equals(queryItem.Symbol, quoteSymbol, StringComparison.InvariantCultureIgnoreCase)).SingleOrDefault();

            var cossOrderBook = _exchangeClient.GetOrderBook(IntegrationNameRes.Coss, symbol, quoteSymbol, CachePolicy.ForceRefresh);
            var binanceOrderBook = binanceOrderBookTask.Result;
            
            var binanceBestBid = binanceOrderBook.BestBid();
            if (binanceBestBid == null)
            {
                Console.WriteLine($"There are no bids on the Binance {symbol}-{quoteSymbol} order book. Aborting.");
                return;
            }

            var binanceBestBidPrice = binanceBestBid.Price;

            var binanceBestAsk = binanceOrderBook.BestAsk();
            if (binanceBestAsk == null)
            {
                Console.WriteLine($"There are no asks on the Binance {symbol}-{quoteSymbol} order book. Aborting.");
                return;
            }

            var binanceBestAskPrice = binanceBestAsk.Price;

            var cossBidsToTake = cossOrderBook.Bids.Where(queryItem => queryItem.Price >= binanceBestAskPrice).ToList();

            if (cossBidsToTake.Any())
            {
                var lowestBidPriceToTake = cossBidsToTake.Select(item => item.Price).Min();
                var quantityToTake = cossBidsToTake.Sum(item => item.Quantity);
                if (quantityToTake > cossSymbolAvailable)
                {
                    quantityToTake = cossSymbolAvailable;
                }

                quantityToTake = MathUtil.ConstrainToMultipleOf(quantityToTake, LotSize);
                if (quantityToTake <= 0)
                {
                    Console.WriteLine($"There are orders on {symbol}-{quoteSymbol} worth taking, but the quantity that we'd have to trade is less than the minimum.");
                }
                else
                {
                    Console.WriteLine($"About to place an ask on Coss for {quantityToTake} {symbol} at {lowestBidPriceToTake} {quoteSymbol}.");

                    try
                    {
                        var sellResult = _exchangeClient.SellLimitV2(IntegrationNameRes.Coss, symbol, quoteSymbol, new QuantityAndPrice
                        {
                            Quantity = quantityToTake,
                            Price = lowestBidPriceToTake
                        });

                        if (sellResult.WasSuccessful)
                        {
                            Console.WriteLine($"Successfully placed an ask on Coss for {quantityToTake} {symbol} at {lowestBidPriceToTake} {quoteSymbol}.");
                        }
                        else
                        {
                            Console.WriteLine($"Failed to place an ask on Coss for {quantityToTake} {symbol} at {lowestBidPriceToTake} {quoteSymbol}.{Environment.NewLine}{sellResult.FailureReason}");
                        }
                    }
                    catch (Exception exception)
                    {
                        Console.WriteLine($"Failed to place an ask on Coss for {quantityToTake} {symbol} at {lowestBidPriceToTake} {quoteSymbol}.{Environment.NewLine}{exception.Message}");
                    }
                }
            }
            else
            {
                Console.WriteLine($"There are no Coss bids on {symbol}-{quoteSymbol} worth taking.");
            }

            var cossAsksToTake = cossOrderBook.Asks.Where(queryItem => queryItem.Price <= binanceBestBidPrice).ToList();
            if (cossAsksToTake.Any())
            {

            }
            else
            {
                Console.WriteLine($"There are no Coss asks on {symbol}-{quoteSymbol} worth taking.");
            }
        }
    }
}
