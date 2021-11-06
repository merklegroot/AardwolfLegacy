using System;

namespace coss_browser_client_lib
{
    public class CossCookieContainer
    {
        public DateTime? TimeStampUtc { get; set; }
        public string SessionToken { get; set; }
        public string XsrfToken { get; set; }
    }
}
