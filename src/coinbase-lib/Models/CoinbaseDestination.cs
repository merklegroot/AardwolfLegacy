namespace coinbase_lib.Models
{
    public class CoinbaseDestination
    {
        //"resource": "bitcoin_address",
        public string Resource { get; set; }

        //"address": "1Q3N1vgQhc698zKQTSV9pmyeTjSTM2Unbi",
        public string Address { get; set; }

        //"currency": "BTC"
        public string Currency { get; set; }
    }
}
