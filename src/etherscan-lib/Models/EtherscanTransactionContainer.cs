using MongoDB.Bson;
using System;
using System.Collections.Generic;

namespace etherscan_lib.Models
{
    public class EtherscanTransactionContainer
    {
        public ObjectId Id { get; set; }
        public DateTime TimeStampUtc { get; set; }
        public string TransactionHash { get; set; }
        public List<KeyValuePair<string, string>> Data { get; set; }
    }
}
