using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using trade_email_lib.Models;
using web_util;

namespace trade_email_lib
{
    public class TradeEmailUtil : ITradeEmailUtil
    {
        private const decimal Tolerance = 0.01m;

        private readonly IWebUtil _webUtil;

        public TradeEmailUtil(IWebUtil webUtil)
        {
            _webUtil = webUtil;
        }

        public string GetWithdrawalLink(
            string integrationName,
            string symbol,
            decimal quantity)
        {
            var recentLinks = GetRecentEmailLinks(integrationName);
            var match = recentLinks.FirstOrDefault(item =>
                string.Equals(item.Symbol, symbol, StringComparison.InvariantCultureIgnoreCase)
                && IsWithinTolerance(item.Quantity, quantity, Tolerance)
            );

            if (match == null) { return null; }

            return match.Link;
        }

        public string GetCossWithdrawalLink(string symbol, decimal quantity)
        {
            return GetWithdrawalLink("coss", symbol, quantity);
        }

        private List<EmailLink> GetRecentEmailLinks(string integrationName)
        {
            var payload = new { name = integrationName };
            var json = JsonConvert.SerializeObject(payload);
            var contents = _webUtil.Post("http://localhost/email-link-api/api/get-email-links", json);
            return !string.IsNullOrWhiteSpace(contents) ? JsonConvert.DeserializeObject<List<EmailLink>>(contents) : new List<EmailLink>();
        }

        private bool IsWithinTolerance(decimal a, decimal b, decimal tol)
        {
            var mean = (a + b) / 2;
            if (mean == 0) { return false; }

            var diff = Math.Abs(b - a);
            var percentDifference = diff / mean;
            return percentDifference <= tol;
        }
    }
}
