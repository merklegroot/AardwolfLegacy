namespace hitbtc_lib.Models
{
    // https://api.hitbtc.com/api/2/public/currency
    public class HitBtcCurrency
    {
        public string Id { get; set; }
        public string Fullname { get; set; }
        public bool Crypto { get; set; }
        public bool PayinEnabled { get; set; }
        public bool PayinPaymentId { get; set; }
        public int PayinConfirmations { get; set; }
        public bool PayoutEnabled { get; set; }
        public bool PayoutIsPaymentId { get; set; }
        public bool TransferEnabled { get; set; }
        public bool Delisted { get; set; }
        public decimal PayoutFee { get; set; }
    }
}
