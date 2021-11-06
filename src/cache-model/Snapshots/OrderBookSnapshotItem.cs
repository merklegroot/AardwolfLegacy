using MongoDB.Bson;
using System;

namespace cache_lib.Models.Snapshots
{
    public class OrderBookSnapshotItem : ISnapshotItem
    {
        public ObjectId Id { get; set; }
        public string CacheKey { get; set; }
        public string Symbol { get; set; }
        public string BaseSymbol { get; set; }
        public DateTime AsOfUtc { get; set; }
        public string Raw { get; set; }
    }
}
