using System.Collections.Generic;
using trade_contracts.Models;

namespace trade_contracts.Messages.Exchange
{
    public class GetAggregateDepositAddressesResponseMessage : ResponseMessage
    {
        public ResponsePayload Payload { get; set; }

        public class ResponsePayload
        {
            public List<DepositAddressWithExchangeAndSymbolContract> DepositAddresses { get; set; }
        }
    }
}
