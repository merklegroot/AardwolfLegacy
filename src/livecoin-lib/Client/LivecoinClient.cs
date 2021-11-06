using date_time_lib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using trade_model;
using web_util;

namespace livecoin_lib.Client
{
    public interface ILivecoinClient
    {
        string GetOrderBook(string nativeSymbol, string nativeBaseSymbol);
        string GetCoinInfoRaw();
        string GetCommission(ApiKey apiKey);
        string GetBalanceRaw(ApiKey apiKey);
        string CancelOrderRaw(ApiKey apiKey, string nativeSymbol, string nativeBaseSymbol, string orderId);
        string GetOpenOrdersRaw(ApiKey apiKey);
        string GetHistoryRaw(ApiKey apiKey);
        string GetTickerRaw();        
    }

    // https://www.livecoin.net/api?lang=en
    public class LivecoinClient : ILivecoinClient
    {
        private const string ApiRoot = "https://api.livecoin.net/";

        private readonly IWebUtil _webUtil;

        public LivecoinClient(IWebUtil webUtil)
        {
            _webUtil = webUtil;
        }

        // https://api.livecoin.net/exchange/all/order_book <-- gives everything
        // https://api.livecoin.net/exchange/order_book?currencyPair=MSCN/RUR
        public string GetOrderBook(string nativeSymbol, string nativeBaseSymbol)
        {
            // var combo = $"{nativeSymbol.ToUpper()}/{nativeBaseSymbol.ToUpper()}";
            var combo = $"{nativeSymbol}/{nativeBaseSymbol}";

            var query = new Dictionary<string, string>
            {
                { "currencyPair", combo }
            };

            var path = $"exchange/order_book";

            return PublicCall(path, query);
        }

        public string GetCoinInfoRaw()
        {
            const string Path = "info/coinInfo";
            return PublicCall(Path);
        }

        public string GetCommission(ApiKey apiKey)
        {
            const string Path = "exchange/commission";
            var fullUrl = $"{ApiRoot}{Path}";

            // var signature = HashHMAC(apiKey.Secret, Path);
            var signature = HashHMAC(apiKey.Secret, string.Empty);

            var req = (HttpWebRequest)WebRequest.Create(fullUrl);
            req.Headers.Add("API-key", apiKey.Key);
            req.Headers.Add("Sign", signature);

            using (var resp = req.GetResponse())
            using (var respStream = resp.GetResponseStream())
            using (var reader = new StreamReader(respStream))
            {
                return reader.ReadToEnd();
            }
        }

        public string GetBalanceRaw(ApiKey apiKey)
        {
            const string Path = "payment/balance";
            var fullUrl = $"{ApiRoot}{Path}";

            // var signature = HashHMAC(apiKey.Secret, Path);
            var signature = HashHMAC(apiKey.Secret, string.Empty);

            var req = (HttpWebRequest)WebRequest.Create(fullUrl);
            req.Headers.Add("API-key", apiKey.Key);
            req.Headers.Add("Sign", signature);

            using (var resp = req.GetResponse())
            using (var respStream = resp.GetResponseStream())
            using (var reader = new StreamReader(respStream))
            {
                return reader.ReadToEnd();
            }
        }

        public string CancelOrderRaw(ApiKey apiKey, string nativeSymbol, string nativeBaseSymbol, string orderId)
        {
            const string Path = "exchange/cancellimit";
            const string Verb = "POST";
            var fullUrl = $"{ApiRoot}{Path}";

            var currencyPair = $"{nativeSymbol}/{nativeBaseSymbol}";
            var query = new Dictionary<string, object>(StringComparer.InvariantCultureIgnoreCase)
            {
                { "currencyPair", currencyPair },
                { "orderId", orderId }
            };

            return PrivateCall(apiKey, Path, Verb, query);
        }

