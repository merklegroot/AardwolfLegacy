namespace cryptopia_lib.Models
{
    public class CryptopiaMarketOrder
    {
        public long TradePairId { get; set; }
        public string Label { get; set; }
        public decimal Price { get; set; }
        public decimal Volume { get; set; }
        public decimal Total { get; set; }
    }
}
