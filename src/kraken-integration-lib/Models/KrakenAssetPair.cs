using Newtonsoft.Json;
using System;
using trade_model;

namespace kraken_integration_lib.Models
{
    public class KrakenAssetPair
    {
        // {"error":[],"result":
        // {"BCHEUR":{

        public string Name { get; set; }

        // "altname":"BCHEUR",
        [JsonProperty("altname")]
        public string AltName { get; set; }

        // "aclass_base":"currency",
        [JsonProperty("aclass_base")]
        public string AssetClassBase { get; set; }

        // "base":"BCH",
        [JsonProperty("base")]
        public string BaseSymbol { get; set; }

        // "aclass_quote":"currency",
        [JsonProperty("aclass_quote")]
        public string AssetClassQuote { get; set; }

        // "quote":"ZEUR",
        [JsonProperty("quote")]
        public string Quote { get; set; }

        // "lot":"unit",
        [JsonProperty("lot")]
        public string Lot { get; set; }

        // "pair_decimals":1,
        [JsonProperty("pair_decimals")]
        public int? PairDecimals { get; set; }

        // "lot_decimals":8,
        [JsonProperty("lot_decimals")]
        public int? LotDecimals { get; set; }

        // "lot_multiplier":1,
        [JsonProperty("lot_multiplier")]
        public int? LotMultiplier { get; set; }

        // "leverage_buy":[],
        // public List<decimal> LeverageBuy { get; set; }

        // "leverage_sell":[],
        // public List<decimal> LeverageSell { get; set; }

        // "fees":[[0,0.26],[50000,0.24],[100000,0.22],[250000,0.2],
        // [500000,0.18],[1000000,0.16],[2500000,0.14],[5000000,0.12],
        // [10000000,0.1]],"fees_maker":[[0,0.16],[50000,0.14],
        // [100000,0.12],[250000,0.1],[500000,0.08],[1000000,0.06],
        // [2500000,0.04],[5000000,0.02],[10000000,0]],
        // public List<List<decimal>> Fees { get; set; }

        // "fee_volume_currency":"ZUSD",
        [JsonProperty("fee_volume_currency")]
        public string FeeVolumeCurrency { get; set; }

        // "margin_call":80,
        [JsonProperty("margin_call")]
        public int? MarginCall { get; set; }

        // "margin_stop":40}
        [JsonProperty("margin_stop")]
        public int? MarginStop { get; set; }

        public TradingPair ToTradingPair()
        {
            var prefixLayer = new Func<string, string>(krakenSymbol =>
            {   
                if (string.IsNullOrWhiteSpace(krakenSymbol)) { return null; }
                var effectiveKraken = krakenSymbol.Trim().ToUpper();

                if (effectiveKraken.StartsWith("X"))
                {
                    return effectiveKraken.Substring(1);
                }

                if (effectiveKraken.StartsWith("Z"))
                {
                    return $"FIAT_{effectiveKraken.Substring(1)}";
                }

                return effectiveKraken;
            });

            var synonymLayer = new Func<string, string>(krakenSymbol =>
            {
                if (string.Equals(krakenSymbol, "XBT"))
                {
                    return "BTC";
                }

                return krakenSymbol;
            });

            var toCanon = new Func<string, string>(krakenSymbol =>
            {
                return synonymLayer(prefixLayer(krakenSymbol));
            });

            return new TradingPair { Symbol = toCanon(BaseSymbol), BaseSymbol = toCanon(Quote) };
        }
    }
}
