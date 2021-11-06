using System;

namespace binance_lib.Models.Canonical
{
    public class BcWithdrawal
    {
        public string Id { get; set; }
        public DateTime ApplyTime { get; set; }
        public decimal Amount { get; set; }
        public string Address { get; set; }
        public string TransactionId { get; set; }
        public string Asset { get; set; }
        public BcWithdrawalStatus Status { get; set; }
    }
}
