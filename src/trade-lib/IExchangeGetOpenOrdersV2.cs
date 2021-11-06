using cache_lib.Models;
using System.Collections.Generic;
using trade_model;

namespace trade_lib
{
    public interface IExchangeGetOpenOrdersV2 : IExchangeGetOpenOrdersForTradingPairV2
    {
        List<OpenOrdersForTradingPair> GetOpenOrdersV2();        
    }
}
