using System.Collections.Generic;

namespace trade_contracts.Messages.Exchange
{
    public class GetCryptoCompareSymbolsResponseMessage : ResponseMessage
    {
        public List<string> Symbols { get; set; }
    }
}
