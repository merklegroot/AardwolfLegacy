using MongoDB.Bson;
using System;
using System.Collections.Generic;

namespace etherscan_lib.Models
{
    public class EtherscanTransactionHistoryContainer
    {
        public ObjectId Id { get; set; }
        public DateTime TimeStampUtc { get; set; }
        public List<List<string>> Rows { get; set; }
    }
}
