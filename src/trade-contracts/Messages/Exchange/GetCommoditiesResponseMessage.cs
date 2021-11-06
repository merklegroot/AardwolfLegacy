using System.Collections.Generic;

namespace trade_contracts.Messages.Exchange
{
    public class GetCommoditiesResponseMessage : ResponseMessage
    {
        public List<CommodityWithExchangesContract> Payload { get; set; }
    }
}
