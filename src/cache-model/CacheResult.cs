using MongoDB.Bson;
using System;

namespace cache_lib.Models
{
    public class CacheResult
    {
        public ObjectId? Id { get; set; }
        public string Contents { get; set; }
        public DateTime? AsOf { get; set; }
        public TimeSpan? CacheAge { get; set; }
        public bool WasFromCache { get; set; }
    }

    public class CacheResult<T> : CacheResult
    {
        public T Data { get; set; }
    }
}
