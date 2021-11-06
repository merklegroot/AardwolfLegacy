using System;

namespace coss_agent_lib.Models
{
    public class CossSessionState
    {
        public DateTime AsOf { get; set; } = DateTime.UtcNow;
        public bool IsLoggedIn { get; set; }
    }
}
