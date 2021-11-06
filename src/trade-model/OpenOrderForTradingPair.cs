namespace trade_model
{
    public class OpenOrderForTradingPair : OpenOrder
    {
        public string Symbol { get; set; }
        public string BaseSymbol { get; set; }
    }
}
