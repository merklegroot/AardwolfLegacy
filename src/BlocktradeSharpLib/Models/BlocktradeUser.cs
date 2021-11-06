using Newtonsoft.Json;

namespace BlocktradeExchangeLib.Models
{
    public class BlocktradeUser
    {
        [JsonProperty("user_id")]
        public long UserId { get; set; }

        [JsonProperty("email")]
        public string Email { get; set; }

        [JsonProperty("primary_currency")]
        public BlocktradeCurrency PrimaryCurrency { get; set; }

        [JsonProperty("kyc_status")]
        public string KycStatus { get; set; }

        [JsonProperty("websocket_auth_token")]
        public string WebsocketAuthToken { get; set; }

        [JsonProperty("is_2fa_enabled")]
        public bool Is2faEnabled { get; set; }

        [JsonProperty("first_name")]
        public string FirstName { get; set; }

        [JsonProperty("last_name")]
        public string LastName { get; set; }

        [JsonProperty("gender")]
        public string Gender { get; set; }

        //"date_of_birth": numeric,
        [JsonProperty("date_of_birth")]
        public long DateOfBirth { get; set; }

        [JsonProperty("place_of_birth")]
        public string PlaceOfBirth { get; set; }

        [JsonProperty("address")]
        public string Address { get; set; }

        [JsonProperty("postal_code")]
        public string PostalCode { get; set; }

        [JsonProperty("city")]
        public string City { get; set; }

        [JsonProperty("state")]
        public string State { get; set; }

        [JsonProperty("country")]
        public string Country { get; set; }

        [JsonProperty("phone_number")]
        public string PhoneNumber { get; set; }

        [JsonProperty("taxpayer_identification_number")]
        public string TaxpayerIdentificationNumber { get; set; }
    }
}
