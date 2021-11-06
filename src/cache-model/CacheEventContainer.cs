using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace cache_lib.Models
{
    public class CacheEventContainer
    {
        public ObjectId Id { get; set; }

        [BsonElement("cacheKey")]
        public string CacheKey { get; set; }
        
        public DateTime StartTimeUtc { get; set; }
        
        public DateTime EndTimeUtc { get; set; }
        
        public string Raw { get; set; }
    }
}
