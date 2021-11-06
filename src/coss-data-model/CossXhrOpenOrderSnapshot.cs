using MongoDB.Bson;
using System;
using System.Collections.Generic;

namespace coss_data_model
{
    public class CossXhrOpenOrderSnapshot
    {
        public ObjectId Id { get; set; }
        public ObjectId LastProcessedId { get; set; }
        public DateTime TimeStampUtc { get; set; } = DateTime.UtcNow;

        public Dictionary<string, List<CossXhrOpenOrder>> OpenOrdersByTradingPair { get; set; } = new Dictionary<string, List<CossXhrOpenOrder>>();
    }
}
