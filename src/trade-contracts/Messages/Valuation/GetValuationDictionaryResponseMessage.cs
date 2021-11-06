using System.Collections.Generic;

namespace trade_contracts.Messages.Valuation
{
    public class GetValuationDictionaryResponseMessage : ResponseMessage
    {
        public ResponsePayload Payload { get; set; }

        public class ResponsePayload
        {
            public Dictionary<string, decimal> ValuationDictionary { get; set; }
        }
    }
}
