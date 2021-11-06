using MongoDB.Bson;
using System;

namespace coss_lib.Models
{
    public class CossCreateOrderEvent
    {
        public ObjectId Id { get; set; }

        public DateTime RequestTimeStampUtc { get; set; }

        public DateTime ResponseTimeStampUtc { get; set; }
        
        public bool IsBid { get; set; }

        public string Symbol { get; set; }

        public string BaseSymbol { get; set; }

        public decimal RequestQuantity { get; set; }

        public decimal RequestPrice { get; set; }

        public string Response { get; set; }
    }
}
