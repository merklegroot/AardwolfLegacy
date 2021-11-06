using Binance.Net.Objects;
using System.Collections.Generic;
using System.Linq;

namespace binance_lib.Models.Canonical
{
	public class BcOrderBook
    {
        public long LastUpdateId { get; set; }
        public List<BcOrderBookEntry> Bids { get; set; }
        public List<BcOrderBookEntry> Asks { get; set; }

        public static BcOrderBook FromModel(BinanceOrderBook model)
        {
            return model != null
                ? new BcOrderBook
                {
                    LastUpdateId = model.LastUpdateId,
                    Bids = model.Bids != null ? model.Bids.Select(item => BcOrderBookEntry.FromModel(item)).ToList() : null,
                    Asks = model.Asks != null ? model.Asks.Select(item => BcOrderBookEntry.FromModel(item)).ToList() : null,
                }
                : null;
        }
    } 
}
