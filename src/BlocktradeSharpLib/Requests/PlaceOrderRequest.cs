using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlocktradeExchangeLib.Requests
{
    public class PlaceOrderRequest
    {
        // portfolio_id integer true    none ID of portfolio
        [JsonProperty("portfolio_id")]
        public int PortfolioId { get; set; }

        // direction   string  true    none none
        [JsonProperty("direction")]
        public string Direction { get; set; }

        // type    string  true    none none
        [JsonProperty("type")]
        public string Type { get; set; }

        // trading_pair_id integer true    none Trading pair id
        [JsonProperty("trading_pair_id")]
        public int TradingPairId { get; set; }

        // amount  string  true    none Amount of asset, number as string
        [JsonProperty("amount")]
        public string Amount { get; set; }

        // price   string  false   none Price for one unit of asset, number as string
        [JsonProperty("price")]
        public string Price { get; set; }

        // time_in_force   string  false   none    none
        [JsonProperty("time_in_force")]
        public string TimeInForce { get; set; }

        // stop_price  string  false   none    Order stop price, number as string
        [JsonProperty("stop_price")]
        public string StopPrice { get; set; }
    }
}
