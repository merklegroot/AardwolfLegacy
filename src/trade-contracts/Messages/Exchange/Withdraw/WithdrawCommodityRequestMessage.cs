namespace trade_contracts.Messages.Exchange.Withdraw
{
    public class WithdrawCommodityRequestMessage : RequestMessage
    {
        public RequestPayload Payload { get; set; }

        public class RequestPayload
        {
            public string SourceExchange { get; set; }
            public string Symbol { get; set; }
            public decimal Quantity { get; set; }
            public DepositAddressContract DepositAddress { get; set; }
        }
    }
}
