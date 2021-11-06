using System;
using System.Collections.Generic;
using trade_browser_lib.Models;
using trade_model;

namespace trade_browser_lib
{
    public interface IOrderManager
    {
        void ManageOrders(TradingPair tradingPair,
            List<OpenOrderEx> myOpenOrders,
            OrderBook cossOrderBook,
            OrderBook binanceOrderBook,
            Action<OpenOrder> placeBid,
            Action<OpenOrder> placeAsk);
    }
}
