using System.Collections.Generic;

namespace idex_integration_lib.Models
{
    public class IdexExtendedOrderBook
    {
        public List<IdexExtendedOrder> Asks { get; set; }
        public List<IdexExtendedOrder> Bids { get; set; }
    }
}
