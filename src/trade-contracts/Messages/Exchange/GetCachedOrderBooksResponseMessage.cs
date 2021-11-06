using System.Collections.Generic;

namespace trade_contracts.Messages.Exchange
{
    public class GetCachedOrderBooksResponseMessage : ResponseMessage
    {
        public List<OrderBookAndTradingPairContract> Payload { get; set; }
    }
}
