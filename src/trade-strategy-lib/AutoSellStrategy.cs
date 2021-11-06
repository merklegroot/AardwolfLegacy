using System;
using System.Collections.Generic;
using System.Linq;
using trade_model;

namespace trade_browser_lib
{
    public interface IAutoSellStrategy
    {
        QuantityAndPrice Execute(
            decimal ownedQuantity,
            OrderBook lowVolumeOrderBook,
            OrderBook highVolumeOrderBook,
            decimal minimumTrade,
            int? lotSize = null);
    }

    public class AutoSellStrategy : IAutoSellStrategy
    {
        public QuantityAndPrice Execute(
            decimal ownedQuantity,
            OrderBook lowVolumeOrderBook,
            OrderBook highVolumeOrderBook,
            decimal minimumTrade,
            int? lotSize = null
            )
        {
            if (ownedQuantity <= 0) { return null; }

            var compBestAsk = highVolumeOrderBook.BestAsk();
            var compBestAskPrice = compBestAsk.Price;

            var lowVolumeBestBid = lowVolumeOrderBook.BestBid();
            var lowVolumeBestBidPrice = lowVolumeBestBid.Price;
            var lowVolumeBestBidQuantity = lowVolumeBestBid.Quantity;

            if (lowVolumeBestBidPrice <= compBestAskPrice) { return new QuantityAndPrice { Quantity = 0 }; }

            var quantityToSell = lowVolumeBestBidQuantity <= ownedQuantity ? lowVolumeBestBidQuantity : ownedQuantity;
            var valuetoSell = quantityToSell * lowVolumeBestBidPrice;
            if (valuetoSell < minimumTrade)
            {
                var minimumQuantityToSell = minimumTrade / lowVolumeBestBidPrice;
                if (minimumQuantityToSell > ownedQuantity) { return new QuantityAndPrice { Quantity = 0 }; }
                quantityToSell = minimumQuantityToSell;
            }

            if (quantityToSell > 0 && lotSize.HasValue && lotSize.Value > 0)
            {
                quantityToSell = RoundDown(quantityToSell, lotSize.Value);
            }

            return new QuantityAndPrice { Quantity = quantityToSell, Price = lowVolumeBestBidPrice };
        }

        private static int RoundDown(decimal quantity, int lotSize)
        {
            return ((int)quantity) / lotSize * lotSize;            
        }

        public class AutoSellMultipleAction
        {
            public QuantityAndPrice EthQuantityAndPrice { get; set; }
            public QuantityAndPrice BtcQuantityAndPrice { get; set; }
        }

        private class BidEx
        {
            public Order Order { get; set; }
            public string BaseCommodity { get; set; }
            public decimal ProfitRatio { get; set; }
        }

        public AutoSellMultipleAction ExecuteWithMultipleBaseSymbols(
            decimal ownedQuantity,
            OrderBook lowVolumeEthOrderBook,
            OrderBook highVolumeEthOrderBook,
            decimal minimumEthTrade,
            OrderBook lowVolumeBtcOrderBook,
            OrderBook highVolumeBtcOrderBook,
            decimal minimumBtcTrade,
            int? lotSize = null
            )
        {
            if (ownedQuantity < 0) { throw new ArgumentException(nameof(ownedQuantity)); }
            if (ownedQuantity == 0) { return new AutoSellMultipleAction(); }

            var compBestEthAsk = highVolumeEthOrderBook?.BestAsk();
            var compBestEthAskPrice = compBestEthAsk?.Price;

            var compBestBtcAsk = highVolumeBtcOrderBook?.BestAsk();
            var compBestBtcAskPrice = compBestBtcAsk?.Price;

            var bids = new List<BidEx>();
            bids.AddRange(lowVolumeEthOrderBook?.Bids?.Select(item => new BidEx { Order = item, BaseCommodity = "ETH" }) ?? new List<BidEx>());
            bids.AddRange(lowVolumeBtcOrderBook?.Bids?.Select(item => new BidEx { Order = item, BaseCommodity = "BTC" }) ?? new List<BidEx>());

            foreach (var bid in bids)
            {
                if (!compBestEthAskPrice.HasValue && bid.BaseCommodity == "ETH") { continue; }
                if (!compBestBtcAskPrice.HasValue && bid.BaseCommodity == "BTC") { continue; }
                var askPrice = bid.BaseCommodity == "ETH" ? compBestEthAskPrice.Value : compBestBtcAskPrice.Value;
                var profit = bid.Order.Price - askPrice;
                bid.ProfitRatio = profit / askPrice;
            }

            var worthwhileBids = bids.Where(item => item.ProfitRatio > 0).OrderByDescending(item => item.ProfitRatio).ToList();

            var remainingOwnedQuantity = ownedQuantity;
            var btcQuantityToSell = 0m;
            var ethQuantityToSell = 0m;
            decimal? btcPriceToTake = 0m;
            decimal? ethPriceToTake = 0m;
            foreach (var bid in worthwhileBids)
            {
                var quantityToSell = remainingOwnedQuantity > bid.Order.Quantity ? bid.Order.Quantity : remainingOwnedQuantity;
                if (quantityToSell > 0)
                {
                    if (bid.BaseCommodity == "ETH")
                    {
                        ethQuantityToSell += quantityToSell;
                        ethPriceToTake = bid.Order.Price;
                    }
                    else
                    {
                        btcQuantityToSell += quantityToSell;
                        btcPriceToTake = bid.Order.Price;
                    }

                    remainingOwnedQuantity -= quantityToSell;
                }

                if (remainingOwnedQuantity <= 0) { break; }
            }

            if (ethQuantityToSell > 0 && ethPriceToTake.HasValue && (ethQuantityToSell * ethPriceToTake.Value < minimumEthTrade))
            {
                ethQuantityToSell = 0;
            }

            if (btcQuantityToSell > 0 && btcPriceToTake.HasValue && (btcQuantityToSell * btcPriceToTake.Value < minimumBtcTrade))
            {
                btcQuantityToSell = 0;
            }

            if (lotSize.HasValue && lotSize.Value > 0)
            {
                if (btcQuantityToSell > 0)
                {
                    btcQuantityToSell = RoundDown(btcQuantityToSell, lotSize.Value);
                }

                if (ethQuantityToSell > 0)
                {
                    ethQuantityToSell = RoundDown(ethQuantityToSell, lotSize.Value);
                }
            }

            return new AutoSellMultipleAction
            {
                BtcQuantityAndPrice = btcQuantityToSell > 0 && btcPriceToTake.HasValue ? new QuantityAndPrice { Quantity = btcQuantityToSell, Price = btcPriceToTake.Value } : null,
                EthQuantityAndPrice = ethQuantityToSell > 0 && ethPriceToTake.HasValue ? new QuantityAndPrice { Quantity = ethQuantityToSell, Price = ethPriceToTake.Value } : null
            };
        }
    }
}
