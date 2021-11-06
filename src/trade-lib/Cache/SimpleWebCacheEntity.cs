using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace trade_lib.Cache
{
    [Obsolete]
    [BsonIgnoreExtraElements]
    public class SimpleWebCacheEntity
    {
        public ObjectId Id { get; set; }
        public DateTime RequestTimeUtc { get; set; }
        public DateTime ResponseTimeUtc { get; set; }
        public string Url { get; set; }
        public string UpperUrl { get; set; }
        public string Contents { get; set; }
        public string Group { get; set; }
    }
}
