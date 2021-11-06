namespace trade_contracts.Messages.Exchange.History
{
    public class GetHistoryForTradingPairResponseMessage : ResponseMessage
    {
        public class ResponsePayload
        {
        }

        public ResponsePayload Payload { get; set; }
    }
}
