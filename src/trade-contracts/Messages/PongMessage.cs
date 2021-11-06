using System;

namespace trade_contracts.Messages
{
    public class PongMessage : ResponseMessage
    {
        public string ApplicationName { get; set; }
        public string ApplicationVersion { get; set; }
        public string MachineName { get; set; }
        public string ProcessName { get; set; }
        public DateTime? BuildDate { get; set; }
    }
}
