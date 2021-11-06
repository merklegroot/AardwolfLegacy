using cache_lib.Models;
using exchange_client_lib;
using log_lib;
using math_lib;
using System;
using System.Collections.Generic;
using trade_constants;
using trade_model;

namespace hitbtc_arb_service_lib.Workflow
{
    public interface IHitbtcArbWorkflowUtil
    {
        void AutoHitbtcCoss();
    }

    public class HitbtcArbWorkflowUtil : IHitbtcArbWorkflowUtil
    {
        private readonly IExchangeClient _exchangeClient;
        private readonly ILogRepo _log;

        public HitbtcArbWorkflowUtil(
            IExchangeClient exchangeClient,
            ILogRepo log)
        {
            _exchangeClient = exchangeClient;
            _log = log;
        }

        public void AutoHitbtcCoss()
        {
            const string Symbol = "COSS";
            const string BaseSymbol = "ETH";

            const int PriceDecimals = 6;
            const decimal PriceTick = 0.000001m;
            const decimal LotSize = 0.01m;

            const decimal OptimalPercentDiff = 15.0m;
            const decimal MinBidPercentDiff = 5.0m;
            const decimal MinAskPercentDiff = 1.0m;
            const decimal DesiredQuantity = 1000.0m;
            const decimal MaxAskQuantity = 1000.0m;

            // HitBTC will probably let us go lower than this, but this minimum is good enough for now.
            const decimal MinQuantity = 10.0m;

            var hitbtcCossEthOpenOrders = _exchangeClient.GetOpenOrders(IntegrationNameRes.HitBtc, Symbol, BaseSymbol, CachePolicy.ForceRefresh) ?? new List<OpenOrderForTradingPair>();
            foreach(var openOrder in hitbtcCossEthOpenOrders)
            {
                _exchangeClient.CancelOrder(IntegrationNameRes.HitBtc, openOrder.OrderId);
            }

            var balances = _exchangeClient.GetBalances(IntegrationNameRes.HitBtc, CachePolicy.ForceRefresh);
            var ethAvailable = balances.GetAvailableForSymbol("ETH");
            var cossAvailable = balances.GetAvailableForSymbol("COSS");

            var cossCossEthOrderBook = _exchangeClient.GetOrderBook(IntegrationNameRes.Coss, "COSS", "ETH", CachePolicy.ForceRefresh);
            var hitbtcCossEthOrderBook = _exchangeClient.GetOrderBook(IntegrationNameRes.HitBtc, "COSS", "ETH", CachePolicy.ForceRefresh);

            var hitbtcCossEthBestBidPrice = hitbtcCossEthOrderBook.BestBid().Price;
            if (hitbtcCossEthBestBidPrice <= 0) { throw new ApplicationException("HitBTC's best COSS-ETH bid price should be > 0."); }

            var hitbtcCossEthBestAskPrice = hitbtcCossEthOrderBook.BestAsk().Price;
            if (hitbtcCossEthBestAskPrice <= 0) { throw new ApplicationException("HitBTC's best COSS-ETH bid price should be > 0."); }

            if (hitbtcCossEthBestBidPrice >= hitbtcCossEthBestAskPrice)
            {
                throw new ApplicationException("HitBTC's best COSS-ETH bid price should be less than HitBTC's best COSS-ETH ask price.");
            }

            var cossCossEthBestBidPrice = cossCossEthOrderBook.BestBid().Price;
            if (cossCossEthBestBidPrice <= 0) { throw new ApplicationException("Coss's best COSS-ETH bid price should be > 0."); }

            var cossCossEthBestAskPrice = cossCossEthOrderBook.BestAsk().Price;
            if (cossCossEthBestAskPrice <= 0) { throw new ApplicationException("Coss's best COSS-ETH ask price should be > 0."); }

            if (cossCossEthBestBidPrice >= cossCossEthBestAskPrice)
            {
                throw new ApplicationException("Coss's best COSS-ETH bid price should be less than Coss's best COSS-ETH ask price.");
            }

            decimal? bidPrice = null;
            var optimalPrice = MathUtil.Truncate(cossCossEthBestBidPrice * (100.0m - OptimalPercentDiff) / 100.0m, PriceDecimals);
            if (optimalPrice > hitbtcCossEthBestBidPrice)
            {
                bidPrice = optimalPrice;
            }
            else
            {
                var tickUpBidPrice = hitbtcCossEthBestBidPrice + PriceTick;
                var diff = cossCossEthBestBidPrice - tickUpBidPrice;
                var ratio = diff / cossCossEthBestBidPrice;
                var percentDiff = 100.0m * ratio;

                if (percentDiff >= MinBidPercentDiff)
                {
                    bidPrice = tickUpBidPrice;
                }
            }

            if (bidPrice.HasValue)
            {
                var price = bidPrice.Value;

                // the fee is 0.1%, but for some reason it's acting like 0.27%.
                // var maxPossibleQuantity = ethAvailable / price / 1.0027m;

                // shaving off a little more than needed.
                var maxPossibleQuantity = ethAvailable / price / 1.1m;
                decimal quantity;
                if (maxPossibleQuantity >= DesiredQuantity) { quantity = DesiredQuantity; }
                else { quantity = MathUtil.ConstrainToMultipleOf(maxPossibleQuantity, LotSize); }
                if (quantity >= MinQuantity)
                {
                    _log.Info($"About to place a bid on hitbtc for {quantity} {Symbol} at {price} {BaseSymbol}.");
                    try
                    {
                        var orderResult = _exchangeClient.BuyLimit(IntegrationNameRes.HitBtc, Symbol, BaseSymbol, new QuantityAndPrice
                        {
                            Quantity = quantity,
                            Price = price
                        });

                        if (orderResult)
                        {
                            _log.Info($"Successfully placed a bid on hitbtc for {quantity} {Symbol} at {price} {BaseSymbol}.");
                        }
                        else
                        {
                            _log.Error($"Failed to place a bid on hitbtc for {quantity} {Symbol} at {price} {BaseSymbol}.");
                        }
                    }
                    catch (Exception exception)
                    {
                        _log.Error($"Failed to place a bid on hitbtc for {quantity} {Symbol} at {price} {BaseSymbol}.{Environment.NewLine}{exception.Message}");
                        _log.Error(exception);
                    }
                }
            }

            decimal? askPrice = null;
            var optimalAskPrice = MathUtil.RoundUp(cossCossEthBestAskPrice * 1.10m, PriceDecimals);
            if (optimalAskPrice < hitbtcCossEthBestAskPrice)
            {
                askPrice = optimalAskPrice;
            }
            else
            {
                var tickDownAskPrice = hitbtcCossEthBestAskPrice - PriceTick;
                var diff = tickDownAskPrice - cossCossEthBestAskPrice;
                var ratio = diff / cossCossEthBestAskPrice;
                var percentDiff = 100.0m * ratio;
                if (percentDiff >= MinAskPercentDiff)
                {
                    askPrice = tickDownAskPrice;
                }
            }

            var askQuantity = MathUtil.ConstrainToMultipleOf(cossAvailable, LotSize);
            if (askQuantity > MaxAskQuantity)
            {
                askQuantity = MaxAskQuantity;
            }

            if (askPrice.HasValue && askPrice.Value > 0 && askQuantity > MinQuantity)
            {
                var price = askPrice.Value;
                var quantity = askQuantity;

                _log.Info($"About to place an ask on hitbtc for {quantity} {Symbol} at {price} {BaseSymbol}.");
                try
                {
                    var orderResult = _exchangeClient.SellLimit(IntegrationNameRes.HitBtc, Symbol, BaseSymbol, new QuantityAndPrice
                    {
                        Quantity = quantity,
                        Price = price
                    });

                    if (orderResult)
                    {
                        _log.Info($"Successfully placed an ask on hitbtc for {quantity} {Symbol} at {price} {BaseSymbol}.");
                    }
                    else
                    {
                        _log.Error($"Failed to place an ask on hitbtc for {quantity} {Symbol} at {price} {BaseSymbol}.");
                    }
                }
                catch (Exception exception)
                {
                    _log.Error($"Failed to place an ask on hitbtc for {quantity} {Symbol} at {price} {BaseSymbol}.{Environment.NewLine}{exception.Message}");
                    _log.Error(exception);
                }
            }
        }
    }
}
