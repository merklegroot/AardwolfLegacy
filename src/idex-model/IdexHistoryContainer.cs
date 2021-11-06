using MongoDB.Bson;
using System;
using System.Collections.Generic;

namespace idex_model
{
    public class IdexHistoryContainer
    {
        public ObjectId Id { get; set; }
        public DateTime TimeStampUtc { get; set; }
        public string ClientTimeZone { get; set; }
        public List<IdexHistoryItem> HistoryItems { get; set; }
    }
}
