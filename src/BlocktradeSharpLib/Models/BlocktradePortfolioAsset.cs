using Newtonsoft.Json;

namespace BlocktradeExchangeLib.Models
{
    public class BlocktradePortfolioAsset
    {
        [JsonProperty("trading_asset_id")]
        public int TradingAssetId { get; set; }

        [JsonProperty("available_amount")]
        public decimal? AvailableAmount { get; set; }

        [JsonProperty("reserved_amount")]
        public decimal? ReservedAmount { get; set; }

        [JsonProperty("wallet_address")]
        public string WalletAddress { get; set; }
    }
}
