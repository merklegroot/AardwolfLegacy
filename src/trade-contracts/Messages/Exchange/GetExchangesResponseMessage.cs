using System.Collections.Generic;

namespace trade_contracts.Messages.Exchange
{
    public class GetExchangesResponseMessage : ResponseMessage
    {
        public List<ExchangeContract> Exchanges { get; set; }
    }
}
