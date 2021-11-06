using cache_lib.Models;
using MongoDB.Bson;
using System;

namespace coss_lib.Models
{
    public class CossNativeUserTradeHistorySnapshotItem : ISnapshotItem
    {
        public ObjectId Id { get; set; }
        public string CacheKey { get; set; }
        public DateTime AsOfUtc { get; set; }
        public string Raw { get; set; }

        public string Symbol { get; set; }
        public string BaseSymbol { get; set; }
    }
}
