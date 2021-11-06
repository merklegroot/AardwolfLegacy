using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;

namespace bit_z_model
{
    [BsonIgnoreExtraElements]
    public class BitzFundsContainer
    {
        public ObjectId Id { get; set; }
        public DateTime TimeStampUtc { get; set; }
        public List<BitzFund> Funds { get; set; }
    }
}
