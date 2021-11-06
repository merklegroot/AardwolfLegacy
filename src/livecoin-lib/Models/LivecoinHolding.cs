namespace livecoin_lib.Models
{
    public class LivecoinHolding
    {
        public string Currency { get; set; }
        public decimal Total { get; set; }
        public decimal Trade { get; set; }
        public decimal Available { get; set; }
        public decimal AvailableWithdrawal { get; set; }
    }
}
