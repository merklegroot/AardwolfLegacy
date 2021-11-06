using System;

namespace cache_lib.Models
{
    public class RefreshCacheResult
    {
        public bool WasRefreshed { get; set; }
        public DateTime? AsOf { get; set; }
        public TimeSpan? CacheAge { get; set; }
    }
}
