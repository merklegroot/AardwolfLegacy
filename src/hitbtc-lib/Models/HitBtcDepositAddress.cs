using Newtonsoft.Json;

namespace hitbtc_lib.Models
{
    public class HitBtcDepositAddress
    {
        [JsonProperty("address")]
        public string Address { get; set; }

        [JsonProperty("paymentId")]
        public string PaymentId { get; set; }
    }
}
