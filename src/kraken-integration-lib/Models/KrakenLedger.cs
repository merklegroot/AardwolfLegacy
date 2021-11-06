using Newtonsoft.Json;
using System.Collections.Generic;

namespace kraken_integration_lib.Models
{
    public class KrakenLedger
    {
        //	"error": [],
        // TODO: find out what kind of data goes into "error"

        [JsonProperty("result")]
        public KrakenLedgerResult Result { get; set; }

        public class KrakenLedgerResult
        {
            [JsonProperty("ledger")]
            public Dictionary<string, KrakenLedgerItem> Ledger { get; set; }
        }        
    }
}
