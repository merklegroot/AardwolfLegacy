using System.Collections.Generic;
using trade_contracts.Messages.Exchange.HitBtc;

namespace trade_contracts.Messages.Exchange
{
    public class GetHitBtcHealthStatusResponseMessage : ResponseMessage
    {
        public List<HitBtcHealthStatusItemContract> Payload { get; set; }
    }
}
