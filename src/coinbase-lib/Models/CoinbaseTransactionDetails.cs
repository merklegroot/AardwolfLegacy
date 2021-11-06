using Newtonsoft.Json;

namespace coinbase_lib.Models
{
    public class CoinbaseTransactionDetails
    {
        public string Title { get; set; }
        public string SubTitle { get; set; }

        [JsonProperty("payment_method_name")]
        public string PaymentMethodName { get; set; }
    }
}
