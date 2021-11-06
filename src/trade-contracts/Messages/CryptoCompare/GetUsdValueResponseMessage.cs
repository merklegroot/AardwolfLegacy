namespace trade_contracts.Messages.CryptoCompare
{
    public class GetUsdValueResponseMessage : ResponseMessage
    {
        public UsdValueResult Payload { get; set; }
    }
}
