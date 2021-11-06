using MongoDB.Bson;
using System;
using System.Collections.Generic;

namespace bit_z_model
{
    public class BitzTradeHistoryContainer
    {
        public ObjectId Id { get; set; }
        public DateTime TimeStampUtc { get; set; }
        public List<BitzTradeHistoryItem> History { get; set; }
    }
}
