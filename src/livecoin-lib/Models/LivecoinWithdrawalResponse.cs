using Newtonsoft.Json;

namespace livecoin_lib.Models
{
    public class LivecoinWithdrawalResponse
    {
        [JsonProperty("fault")]
        public string Fault { get; set; }

        [JsonProperty("userId")]
        public string UserId { get; set; }

        [JsonProperty("userName")]
        public string UserName { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("state")]
        public string State { get; set; }

        [JsonProperty("createDate")]
        public string CreateDate { get; set; }

        [JsonProperty("lastModifyDate")]
        public string LastModifyDate { get; set; }

        [JsonProperty("comment")]
        public string Comment { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("amount")]
        public string Amount { get; set; }

        [JsonProperty("currency")]
        public string Currency { get; set; }

        [JsonProperty("wallet")]
        public string Wallet { get; set; }
    }
}
