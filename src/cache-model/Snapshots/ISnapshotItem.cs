using MongoDB.Bson;
using System;

namespace cache_lib.Models
{
    public interface ISnapshotItem
    {
        ObjectId Id { get; set; }
        string CacheKey { get; set; }
        DateTime AsOfUtc { get; set; }
        string Raw { get; set; }
    }
}
