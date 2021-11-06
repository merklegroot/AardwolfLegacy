using MongoDB.Bson.Serialization.Attributes;

namespace trade_model
{
    [BsonIgnoreExtraElements]
    public class DepositAddress
    {
        public string Address { get; set; }
        public string Memo { get; set; }
    }
}
