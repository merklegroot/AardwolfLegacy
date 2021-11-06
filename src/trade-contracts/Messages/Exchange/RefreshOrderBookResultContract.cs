using System;

namespace trade_contracts.Messages.Exchange
{
    public class RefreshOrderBookResultContract
    {
        public bool WasRefreshed { get; set; }
        public DateTime? AsOf { get; set; }
        public TimeSpan? CacheAge { get; set; }
    }
}
