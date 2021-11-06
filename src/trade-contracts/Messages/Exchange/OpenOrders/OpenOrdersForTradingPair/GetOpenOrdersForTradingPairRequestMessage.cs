using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace trade_contracts.Messages.Exchange
{
    public class GetOpenOrdersForTradingPairRequestMessage : RequestMessage
    {
        public string Exchange { get; set; }
        public string Symbol { get; set; }
        public string BaseSymbol { get; set; }
        public CachePolicyContract CachePolicy { get; set; }
    }
}
