using System;

namespace trade_contracts.Messages
{
    public interface IMessageBase
    {
        Guid MessageId { get; set; }
        Guid CorrelationId { get; set; }
        DateTime TimeStampUtc { get; set; }
    }
}
