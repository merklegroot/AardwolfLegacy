using System;

namespace client_lib.Models
{
    public class PongInfo
    {
        public string ApplicationName { get; set; }
        public string ApplicationVersion { get; set; }
        public string MachineName { get; set; }
        public string ProcessName { get; set; }
        public DateTime? BuildDate { get; set; }
    }
}
