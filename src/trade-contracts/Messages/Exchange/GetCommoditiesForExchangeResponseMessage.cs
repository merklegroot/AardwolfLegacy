using System.Collections.Generic;

namespace trade_contracts.Messages.Exchange
{
    public class GetCommoditiesForExchangeResponseMessage : ResponseMessage
    {
        public List<ExchangeCommodityContract> Commodities { get; set; }
    }
}
