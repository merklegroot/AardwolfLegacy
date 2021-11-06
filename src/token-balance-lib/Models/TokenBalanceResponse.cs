using Newtonsoft.Json;

namespace token_balance_lib.Models
{
    internal class TokenBalanceResponse
    {
	    //"name": "Golem Network Token",
        [JsonProperty("name")]
        public string Name { get; set; }

        //"wallet": "0xda0AEd568D9A2dbDcBAFC1576fedc633d28EEE9a",
        [JsonProperty("wallet")]
        public string Wallet { get; set; }

        //"symbol": "GNT",
        [JsonProperty("symbol")]
        public string Symbol { get; set; }

        //"balance": "5401731.086778292432427406",
        [JsonProperty("balance")]
        public decimal Balance { get; set; }

        //"eth_balance": "0.985735366999999973",
        [JsonProperty("eth_balance")]
        public decimal EthBalance { get; set; }

        //"decimals": 18,
        [JsonProperty("decimals")]
        public int Decimals { get; set; }

        //"block": 5898875
        [JsonProperty("block")]
        public string Block { get; set; }
    }
}
