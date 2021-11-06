using trade_contracts.Messages;

namespace trade_contracts
{
    public class ConfirmWithdrawalLinkRequestMessage : RequestMessage
    {
        public string Url { get; set; }        
    }
}
