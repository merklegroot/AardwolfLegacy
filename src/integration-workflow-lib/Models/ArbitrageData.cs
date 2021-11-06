using System.Collections.Generic;
using trade_model;

namespace integration_workflow_lib.Models
{
    public class ArbitrageData
    {
        public class OrderBookAndBaseSymbol
        {
            public OrderBook OrderBook { get; set; }
            public string BaseSymbol { get; set; }            

            public OrderBookAndBaseSymbol(OrderBook orderBook, string baseSymbol)
            {
                OrderBook = orderBook;
                BaseSymbol = baseSymbol;
            }

            public OrderBookAndBaseSymbol() { }
        }

        public decimal EthToBtcRatio { get; set; }
        public decimal BtcToUsdRatio { get; set; }
        public decimal? SourceWithdrawalFee { get; set; }
        public OrderBook SourceEthOrderBook { get; set; }
        public OrderBook SourceBtcOrderBook { get; set; }

        public List<OrderBookAndBaseSymbol> SourceOrderBooks
        {
            get
            {
                return new List<OrderBookAndBaseSymbol>
                {
                    new OrderBookAndBaseSymbol(SourceEthOrderBook, "ETH"),
                    new OrderBookAndBaseSymbol(SourceBtcOrderBook, "BTC"),
                };
            }
        }

        public OrderBook DestEthOrderBook { get; set; }
        public OrderBook DestBtcOrderBook { get; set; }

        public List<OrderBookAndBaseSymbol> DestOrderBooks
        {
            get
            {
                return new List<OrderBookAndBaseSymbol>
                {
                    new OrderBookAndBaseSymbol(DestEthOrderBook, "ETH"),
                    new OrderBookAndBaseSymbol(DestBtcOrderBook, "BTC"),
                };
            }
        }
    }
}
