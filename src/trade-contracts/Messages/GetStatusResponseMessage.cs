using System;
using trade_contracts.Messages;

namespace trade_contracts
{
    public class GetStatusResponseMessage : MessageBase
    {
        public string StatusText { get; set; }
        public string MachineName { get; set; }
        public string ApplicationName { get; set; }
        public DateTime? BuildDate { get; set; }
        public DateTime? ProcessStartTime { get; set; }
    }
}
