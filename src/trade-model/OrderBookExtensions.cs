using System.Linq;

namespace trade_model
{
    public static class OrderBookExtensions
    {
        public static Order BestAsk(this OrderBook orderBook, decimal minimumQuantityThatMatters = 0)
        {
            if (orderBook == null || orderBook.Asks == null || !orderBook.Asks.Any()) { return null; }

            return orderBook.Asks.Where(item => item.Quantity >= minimumQuantityThatMatters).OrderBy(item => item.Price).First();
        }

        public static Order BestBid(this OrderBook orderBook, decimal minimumQuantityThatMatters = 0)
        {
            if (orderBook == null || orderBook.Bids == null || !orderBook.Bids.Any()) { return null; }

            return orderBook.Bids.Where(item => item.Quantity >= minimumQuantityThatMatters).OrderByDescending(item => item.Price).First();
        }

        public static OrderBook Invert(this OrderBook orderBook)
        {
            if (orderBook == null) { return null; }

            var inverted = new OrderBook
            {
                Bids = orderBook.Asks != null ? orderBook.Asks.OrderByDescending(item => item.Price).ToList() : null,
                Asks = orderBook.Bids != null ? orderBook.Bids.OrderBy(item => item.Price).ToList() : null
            };

            return inverted;
        }
    }
}
