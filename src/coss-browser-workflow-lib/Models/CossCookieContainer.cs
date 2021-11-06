using System;

namespace coss_browser_workflow_lib.Models
{
    public class CossCookieContainer
    {
        public DateTime TimeStampUtc { get; set; }
        public string XsrfToken { get; set; }
        public string SessionToken { get; set; }        
    }
}
