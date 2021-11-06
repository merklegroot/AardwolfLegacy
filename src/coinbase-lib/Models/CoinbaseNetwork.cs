using Newtonsoft.Json;

namespace coinbase_lib.Models
{
    public class CoinbaseNetwork
    {
        public string Status { get; set; }
        public string Hash { get; set; }

        [JsonProperty("transaction_fee")]
        public CoinbaseAmount TransactionFee { get; set; }

        [JsonProperty("transaction_amount")]
        public CoinbaseAmount TransactionAmount { get; set; }

        public long? Confirmations { get; set; }
    }
}
