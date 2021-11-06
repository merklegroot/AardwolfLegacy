using coss_api_client_lib.Models;
using date_time_lib;
using Newtonsoft.Json;
using System.Linq;

namespace coss_api_client_lib
{
    public static class CossApiUtil
    {
        public static CossErrorInfo InterpretError(string errorText)
        {
            var message = JsonConvert.DeserializeObject<CossOrderErrorResponseMessage>(errorText);
            const string NotEqualIndicator = @"=/=";
            var notEqualPos = message.ErrorDescription.IndexOf(NotEqualIndicator);
            var afterNotEqual = message.ErrorDescription.Substring(notEqualPos + NotEqualIndicator.Length);
            var afterRemovedReceivedValue = afterNotEqual.Replace("received value:", "");

            var pieces = afterRemovedReceivedValue.Split('|').Select(item => item.Trim()).ToList();
            if (pieces.Count != 2) { return null; }

            return new CossErrorInfo
            {
                ExpectedValue = decimal.Parse(pieces[0]),
                ReceivedValue = decimal.Parse(pieces[1])
            };
        }

        public static long GenerateNonce()
        {
            return DateTimeUtil.GetUnixTimeStamp13Digit();
        }
    }
}
