using Newtonsoft.Json;

namespace bit_z_lib.Models
{
    public class BitzPlaceOrderResponse
    {
        // "status": 200,
        [JsonProperty("status")]
        public int Status { get; set; }

        // "msg": "",
        [JsonProperty("msg")]
        public string Msg { get; set; }

        // "data": {
        [JsonProperty("data")]
        public ResponseData Data { get; set; }

        // "time": 1539880298,
        [JsonProperty("time")]
        public int Time { get; set; }

        // "microtime": "0.26178600 1539880298",
        [JsonProperty("microtime")]
        public string MicroTime { get; set; }

        // "source": "api"
        [JsonProperty("source")]
        public string Source { get; set; }

        public class ResponseData
        {
            // "id": 961520255,
            [JsonProperty("id")]
            public long Id { get; set; }

            // "uId": 1371511,
            [JsonProperty("uId")]
            public long Uid { get; set; }

            // "price": "0.0000073500",
            [JsonProperty("price")]
            public decimal Price { get; set; }

            // "number": "34334.7138",
            [JsonProperty("number")]
            public decimal Number { get; set; }

            // "numberOver": "34334.7138",
            [JsonProperty("numberOver")]
            public decimal NumberOver { get; set; }

            // "flag": "sale",
            [JsonProperty("flag")]
            public string Flag { get; set; }

            // "status": 0,
            [JsonProperty("status")]
            public int Status { get; set; }

            // "coinFrom": "npxs",
            [JsonProperty("coinFrom")]
            public string CoinFrom { get; set; }

            // "coinTo": "eth",
            [JsonProperty("coinTo")]
            public string CoinTo { get; set; }

            // "numberDeal": 0
            [JsonProperty("numberDeal")]
            public decimal NumberDeal { get; set; }
        }
    }
}
