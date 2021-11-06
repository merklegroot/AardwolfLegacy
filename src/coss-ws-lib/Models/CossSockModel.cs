using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace coss_ws_lib.Models
{
    public class CossSockModel
    {
        public ObjectId Id { get; set; }

        [BsonElement("timeStampUtc")]
        public DateTime TimeStampUtc { get; set; }

        [BsonElement("contract")]
        public string Contract { get; set; }

        [BsonElement("messageContents")]
        public string MessageContents { get; set; }
    }
}