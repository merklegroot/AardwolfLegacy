using System;

namespace bit_z_model
{
    public class BitzTradeHistoryItem
    {
        // starting at 1.
        public int PageNumber { get; set; }

        // Market	Type	Price	Amount	Total	Transaction Time
        public string Market { get; set; }
        public string Type { get; set; }
        public decimal Price { get; set; }
        public decimal Amount { get; set; }
        public string Total { get; set; }
        public DateTime TransactionTime { get; set; }
    }
}
