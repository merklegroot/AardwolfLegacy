using System.Collections.Generic;

namespace coinbase_lib.Models
{
    public class CoinbaseAccountWithTranscations
    {
        public string AccountId { get; set; }
        public List<CoinbaseTransaction> Transactions { get; set; }
    }
}
