using trade_contracts.Messages;

namespace trade_contracts
{
    public class WithdrawFundsRequestMessage : MessageBase
    {
        public CommodityContract Commodity { get; set; }
        public decimal Quantity { get; set; }
        public string DepositAddress { get; set; }
    }
}
