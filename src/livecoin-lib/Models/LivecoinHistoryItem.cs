using Newtonsoft.Json;

namespace livecoin_lib.Models
{
    public class LivecoinHistoryItem
    {
        //"id": "0xb367cca09684818b37b57309e5b2e42b383c01ed48eed730818222d7f699e232",
        [JsonProperty("id")]
        public string Id { get; set; }

        //"type": "DEPOSIT",
        [JsonProperty("type")]
        public string Type { get; set; }

        //"date": 1542491149521,
        [JsonProperty("date")]
        public long Date { get; set; }

        //"amount": 3905.95222580,
        [JsonProperty("amount")]
        public decimal? Amount { get; set; }

        //"fee": 0E-8,
        [JsonProperty("fee")]
        public decimal? Fee { get; set; }

        //"fixedCurrency": "NOX",
        [JsonProperty("fixedCurrency")]
        public string FixedCurrency { get; set; }

        //"taxCurrency": "NOX",
        [JsonProperty("taxCurrency")]
        public string TaxCurrency { get; set; }

        //"variableAmount": null,
        [JsonProperty("variableAmount")]
        public decimal? VariableAmount { get; set; }

        //"variableCurrency": null,
        [JsonProperty("variableCurrency")]
        public string VariableCurrency { get; set; }

        //"external": "Coin",
        [JsonProperty("external")]
        public string External { get; set; }

        //"login": "---",
        [JsonProperty("login")]
        public string Login { get; set; }

        //"externalKey": "0xfa214d7da793a0063ca48ac929d0e1829903adae",
        [JsonProperty("externalKey")]
        public string ExternalKey { get; set; }

        //"documentId": 839228488
        [JsonProperty("documentId")]
        public long? DocumentId { get; set; }
    }
}
