using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;

namespace trade_model
{
    public class OpenOrderInfo
    {
        [BsonId]
        public ObjectId Id { get; set; }

        public string Symbol { get; set; }
        public string BaseSymbol { get; set; }
        public Guid ExchangeId { get; set; }
        public string ExchangeName { get; set; }        
        public List<OpenOrder> OpenOrders { get; set; }

        public DateTime RequestTimeUtc { get; set; }
        public DateTime ResponseTimeUtc { get; set; }
    }
}
