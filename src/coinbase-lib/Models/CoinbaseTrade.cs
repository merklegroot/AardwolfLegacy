using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace coinbase_lib.Models
{
    public class CoinbaseTrade
    {
        [JsonProperty("id")]
        public Guid? Id { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("payment_method")]
        public CoinbaseResource PaymentMethod { get; set; }

        [JsonProperty("transaction")]
        public CoinbaseResource Transaction { get; set; }

        [JsonProperty("user_reference")]
        public string UserReference { get; set; }

        [JsonProperty("created_at")]
        public DateTime? CreatedAt { get; set; }

        [JsonProperty("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        [JsonProperty("resource")]
        public string Resource { get; set; }

        [JsonProperty("resource_path")]
        public string ResourcePath { get; set; }

        [JsonProperty("committed")]
        public bool? Committed { get; set; }

        [JsonProperty("payout_at")]
        public DateTime? PayoutAt { get; set; }

        [JsonProperty("instant")]
        public bool? Instant { get; set; }

        [JsonProperty("fees")]
        public List<CoinbaseFee> Fees { get; set; }

        [JsonProperty("amount")]
        public CoinbaseAmount Amount { get; set; }

        [JsonProperty("total")]
        public CoinbaseAmount Total { get; set; }

        [JsonProperty("subtotal")]
        public CoinbaseAmount Subtotal { get; set; }

        [JsonProperty("hold_until")]
        public string Hold_until { get; set; }

        [JsonProperty("hold_days")]
        public decimal? HoldDays { get; set; }

        [JsonProperty("is_first_buy")]
        public bool? IsFirstBuy { get; set; }

        [JsonProperty("requires_completion_step")]
        public bool? RequiresCompletionStep { get; set; }

        [JsonProperty("accountId")]
        public Guid? AccountId { get; set; }
    }
}
