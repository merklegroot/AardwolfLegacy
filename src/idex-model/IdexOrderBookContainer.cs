using MongoDB.Bson;
using System;
using trade_model;

namespace idex_model
{
    public class IdexOrderBookContainer
    {
        public ObjectId Id { get; set; }
        public DateTime TimeStampUtc { get; set; }
        public string Symbol { get; set; }
        public string BaseSymbol { get; set; }
        public OrderBook OrderBook { get; set; }
    }
}
