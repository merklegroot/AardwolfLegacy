using MongoDB.Bson;
using System;
using System.Collections.Generic;

namespace coss_data_model
{
    public class CossOpenOrdersForTradingPairContainer
    {
        public ObjectId Id { get; set; }
        public DateTime TimeStampUtc { get; set; }
        public string Symbol { get; set; }
        public string BaseSymbol { get; set; }
        public List<CossOpenOrder> OpenOrders { get; set; }
    }
}
