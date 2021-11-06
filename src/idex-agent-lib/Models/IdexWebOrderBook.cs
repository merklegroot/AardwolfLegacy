using System.Collections.Generic;

namespace idex_agent_lib.Models
{
    public class IdexWebOrderBook
    {
        public List<IdexWebOrderBookItem> Asks { get; set; }
        public List<IdexWebOrderBookItem> Bids { get; set; }

        public class IdexWebOrderBookItem
        {
            public string RowId { get; set; }
            public decimal Price { get; set; }
            public decimal SymbolQuantity { get; set; }
            public decimal EthQuantity { get; set; }
            public decimal Sum { get; set; }
        }
    }
}
