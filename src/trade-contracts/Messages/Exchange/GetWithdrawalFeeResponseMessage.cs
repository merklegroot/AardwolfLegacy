namespace trade_contracts.Messages.Exchange
{
    public class GetWithdrawalFeeResponseMessage : ResponseMessage
    {
        public class GetWithdrawalFeePayload
        {
            public decimal? WithdrawalFee { get; set; }
        }

        public GetWithdrawalFeePayload Payload { get; set; }
    }
}
