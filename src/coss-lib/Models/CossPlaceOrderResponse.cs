using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace coss_lib.Models
{
    public class CossPlaceOrderResponse
    {
        private static Dictionary<string, bool> SuccessParsingDictionary = new Dictionary<string, bool>(StringComparer.InvariantCultureIgnoreCase)
        {
            { "success", true },
            { "failure", false },
            { "failed", false },
            { "fail", false },
        };

        [JsonProperty("successful")]
        public bool Successful { get; set; }

        //public List<CossPlaceOrderResponsePayloadItem> Payload { get; set; }

        //public class CossPlaceOrderResponsePayloadItem : List<string>
        //{
        //    public bool? Successful
        //    {
        //        get
        //        {
        //            const int Index = 0;
        //            if (Count < (Index + 1) || string.IsNullOrWhiteSpace(this[Index])) { return null; }
        //            var comp = this[0].Trim();

        //            return SuccessParsingDictionary.ContainsKey(comp)
        //                ? SuccessParsingDictionary[comp]
        //                : (bool?)null;
        //        }
        //    }

        //    public string Description => Count > 1 ? this[1] : null;
        //}
    }
}
