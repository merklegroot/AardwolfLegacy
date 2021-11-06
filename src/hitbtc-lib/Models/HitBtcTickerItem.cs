using Newtonsoft.Json;
using System;

namespace hitbtc_lib.Models
{
    public class HitBtcTickerItem
    {
        public decimal? Ask { get; set; }
        public decimal? Bid { get; set;  }
        public decimal? Last { get; set; }
        public decimal? Open { get; set;  }
        public decimal? Low { get; set; }
        public decimal? High { get; set; }
        public decimal? Volume { get; set; }
        public decimal? VolumeQuote { get; set; }
        public DateTime TimeStamp { get; set; }

        public string Symbol { get; set; }
        public string BaseSymbol
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Symbol)) { return null; }
                var effectiveText = Symbol.Trim().ToUpper();
                if (effectiveText.Length < 3) { return null; }

                return effectiveText.Substring(Symbol.Length - 3, 3);
            }
        }

        public string QuoteSymbol
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Symbol)) { return null; }
                var effectiveText = Symbol.Trim().ToUpper();
                if (effectiveText.Length < 3) { return null; }

                return effectiveText.Substring(0, 3);
            }
        }
    }
}
