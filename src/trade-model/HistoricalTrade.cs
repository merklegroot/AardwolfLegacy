using System;

namespace trade_model
{
    public class HistoricalTrade
    {
        public string NativeId { get; set; }
        public TradingPair TradingPair { get; set; }
        public string Symbol { get; set; }
        public string BaseSymbol { get; set; }
        public DateTime TimeStampUtc { get; set; }
        public DateTime? SuccessTimeStampUtc { get; set; }
        public decimal Price { get; set; }
        public decimal Quantity { get; set; }
        public decimal FeeQuantity { get; set; }
        public string FeeCommodity { get; set; }
        public TradeTypeEnum TradeType { get; set; }
        public TradeStatusEnum TradeStatus { get; set; }
        public string WalletAddress { get; set; }
        public string DestinationExchange { get; set; }
        public string TransactionHash { get; set; }
        public string Comments { get; set; }
    }
}
