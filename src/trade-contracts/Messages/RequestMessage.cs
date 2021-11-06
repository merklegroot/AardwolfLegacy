using System;

namespace trade_contracts.Messages
{
    public class RequestMessage : MessageBase
    {
        public string ResponseQueue { get; set; } = Guid.NewGuid().ToString();
    }
}
