using System;

namespace trade_contracts
{
    public class HistoryItemContract
    {
        public string NativeId { get; set; }
        public string Comments { get; set; }
        public string Symbol { get; set; }
        public string BaseSymbol { get; set; }
        public DateTime TimeStampUtc { get; set; }
        public DateTime? SuccessTimeStampUtc { get; set; }
        public decimal Price { get; set; }
        public decimal Quantity { get; set; }
        public decimal FeeQuantity { get; set; }
        public string FeeCommodity { get; set; }

        public TradeTypeEnumContract TradeType { get; set; }
        public string TradeTypeText { get { return TradeType.ToString(); } }

        public TradeStatusEnumContract TradeStatus { get; set; }
        public string TradeStatusText { get { return TradeStatus.ToString(); } }

        public string DestinationExchange { get; set; }
        public string WalletAddress { get; set; }
        public string TransactionHash { get; set; }
    }
}
