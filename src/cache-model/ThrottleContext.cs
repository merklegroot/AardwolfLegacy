using System;

namespace cache_lib.Models
{
    public class ThrottleContext
    {
        public object Locker { get; set; }
        public TimeSpan ThrottleThreshold { get; set; }
        public DateTime? LastReadTime { get; set; }
    }
}
