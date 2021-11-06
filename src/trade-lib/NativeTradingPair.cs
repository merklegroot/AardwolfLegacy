namespace trade_lib
{
    public class NativeTradingPair
    {
        public NativeTradingPair() { }
        public NativeTradingPair(string symbol, string baseSymbol)
        {
            Symbol = symbol;
            BaseSymbol = baseSymbol;
        }

        public string Symbol { get; set; }
        public string BaseSymbol { get; set; }
    }
}
