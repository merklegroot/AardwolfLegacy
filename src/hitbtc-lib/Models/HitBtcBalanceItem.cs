namespace hitbtc_lib.Models
{
    public class HitBtcBalanceItem
    {
        public string Currency { get; set; }
        public decimal Available { get; set; }
        public decimal Reserved { get; set; }
    }
}
