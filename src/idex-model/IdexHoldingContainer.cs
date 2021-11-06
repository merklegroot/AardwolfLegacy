using MongoDB.Bson;
using System;
using System.Collections.Generic;

namespace idex_model
{
    public class IdexHoldingContainer
    {
        public ObjectId Id { get; set; }
        public DateTime TimeStampUtc { get; set; }
        public List<IdexHolding> Holdings { get; set; }
    }
}
