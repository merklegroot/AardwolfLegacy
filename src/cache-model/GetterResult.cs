using System;

namespace cache_lib.Models
{
    public class GetterResult<T>
    {
        public DateTime StartTime { get; set; }
        public T Data { get; set; }
        public DateTime EndTime { get; set; }
    }
}
