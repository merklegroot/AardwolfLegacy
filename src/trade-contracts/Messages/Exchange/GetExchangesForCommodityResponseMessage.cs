using System.Collections.Generic;

namespace trade_contracts.Messages.Exchange
{
    public class GetExchangesForCommodityResponseMessage : ResponseMessage
    {
        public List<string> Payload { get; set; }
    }
}