        public string GetHistoryRaw(ApiKey apiKey)
        {            
            const string Path = "/payment/history/transactions";
            
            var endTime = DateTime.UtcNow;
            var startTime = endTime.AddDays(-30);

            var query = new Dictionary<string, object>
            {
                { "start", DateTimeUtil.GetUnixTimeStamp13Digit(startTime) },
                { "end", DateTimeUtil.GetUnixTimeStamp13Digit(endTime) },
            };

            return PrivateCall(apiKey, Path, "GET", query);
        }

        public string GetOpenOrdersRaw(ApiKey apiKey)
        {
            const string Path = "/exchange/client_orders";
            return PrivateCall(apiKey, Path, "GET");
        }

        public string GetTickerRaw()
        {
            const string Path = "/exchange/ticker";
            return PublicCall(Path);
        }

        private static string EncodeLivecoinQuery(string formData)
        {
            return formData.Replace("/", "%2F")
                .Replace("@", "%40")
                .Replace(";", "%3B");
        }

        private static string HashHMAC(string privateKey, string message)
        {
            var encoding = new UTF8Encoding();
            byte[] keyByte = encoding.GetBytes(privateKey);

            var hmacsha256 = new HMACSHA256(keyByte);

            byte[] messageBytes = encoding.GetBytes(message);
            byte[] hashmessage = hmacsha256.ComputeHash(messageBytes);

            return ByteArrayToString(hashmessage);
        }

        private static string ByteArrayToString(byte[] ba)
        {
            var hex = new StringBuilder(ba.Length * 2);
            foreach (byte b in ba) { hex.AppendFormat("{0:x2}", b); }

            return hex.ToString();
        }

        private string PublicCall(string path, Dictionary<string, string> query = null)
        {
            var fullUrl = $"{ApiRoot}{path}";

            if (query?.Keys?.Any() ?? false)
            {
                var queryText = string.Join("&", query.Keys.OrderBy(key => key)
                    .Select(key => $"{key}={query[key]}"));

                fullUrl = fullUrl + $"?{queryText}";
            }

            return _webUtil.Get(fullUrl);
        }

        private string PrivateCall(ApiKey apiKey, string path, string verb, Dictionary<string, object> query = null)
        {
            var url = $"{ApiRoot}{path}";
            if (string.Equals(verb, "GET", StringComparison.InvariantCultureIgnoreCase) && query != null && query.Any())
            {
                var urlQuery = string.Join("&", query.Keys.OrderBy(queryKey => queryKey).Select(queryKey =>
                {
                    var encodedKey = EncodeLivecoinQuery(queryKey);
                    var encodedValue = EncodeLivecoinQuery(query[queryKey].ToString());

                    return $"{encodedKey}={encodedValue}";
                }));

                url += "?" + urlQuery;
            }

            var encodedQuery = query != null && query.Keys.Any()
                ? BuildLivecoinQuery(query)
                : null;

            var signature = HashHMAC(apiKey.Secret, encodedQuery).ToUpper();

            var req = (HttpWebRequest)WebRequest.Create(url);
            req.Method = verb;
            req.Headers.Add("API-key", apiKey.Key);
            req.Headers.Add("Sign", signature);
            req.ContentType = "application/x-www-form-urlencoded";

            if (string.Equals(verb, "POST", StringComparison.InvariantCultureIgnoreCase))
            {
                var queryBytes = Encoding.ASCII.GetBytes(encodedQuery);
                req.ContentLength = queryBytes.Length;

                using (var requestStream = req.GetRequestStream())
                {
                    requestStream.Write(queryBytes, 0, queryBytes.Length);
                }
            }

            using (var resp = req.GetResponse())
            using (var respStream = resp.GetResponseStream())
            using (var reader = new StreamReader(respStream))
            {
                return reader.ReadToEnd();
            }
        }

        private static string BuildLivecoinQuery(Dictionary<string, object> queryData)
        {
            var joinedQuery = string.Join("&", queryData.Keys.OrderBy(queryKey => queryKey).Select(queryKey => $"{queryKey}={queryData[queryKey]}"));
            var encodedQuery = EncodeLivecoinQuery(joinedQuery);

            return encodedQuery;
        }
    }
}
