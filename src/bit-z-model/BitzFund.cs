using MongoDB.Bson.Serialization.Attributes;
using trade_model;

namespace bit_z_model
{
    [BsonIgnoreExtraElements]
    public class BitzFund
    {
        public string Symbol { get; set; }
        public string Name { get; set; }
        public decimal? TotalBalance { get; set; }
        public decimal? AvailableBalance { get; set; }
        public decimal? FrozenBalance { get; set; }
        public decimal? BtcBalanceValue { get; set; }
        public string DepositLink { get; set; }
        public string WithdrawLink { get; set; }
        public bool CanDeposit { get; set; }
        public bool CanWithdraw { get; set; }

        public DepositAddress DepositAddress { get; set; }
    }
}
