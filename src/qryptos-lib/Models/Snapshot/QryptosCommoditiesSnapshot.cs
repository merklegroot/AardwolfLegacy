using MongoDB.Bson;
using System;
using System.Collections.Generic;
using trade_model;

namespace qryptos_lib.Models.Snapshot
{
    public class QryptosCommoditiesSnapshot
    {
        public ObjectId Id { get; set; }
        public DateTime TimeStampUtc { get; set; }
        public DateTime MapTimeStampUtc { get; set; }
        public List<CommodityForExchange> ExchangeCommodities { get; set; }
    }
}
