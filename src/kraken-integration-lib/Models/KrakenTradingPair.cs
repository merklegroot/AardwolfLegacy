using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace kraken_integration_lib.Models
{
    public class KrakenTradingPair
    {
        public string PairName { get; set; }
        public string AltName { get; set; }
        public string AClass_Base { get; set; }
        public string Base { get; set; }
        public string AClass_Quote { get; set; }

        /*
        "BCHEUR": {
			"altname": "BCHEUR",
			"aclass_base": "currency",
			"base": "BCH",
			"aclass_quote": "currency",
			"quote": "ZEUR",
			"lot": "unit",
			"pair_decimals": 1,
			"lot_decimals": 8,
			"lot_multiplier": 1,
			"leverage_buy": [],
			"leverage_sell": [],
			"fees": [[0, 0.26], [50000, 0.24], [100000, 0.22], [250000, 0.2], [500000, 0.18], [1000000, 0.16], [2500000, 0.14], [5000000, 0.12], [10000000, 0.1]],
			"fees_maker": [[0, 0.16], [50000, 0.14], [100000, 0.12], [250000, 0.1], [500000, 0.08], [1000000, 0.06], [2500000, 0.04], [5000000, 0.02], [10000000, 0]],
			"fee_volume_currency": "ZUSD",
			"margin_call": 80,
			"margin_stop": 40
		}
        */


    }
}
