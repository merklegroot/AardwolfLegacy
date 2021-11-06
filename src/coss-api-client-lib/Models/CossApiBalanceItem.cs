using Newtonsoft.Json;

namespace coss_api_client_lib.Models
{
    public class CossApiBalanceItem
    {
		//"currency_code": "XEM",
        [JsonProperty("currency_code")]
        public string CurrencyCode { get; set; }

        //"address": "",
        [JsonProperty("address")]
        public string Address { get; set; }

        //"total": "9.76240256",
        [JsonProperty("total")]
        public decimal Total { get; set; }

        //"available": "9.76240256",
        [JsonProperty("available")]
        public decimal Available { get; set; }

        //"in_order": "0",
        [JsonProperty("in_order")]
        public decimal InOrder { get; set; }

        //"memo": null
        [JsonProperty("memo")]
        public string Memo { get; set; }
    }
}
