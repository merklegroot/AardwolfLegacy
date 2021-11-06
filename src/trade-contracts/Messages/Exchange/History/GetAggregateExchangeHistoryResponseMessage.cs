using System;
using System.Collections.Generic;
using trade_contracts.Models;

namespace trade_contracts.Messages.Exchange
{
    public class GetAggregateExchangeHistoryResponseMessage : ResponseMessage
    {
        public ResponsePayload Payload { get; set; }

        public class ResponsePayload
        {
            public Dictionary<string, DateTime?> AsOfUtcByExchange { get; set; }
            public List<HistoryItemWithExchangeContract> History { get; set; }
        }
    }
}
