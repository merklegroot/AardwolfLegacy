using System;

namespace cache_lib.Models
{
    public class MemCacheItem<T>
        where T : class
    {
        public T Value { get; set; } = null;
        public DateTime? TimeStampUtc { get; set; } = null;
    }
}
