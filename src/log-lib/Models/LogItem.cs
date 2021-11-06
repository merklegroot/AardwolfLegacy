using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace log_lib.Models
{
    [BsonIgnoreExtraElements]
    public class LogItem
    {
        [BsonId]
        public ObjectId Id { get; set; }

        public string Application { get; set; }

        public string Machine { get; set; }

        public DateTime CreatedDateUtc { get; set; } = DateTime.UtcNow;

        public string EventType { get; set; }

        public Guid? CorrelationId { get; set; }

        public string Message { get; set; }

        public string Level { get; set; }

        public string StackTrace { get; set; }

        public string ExceptionType { get; set; }
    }
}
