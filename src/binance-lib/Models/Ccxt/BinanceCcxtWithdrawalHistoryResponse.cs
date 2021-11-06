using Newtonsoft.Json;
using System.Collections.Generic;

namespace binance_lib.Models.Ccxt
{
    public class BinanceCcxtWithdrawalHistoryResponse
    {
	    // "withdrawList": []        
        [JsonProperty("withdrawList")]
        public List<BinanceCcxtWithdrawalHistoryItem> WithdrawList { get; set; }

        public class BinanceCcxtWithdrawalHistoryItem
        {
            //"id": "a16851f2347549538a5e9bce1df28dfa",
            [JsonProperty("id")]
            public string Id { get; set; }

            //"amount": 58.94974019,
            [JsonProperty("amount")]
            public decimal? Amount { get; set; }

            //"address": "0x049264b75db971941b0334ad795181f1b17da24a",
            [JsonProperty("address")]
            public string Address { get; set; }

            //"successTime": 1530927292000,
            [JsonProperty("successTime")]
            public long? SuccessTime { get; set; }

            //"txId": "0x6b24bde7f06f8996b41a396b90639057f308f0ecf17d9f94728d7a56ebc614cf",
            [JsonProperty("txId")]
            public string TxId { get; set; }

            //"asset": "OMG",
            [JsonProperty("asset")]
            public string Asset { get; set; }

            //"applyTime": 1530926102000,
            [JsonProperty("applyTime")]
            public long? ApplyTime { get; set; }

            //"status": 6
            [JsonProperty("status")]
            public int? Status { get; set; }
        }
    }
}
