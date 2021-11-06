using MongoDB.Bson;
using System;
using System.Collections.Generic;

namespace coss_lib.Models
{
    public class CossResponseAndUrlContainer<T>
    {
        public ObjectId Id { get; set; }
        public DateTime TimeStampUtc { get; set; }
        public List<CossResponseAndUrl<T>> Responses { get; set; }
    }
}
