using MongoDB.Bson;
using System;

namespace cache_lib.Models.Snapshots
{
    public class DepositAddressSnapshotItem : ISnapshotItem
    {
        public ObjectId Id { get; set; }
        public string CacheKey { get; set; }
        public DateTime AsOfUtc { get; set; }
        public string Raw { get; set; }
    }
}
