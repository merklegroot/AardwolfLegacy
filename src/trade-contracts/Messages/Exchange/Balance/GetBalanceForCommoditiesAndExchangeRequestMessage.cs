using System.Collections.Generic;

namespace trade_contracts.Messages.Exchange.Balance
{
    public class GetBalanceForCommoditiesAndExchangeRequestMessage : RequestMessage
    {
        public RequestPayload Payload { get; set; }

        public class RequestPayload
        {
            public List<string> Symbols { get; set; }
            public string Exchange { get; set; }
            public CachePolicyContract CachePolicy { get; set; }
        }
    }
}
