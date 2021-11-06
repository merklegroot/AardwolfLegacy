using trade_contracts.Messages;

namespace trade_contracts
{
    public class ConfirmWithdrawalLinkResponseMessage : MessageBase
    {
        public bool WasSuccessful { get; set; }
        public string FailureReason { get; set; }
    }
}
