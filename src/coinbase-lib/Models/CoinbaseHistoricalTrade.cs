using System;

namespace coinbase_lib.Models
{
    public class CoinbaseHistoricalTrade
    {
        public CoinbaseTradeType TradeType { get; set; }
        public CoinbaseQuantityAndCommodity Gave { get; set; }
        public CoinbaseQuantityAndCommodity Got { get; set; }
        public CoinbaseQuantityAndCommodity Fee { get; set; }
        public CoinbaseQuantityAndCommodity Price { get; set; }
        public CoinbaseTradeStatus Status { get; set; }
        public DateTime TimeStampUtc { get; set; }
    }
}
