namespace trade_contracts.Messages.Exchange
{
    public class GetArbResponseMessage : ResponseMessage
    {
        public ArbitrageResultContract Result { get; set; }
    }
}
