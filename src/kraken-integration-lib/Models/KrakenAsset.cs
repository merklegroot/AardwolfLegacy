namespace kraken_integration_lib.Models
{
    public class KrakenAsset
    {
        public string Symbol { get; set; }
        public string AssetClass { get; set; }
        public string AltName { get; set; }
        public int? Decimals { get; set; }
        public int? DisplayDecimals { get; set; }
        /*
{"error":[],"result":{"BCH":{"aclass":"currency","altname":"BCH",
"decimals":10,"display_decimals":5}
        */
    }
}
