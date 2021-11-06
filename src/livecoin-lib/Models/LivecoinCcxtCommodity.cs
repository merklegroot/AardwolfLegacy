namespace livecoin_lib.Models
{
    public class LivecoinCcxtCommodity
    {
        public string id { get; set; }
        public string code { get; set; }
        public Info info { get; set; }
        public string name { get; set; }
        public bool active { get; set; }
        public string status { get; set; }
        public decimal fee { get; set; }
        public int? precision { get; set; }
        public Limits limits { get; set; }

        public class Info
        {
            public string name { get; set; }
            public string symbol { get; set; }
            public string walletStatus { get; set; }
            public decimal? withdrawFee { get; set; }
            public decimal? difficulty { get; set; }
            public int? minDepositAmount { get; set; }
            public string minWithdrawAmount { get; set; }
            public decimal? minOrderAmount { get; set; }
        }

        public class Amount
        {
            public decimal min { get; set; }
            public decimal? max { get; set; }
        }

        public class Price
        {
            public decimal? min { get; set; }
            public decimal? max { get; set; }
        }

        public class Cost
        {
            public decimal? min { get; set; }
        }

        public class Withdraw
        {
            public decimal? min { get; set; }
            public decimal? max { get; set; }
        }

        public class Deposit
        {
            public decimal? min { get; set; }
        }

        public class Limits
        {
            public Amount amount { get; set; }
            public Price price { get; set; }
            public Cost cost { get; set; }
            public Withdraw withdraw { get; set; }
            public Deposit deposit { get; set; }
        }
    }
}
