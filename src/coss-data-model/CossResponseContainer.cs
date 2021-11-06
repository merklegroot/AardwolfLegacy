using MongoDB.Bson;
using System;

namespace coss_data_model
{
    public class CossResponseContainer<T>
    {
        public ObjectId Id { get; set; }
        public DateTime TimeStampUtc { get; set; }
        public string Url { get; set; }
        public string Type { get; set; } = typeof(CossResponseContainer<T>).FullName;
        public T Response { get; set; }
    }
}
