//using Newtonsoft.Json;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using web_util;

//namespace coss_lib
//{
//    public class CossEmailUtil : ICossEmailUtil
//    {
//        private readonly IWebUtil _webUtil;

//        public CossEmailUtil(IWebUtil webUtil)
//        {
//            _webUtil = webUtil;
//        }

//        public class EmailLink
//        {
//            [JsonProperty("messageId")]
//            public string MessageId { get; set; }

//            [JsonProperty("receivedTimeStamp")]
//            public DateTime ReceivedTimeStamp { get; set; }

//            [JsonProperty("symbol")]
//            public string Symbol { get; set; }

//            [JsonProperty("quantity")]
//            public decimal Quantity { get; set; }

//            [JsonProperty("link")]
//            public string Link { get; set; }
//        }

//        public string GetWithdrawalLink(string symbol, decimal quantity)
//        {
//            var recentLinks = GetRecentEmailLinks();
//            var match = recentLinks.FirstOrDefault(item =>
//                string.Equals(item.Symbol, symbol, StringComparison.InvariantCultureIgnoreCase)
//                && IsWithinTolerance(item.Quantity, quantity, 0.01m)
//            );

//            if (match == null) { return null; }

//            return match.Link;
//        }

//        private List<EmailLink> GetRecentEmailLinks()
//        {
//            var payload = new { name = "coss" };
//            var json = JsonConvert.SerializeObject(payload);
//            var contents = _webUtil.Post("http://localhost/email-link-api/api/get-email-links", json);
//            return !string.IsNullOrWhiteSpace(contents) ? JsonConvert.DeserializeObject<List<EmailLink>>(contents) : new List<EmailLink>();
//        }

//        private bool IsWithinTolerance(decimal a, decimal b, decimal tol)
//        {
//            var mean = (a + b) / 2;
//            if (mean == 0) { return false; }

//            var diff = Math.Abs(b - a);
//            var percentDifference = diff / mean;
//            return percentDifference <= tol;
//        }
//    }
//}
