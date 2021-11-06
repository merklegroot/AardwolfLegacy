using System;

namespace binance_lib.Models.Canonical
{
    public class BcDeposit
    {
        public DateTime InsertTime { get; set; }
        public decimal Amount { get; set; }
        public string Asset { get; set; }
        public string Address { get; set; }
        public string TransactionId { get; set; }
        public BcDepositStatus Status { get; set; }
    }
}
