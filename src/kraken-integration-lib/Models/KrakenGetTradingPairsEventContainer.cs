using System.Collections.Generic;
using trade_lib;

namespace kraken_integration_lib.Models
{
    public class KrakenGetTradingPairsEventContainer : EventContainerWithContext<KrakenGetTradingPairsContext, List<KrakenGetTradingPairsResponse>>
    {
    }
}
