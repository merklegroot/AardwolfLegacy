namespace livecoin_lib.Models
{
    public class LivecoinDepositAddress
    {
        public string Currency { get; set; }
        public string Address { get; set; }
        public string Status { get; set; }

        public LivecoinDepositAddressInfo Info { get; set; }

        public class LivecoinDepositAddressInfo
        {
            public string Fault { get; set; }
            public long UserId { get; set; }
            public string UserName { get; set; }
            public string Currency { get; set; }
            public string Wallet { get; set; }
        }
    }
}
