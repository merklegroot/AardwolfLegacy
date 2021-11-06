using Newtonsoft.Json;

namespace coss_lib.Models
{
    internal class PlaceLimitOrderPayload
    {
        //"pairId": "lsk-eth",
        [JsonProperty("pairId")]
        public string PairId { get; set; }

        //"tradeType": "sell",
        [JsonProperty("tradeType")]
        public string TradeType { get; set; }

        //"orderType": "limit",
        [JsonProperty("orderType")]
        public string OrderType { get; set; }

        //"orderPrice": "0.01612513",
        [JsonProperty("orderPrice")]
        public string OrderPrice { get; set; }

        //"orderAmount": "9.25902793",
        [JsonProperty("orderAmount")]
        public string OrderAmount { get; set; }

        //"orderTotalWithFee": "0.14945233",
        [JsonProperty("orderTotalWithFee")]
        public string OrderTotalWithFee { get; set; }

        //"orderTotalWithoutFee": "0.14930303",
        [JsonProperty("orderTotalWithoutFee")]
        public string OrderTotalWithoutFee { get; set; }

        //"feeValue": "0.00014930",
        [JsonProperty("feeValue")]
        public string FeeValue { get; set; }

        //"fee": "0.001000"
        [JsonProperty("fee")]
        public string Fee { get; set; }
    }
}
