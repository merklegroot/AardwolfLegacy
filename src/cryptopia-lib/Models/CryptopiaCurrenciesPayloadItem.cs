namespace cryptopia_lib.Models
{
    public class CryptopiaCurrenciesPayloadItem
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Symbol { get; set; }
        public string Algorithm { get; set; }
        public decimal WithdrawFee { get; set; }
        public decimal MinWithdraw { get; set; }
        public decimal MaxWithdraw { get; set; }
        public decimal MinBaseTrade { get; set; }
        public bool IsTipEnabled { get; set; }
        public decimal MinTip { get; set; }
        public long DepositConfirmations { get; set; }
        public string Status { get; set; }
        public string StatusMessage { get; set; }
        public string ListingStatus { get; set; }
    }
}
