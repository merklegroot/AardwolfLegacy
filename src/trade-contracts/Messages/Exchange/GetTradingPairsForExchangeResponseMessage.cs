using System.Collections.Generic;

namespace trade_contracts.Messages.Exchange
{
    public class GetTradingPairsForExchangeResponseMessage : ResponseMessage
    {
        public List<TradingPairContract> TradingPairs { get; set; }
    }
}
