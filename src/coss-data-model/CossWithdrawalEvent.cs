using MongoDB.Bson;
using System;

namespace coss_lib.Models
{
    public class CossWithdrawalEvent
    {
        public ObjectId Id { get; set; }
        public ObjectId RelatedEventId { get; set; }
        public DateTime TimeStampUtc { get; set; }
        public string Symbol { get; set; }
        public decimal Quantity { get; set; }
        public string DestinationAddress { get; set; }

        public string EventType { get; set; }

        public const string CreateRequestEventType = "CreateRequest";
        public const string CommitRequestEventType = "CommitRequest";
    }
}
