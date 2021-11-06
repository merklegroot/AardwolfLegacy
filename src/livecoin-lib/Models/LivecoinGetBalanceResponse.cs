using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace livecoin_lib.Models
{
    //{
    //  "info": [
    //    {
    //      "type": "total",
    //      "currency": "RUR",
    //      "value": 0
    //    },

    public class LivecoinGetBalanceResponse
    {
        [JsonProperty("info")]
        public List<LivecoinHolding> Info { get; set; }

        public class LivecoinHolding
        {
            private static Dictionary<string, LivecoinHoldingType> HoldingTypeDictionary = new Dictionary<string, LivecoinHoldingType>(StringComparer.InvariantCultureIgnoreCase)
            {
                { "total", LivecoinHoldingType.Total },
                { "trade", LivecoinHoldingType.Trade },
                { "available", LivecoinHoldingType.Available },
                { "available_withdrawal", LivecoinHoldingType.AvailableWithdrawal }
            };

            [JsonProperty("type")]
            public string HoldingTypeText { get; set; }

            public LivecoinHoldingType HoldingType
            {
                get
                {
                    return HoldingTypeDictionary.ContainsKey(HoldingTypeText)
                        ? HoldingTypeDictionary[HoldingTypeText]
                        : LivecoinHoldingType.Unknown;
                }
            }

            [JsonProperty("currency")]
            public string Currency { get; set; }

            [JsonProperty("value")]
            public decimal Value { get; set; }
        }
    }
}
