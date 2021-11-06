using System;

namespace trade_model
{
    public class AsOfWrapper<T>
    {
        public DateTime? AsOfUtc { get; set; }
        public T Data { get; set; }
    }
}
