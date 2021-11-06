using System.Collections.Generic;

namespace trade_contracts.Messages.Exchange
{
    public class GetOpenOrdersResponseMessage : ResponseMessage
    {
        public List<OpenOrderForTradingPairContract> Payload { get; set; }
    }
}
