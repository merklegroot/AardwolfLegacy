using trade_contracts.Messages;

namespace trade_contracts
{
    public class OpenUrlRequestMessage : MessageBase
    {
        public string Url { get; set; }
    }
}
