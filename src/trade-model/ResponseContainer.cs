using MongoDB.Bson;
using System;

namespace trade_model
{
    public class ResponseContainer
    {
        public ObjectId Id { get; set; }
        public DateTime RequestTimeUtc { get; set; }
        public DateTime ResponseTimeUtc { get; set; }
        public string Contents { get; set; }
    }
}
