using Newtonsoft.Json;

namespace binance_lib.Models.Ccxt
{
    public class BinanceCcxtFetchOpenOrdersResponse
    {
        //"info": {
        [JsonProperty("info")]
        public BinanceCcxtFetchOpenOrdersResponseInfo Info { get; set; }

        public class BinanceCcxtFetchOpenOrdersResponseInfo
        {
            //	"symbol": "ARKBTC",
            [JsonProperty("symbol")]
            public string Symbol { get; set; }

            //	"orderId": 14691365,
            [JsonProperty("orderId")]
            public long OrderId { get; set; }

            //	"clientOrderId": "ufFLvLJXd1GX20K69JN9VY",
            [JsonProperty("clientOrderId")]
            public string ClientOrderId { get; set; }

            //	"price": "0.00023620",
            [JsonProperty("price")]
            public string Price { get; set; }

            //	"origQty": "5.00000000",
            [JsonProperty("origQty")]
            public string OrigQty { get; set; }

            //	"executedQty": "0.00000000",
            [JsonProperty("executedQty")]
            public string ExecutedQty { get; set; }

            //	"status": "NEW",
            [JsonProperty("status")]
            public string Status { get; set; }

            //	"timeInForce": "GTC",
            [JsonProperty("timeInForce")]
            public string TimeInForce { get; set; }

            //	"type": "LIMIT",
            [JsonProperty("type")]
            public string Type { get; set; }

            //	"side": "SELL",
            [JsonProperty("side")]
            public string Side { get; set; }

            //	"stopPrice": "0.00000000",
            [JsonProperty("stopPrice")]
            public string StopPrice { get; set; }

            //	"icebergQty": "0.00000000",
            [JsonProperty("icebergQty")]
            public string IcebergQty { get; set; }

            //	"time": 1530559973884,
            [JsonProperty("time")]
            public long Time { get; set; }

            //	"isWorking": true
            [JsonProperty("isWorking")]
            public bool IsWorking { get; set; }
        }

        //"id": "14691365",
        [JsonProperty("id")]
        public string Id { get; set; }

        //"timestamp": 1530559973884,
        [JsonProperty("timestamp")]
        public long TimeStamp { get; set; }

        //"datetime": "2018-07-02T19:32:53.884Z",
        [JsonProperty("datetime")]
        public string DateTime { get; set; }

        //"symbol": "ARK/BTC",
        [JsonProperty("symbol")]
        public string Symbol { get; set; }

        //"type": "limit",
        [JsonProperty("type")]
        public string Type { get; set; }

        //"side": "sell",
        [JsonProperty("side")]
        public string Side { get; set; }

        //"price": 0.0002362,
        [JsonProperty("price")]
        public decimal Price { get; set; }

        //"amount": 5,
        [JsonProperty("amount")]
        public decimal Amount { get; set; }

        //"cost": 0,
        [JsonProperty("cost")]
        public decimal Cost { get; set; }

        //"filled": 0,
        [JsonProperty("filled")]
        public decimal Filled { get; set; }

        //"remaining": 5,
        [JsonProperty("remaining")]
        public decimal Remaining { get; set; }

        //"status": "open"
        [JsonProperty("status")]
        public string Status { get; set; }
    }
}
