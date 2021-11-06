namespace cryptopia_lib.Models
{
    public class CryptopiaTradingPairItem
    {
        public long Id { get; set; }
        public string Label { get; set; }
        public string Currency { get; set; }
        public string Symbol { get; set; }
        public string BaseCurrency { get; set; }
        public string BaseSymbol { get; set; }
        public string Status { get; set; }
        public string StatusMessage { get; set; }
        public decimal TradeFee { get; set; }
        public decimal MinimumTrade { get; set; }
        public decimal MaximumTrade { get; set; }
        public decimal MinimumBaseTrade { get; set; }
        public decimal MaximumBaseTrade { get; set; }
        public decimal MinimumPrice { get; set; }
        public decimal MaximumPrice { get; set; }

    }
}
