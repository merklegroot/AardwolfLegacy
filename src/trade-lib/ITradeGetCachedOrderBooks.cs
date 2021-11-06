using System.Collections.Generic;
using trade_model;

namespace trade_lib
{
    public interface ITradeGetCachedOrderBooks
    {
        List<OrderBookAndTradingPair> GetCachedOrderBooks();
    }
}
