using System;

namespace coss_lib.Models
{
    public class CossWithdrawalConfirmation
    {
        public long MessageId { get; set; }

        public DateTime? ReceivedTimeStamp { get; set; }

        public string Symbol { get; set; }

        public decimal Quantity { get; set; }

        public string Link { get; set; }
    }
}
