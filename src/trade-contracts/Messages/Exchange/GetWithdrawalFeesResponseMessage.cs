using System.Collections.Generic;

namespace trade_contracts.Messages.Exchange
{
    public class GetWithdrawalFeesResponseMessage : ResponseMessage
    {
        public Dictionary<string, decimal> WithdrawalFees { get; set; }
    }
}
