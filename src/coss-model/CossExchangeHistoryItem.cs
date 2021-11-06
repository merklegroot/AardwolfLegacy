using System;

namespace coss_lib.Models
{
    public class CossExchangeHistoryItem
    {
        public DateTime Date { get; set; }
        public string Pair { get; set; }
        public string Action { get; set; }
        public decimal Amount { get; set; }
        public decimal Price { get; set; }
        public decimal Total { get; set; }
        public decimal TransactionFee { get; set; }
    }
}
