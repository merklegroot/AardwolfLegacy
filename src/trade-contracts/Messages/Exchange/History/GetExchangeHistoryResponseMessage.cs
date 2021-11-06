using System;
using System.Collections.Generic;

namespace trade_contracts.Messages.Exchange
{
    public class GetExchangeHistoryResponseMessage : ResponseMessage
    {
        public ResponsePayload Payload { get; set; }

        public class ResponsePayload
        {
            public DateTime? AsOfUtc { get; set; }
            public List<HistoryItemContract> History { get; set; }
        }
    }
}
