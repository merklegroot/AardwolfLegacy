using date_time_lib;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using trade_model;

namespace bit_z_lib.Utils
{
    public static class BitzHttpUtil
    {
        public static string BitzAuthenticatedRequest(
            ApiKey apiKey,
            string url,
            Dictionary<string, string> queryParams = null)
        {
            var effectiveQueryParams = new Dictionary<string, string>();
            if (queryParams != null)
            {
                foreach (var key in queryParams.Keys.ToList())
                {
                    effectiveQueryParams[key] = queryParams[key];
                }
            }

            var timeStamp = DateTimeUtil.GetUnixTimeStamp().ToString();
            effectiveQueryParams["apiKey"] = apiKey.Key;
            effectiveQueryParams["timeStamp"] = timeStamp;
            effectiveQueryParams["nonce"] = timeStamp.Substring(0, 6);
            effectiveQueryParams["sign"] = BitzSignatureUtil.BuildSignature(apiKey.Secret, effectiveQueryParams);

            return HttpPost(url, effectiveQueryParams);
        }

        private static string HttpPost(string url, Dictionary<string, string> queryParams)
        {
            var httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
            httpWebRequest.Method = "POST";
            httpWebRequest.ContentType = "application/x-www-form-urlencoded";

            if (queryParams != null && queryParams.Any())
            {
                var queryText = string.Join("&", queryParams.Keys.Select(key => $"{key}={queryParams[key]}"));

                var queryData = Encoding.UTF8.GetBytes(queryText);
                httpWebRequest.ContentLength = queryData.Length;

                using (var requestStream = httpWebRequest.GetRequestStream())
                {
                    requestStream.Write(queryData, 0, queryData.Length);
                }
            }

            using (var httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse())
            using (var streamReader = new StreamReader(httpWebResponse.GetResponseStream()))
            {
                return streamReader.ReadToEnd();
            }
        }
    }
}
