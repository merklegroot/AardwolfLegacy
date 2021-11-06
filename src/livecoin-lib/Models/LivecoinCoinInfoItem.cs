namespace livecoin_lib.Models
{
    public class LivecoinCoinInfoItem
    {
        //"name": "Bitcoin",
        public string Name { get; set; }

        //"symbol": "BTC",
        public string Symbol { get; set; }

        //"walletStatus": "normal",
        public string WalletStatus { get; set; }

        //"withdrawFee": 0.0005,
        public decimal? WithdrawFee { get; set; }

        //"difficulty": 3839316899029.7,
        public decimal? Difficulty { get; set; }

        //"minDepositAmount": 0,
        public decimal? MinDepositAmount { get; set; }

        //"minWithdrawAmount": " 0.002",
        public decimal? MinWithdrawAmount { get; set; }

        //"minOrderAmount": 0.0001
        public decimal? MinOrderAmount { get; set; }
    }
}
