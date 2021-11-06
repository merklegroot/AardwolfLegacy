using System;
using System.Collections.Generic;
using trade_contracts.Models.OpenOrders;

namespace trade_contracts.Messages.Exchange.OpenOrders
{
    public class GetOpenOrdersForTradingPairResponseMessageV2 : ResponseMessage
    {
        public ResponsePayload Payload { get; set; }

        public class ResponsePayload
        {
            public DateTime? AsOfUtc { get; set; }
            public List<OpenOrderContract> OpenOrders { get; set; }
        }
    }
}
