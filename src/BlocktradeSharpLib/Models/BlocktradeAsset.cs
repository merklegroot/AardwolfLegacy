using Newtonsoft.Json;

namespace BlocktradeExchangeLib.Models
{
    public class BlocktradeAsset
    {
		//"id": 9,
        [JsonProperty("id")]
        public int Id { get; set; }

        //"full_name": "Basic Attention Token",
        [JsonProperty("full_name")]
        public string FullName { get; set; }

        //"iso_code": "BAT",
        [JsonProperty("iso_code")]
        public string IsoCode { get; set; }

        //"icon_path": "/media/bat.svg",
        [JsonProperty("icon_path")]
        public string IconPath { get; set; }

        //"icon_path_png": "/media/bat.png",
        [JsonProperty("icon_path_png")]
        public string IconPathPng { get; set; }

        //"color": "#FF5000",
        [JsonProperty("color")]
        public string Color { get; set; }

        //"sign": null,

        //"currency_type": "CRYPTOCURRENCY",
        [JsonProperty("currency_type")]
        public string CurrencyType { get; set; }

        //"minimal_withdrawal_amount": "10",
        [JsonProperty("minimal_withdrawal_amount")]
        public decimal? MinimalWithdrawalAmount { get; set; }

        //"minimal_order_value": "10",
        [JsonProperty("minimal_order_value")]
        public decimal? MinimalOrderValue { get; set; }

        //"maximum_order_value": "50000",
        [JsonProperty("maximum_order_value")]
        public decimal? MaximumOrderValue { get; set; }

        //"lot_size": "10",
        [JsonProperty("lot_size")]
        public decimal? LotSize { get; set; }

        //"decimal_precision": 8
        [JsonProperty("decimal_precision")]
        public int? DecimalPrecision { get; set; }
    }
}
