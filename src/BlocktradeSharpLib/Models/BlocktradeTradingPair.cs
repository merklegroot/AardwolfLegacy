using Newtonsoft.Json;

namespace BlocktradeExchangeLib.Models
{
    public class BlocktradeTradingPair
    {
        // "id": 12,
        [JsonProperty("id")]
        public int Id { get; set; }

        // "base_asset_id": 5,
        [JsonProperty("base_asset_id")]
        public int BaseAssetId { get; set; }

        // "quote_asset_id": 3,
        [JsonProperty("quote_asset_id")]
        public int QuoteAssetId { get; set; }

        // "decimal_precision": 8,
        [JsonProperty("decimal_precision")]
        public int DecimalPrecision { get; set; }

        // "lot_size": "0.01",
        [JsonProperty("lot_size")]
        public decimal LotSize { get; set; }

        // "tick_size": "0.00001"
        [JsonProperty("tick_size")]
        public decimal TickSize { get; set; }
    }
}
