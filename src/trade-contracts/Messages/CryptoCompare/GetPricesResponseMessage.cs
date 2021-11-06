using System.Collections.Generic;

namespace trade_contracts.Messages.CryptoCompare
{
    public class GetPricesResponseMessage : ResponseMessage
    {
        public Dictionary<string, decimal> Payload { get; set; }
    }
}
