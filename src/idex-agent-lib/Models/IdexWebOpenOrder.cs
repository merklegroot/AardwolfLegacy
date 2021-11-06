using System;

namespace idex_agent_lib.Models
{
    public class IdexWebOpenOrder
    {
        public string RowId { get; set; }

        // operation: "Buy", 
        public string Operation { get; set; }

        // price: "0.00038169", 
        public decimal Price { get; set; }

        // symbolQuantity: "1309.96489298",
        public decimal SymbolQuantity { get; set; }

        // ethQuantity: "0.5000005", 
        public decimal EthQuantity { get; set; }

        // date: "2018-07-12 17:21:13"
        public DateTime Date { get; set; }
    }
}
