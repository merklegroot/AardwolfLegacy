using Newtonsoft.Json;
using System.Collections.Generic;

namespace BlocktradeExchangeLib.Models
{
    public class BlocktradeUserOrder
    {
		// "id": 13942479,
        [JsonProperty("id")]
        public long Id { get; set; }

        // "portfolio_id": 1,
        [JsonProperty("portfolio_id")]
        public int PortfolioId { get; set; }

        // "direction": "SELL",
        [JsonProperty("direction")]
        public string Direction { get; set; }

        // "type": "LIMIT",
        [JsonProperty("type")]
        public string Type { get; set; }

        // "trading_pair_id": 11,
        [JsonProperty("trading_pair_id")]
        public int TradingPairId { get; set; }

        // "amount": "0.01",
        [JsonProperty("amount")]
        public decimal Amount { get; set; }

        // "remaining_amount": "0.01",
        [JsonProperty("remaining_amount")]
        public decimal RemainingAmount { get; set; }

        // "price": "0.03499",
        [JsonProperty("price")]
        public decimal Price { get; set; }

        // "time_in_force": "GTC",
        [JsonProperty("time_in_force")]
        public string TimeInForce { get; set; }

        // "stop_price": null,
        [JsonProperty("stop_price")]
        public decimal? StopPrice { get; set; }

        // "date": 1547263358366,
        [JsonProperty("date")]
        public long Date { get; set; }

        // "status": "NEW",
        [JsonProperty("status")]
        public string Status { get; set; }

        // "trades": []
        [JsonProperty("trades")]
        public List<BlocktradeTrade> Trades { get; set; }
    }
}
