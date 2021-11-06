using Newtonsoft.Json;
using System;

namespace hitbtc_lib.Client.ClientModels
{
    public class HitBtcApiTradeHistoryItem
    {
        //"id": 436647685,
        [JsonProperty("id")]
        public long Id { get; set; }

        //"clientOrderId": "67926fc0edb1733b745132ea58d64a35",
        [JsonProperty("clientOrderId")]
        public string ClientOrderId { get; set; }

        //"orderId": 94996856505,
        [JsonProperty("orderId")]
        public long OrderId { get; set; }

        //"symbol": "CHXETH",
        [JsonProperty("symbol")]
        public string Symbol { get; set; }

        //"side": "sell",
        [JsonProperty("side")]
        public string Side { get; set; }

        //"quantity": "100",
        [JsonProperty("quantity")]
        public decimal? Quantity { get; set; }

        //"price": "0.0010608",
        [JsonProperty("price")]
        public decimal? Price { get; set; }

        //"fee": "-0.000010608000",
        [JsonProperty("fee")]
        public decimal? Fee { get; set; }

        //"timestamp": "2019-01-21T15:45:01.896Z"
        [JsonProperty("timestamp")]
        public DateTime TimeStamp { get; set; }
    }
}
