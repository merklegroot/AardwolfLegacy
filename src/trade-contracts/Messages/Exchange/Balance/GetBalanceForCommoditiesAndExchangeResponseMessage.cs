using System.Collections.Generic;
using trade_contracts.Models;

namespace trade_contracts.Messages.Exchange.Balance
{
    public class GetBalanceForCommoditiesAndExchangeResponseMessage : ResponseMessage
    {
        public ResponsePayload Payload { get; set; }

        public class ResponsePayload
        {
            public List<BalanceContractWithAsOf> Balances { get; set; }
        }
    }
}
