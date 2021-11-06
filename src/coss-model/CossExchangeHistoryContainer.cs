using MongoDB.Bson;
using System;
using System.Collections.Generic;

namespace coss_lib.Models
{
    public class CossExchangeHistoryContainer
    {
        public ObjectId Id { get; set; }
        public DateTime TimeStampUtc { get; set; }
        public List<CossExchangeHistoryItem> History { get; set; }
    }
}
