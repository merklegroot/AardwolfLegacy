using System.Collections.Generic;
using trade_contracts.Models.OpenOrders;

namespace trade_contracts.Messages.Exchange.OpenOrders
{
    public class GetOpenOrdersResponseMessageV2 : ResponseMessage
    {
        public ResponsePayload Payload { get; set; }

        public class ResponsePayload
        {
            public List<OpenOrdersForTradingPairContract> OpenOrdersForTradingPairs { get; set; }
        }
    }
}
