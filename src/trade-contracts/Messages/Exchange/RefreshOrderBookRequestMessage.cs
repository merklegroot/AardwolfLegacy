using System;

namespace trade_contracts.Messages.Exchange
{
    public class RefreshOrderBookRequestMessage : RequestMessage
    {
        public string Exchange { get; set; }
        public string Symbol { get; set; }
        public string BaseSymbol { get; set; }
    }
}
