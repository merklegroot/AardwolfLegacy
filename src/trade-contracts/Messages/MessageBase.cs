using System;

namespace trade_contracts.Messages
{
    public class MessageBase : IMessageBase
    {
        public Guid MessageId { get; set; } = Guid.NewGuid();
        public Guid CorrelationId { get; set; } = Guid.NewGuid();
        public DateTime TimeStampUtc { get; set; } = DateTime.UtcNow;
    }
}
