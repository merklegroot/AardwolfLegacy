using System;

namespace trade_contracts.Messages.Browser
{
    public class GetCossCookiesResponseMessage : ResponseMessage
    {
        public class GetCookiesResponseMessagePayload
        {
            public DateTime? TimeStampUtc { get; set; }
            public string SessionToken { get; set; }
            public string XsrfToken { get; set; }
        }

        public GetCookiesResponseMessagePayload Payload { get; set; }
    }
}
