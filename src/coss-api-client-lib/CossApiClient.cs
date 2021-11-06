using coss_api_client_lib.Models;
using date_time_lib;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using trade_model;

namespace coss_api_client_lib
{
    // https://api.coss.io/v1/spec
    public class CossApiClient : ICossApiClient
    {
        // private const int DefaultReceiveWindow = 5000;
        private const int DefaultReceiveWindow = 10000;

        private const string EngineRoot = "https://engine.coss.io/api/v1/";
        private const string TradeRoot = "https://trade.coss.io/c/api/v1/";
        private const string LegacyRoot = "https://api.coss.io/v1/";

        public string GetExchangeInfoRaw()
        {
            const string Path = "exchange-info";
            var url = $"{TradeRoot}{Path}";
            return HttpGet(url);
        }

        public string GetWebCoinsRaw()
        {
            const string Url = "https://coss.io/c/coins/getinfo/all";
            return HttpGet(Url);
        }

        public string GetMarketSummariesRaw(string symbol, string baseSymbol)
        {
            if (string.IsNullOrWhiteSpace(symbol)) { throw new ArgumentNullException(nameof(symbol)); }
            if (string.IsNullOrWhiteSpace(baseSymbol)) { throw new ArgumentNullException(nameof(baseSymbol)); }

            var tradingPairText = GetTradingPairSymbol(symbol, baseSymbol);
            var encodedTradingPairText = HttpUtility.UrlEncode(tradingPairText);

            var url = $"https://api.coss.io/v1/getmarketsummaries?symbol={encodedTradingPairText}";
            return HttpGet(url);
        }

        public string GetOrderBookRaw(string symbol, string baseSymbol)
        {
            if (string.IsNullOrWhiteSpace(symbol)) { throw new ArgumentNullException(nameof(symbol)); }
            if (string.IsNullOrWhiteSpace(baseSymbol)) { throw new ArgumentNullException(nameof(baseSymbol)); }

            var tradingPairText = GetTradingPairSymbol(symbol, baseSymbol);
            var encodedTradingPairText = HttpUtility.UrlEncode(tradingPairText);

            var url = $"{EngineRoot}dp?symbol={encodedTradingPairText}";
            return HttpGet(url);
        }

        public string GetBalanceRaw(ApiKey apiKey)
        {
            const string Path = "account/balances";
            return AuthenticatedGet(TradeRoot, apiKey, Path);
        }

        public string GetOpenOrdersRaw(ApiKey apiKey, string symbol, string baseSymbol)
        {
            const string Path = "order/list/open";

            return AuthenticatedPost(TradeRoot, apiKey, Path, () =>
            {
                var cossSymbol = $"{symbol}-{baseSymbol}".ToLower();
                var nonce = GenerateNonce();

                var payload = new Dictionary<string, object>
                {
                    { "limit", 10 },
                    { "page", 0 },
                    { "symbol", cossSymbol.ToLower() },
                    { "timestamp", nonce },
                    { "recvWindow", DefaultReceiveWindow }
                };

                return  JsonConvert.SerializeObject(payload);
            });
        }

        public CossApiGetOpenOrdersResponseMessage GetOpenOrders(ApiKey apiKey, string symbol, string baseSymbol)
        {
            var contents = GetOpenOrdersRaw(apiKey, symbol, baseSymbol);
            return JsonConvert.DeserializeObject<CossApiGetOpenOrdersResponseMessage>(contents);
        }

        public string CreateOrderRaw(ApiKey apiKey, string symbol, string baseSymbol, decimal price, decimal quantity, bool isBid)
        {
            const string Path = "/order/add";

            var tradingPairSymbol = GetTradingPairSymbol(symbol, baseSymbol);            

            return AuthenticatedPost(TradeRoot, apiKey, Path, () =>
            {
                var payload = new CreateApiOrderRequestMessage
                {
                    OrderSymbol = tradingPairSymbol,
                    OrderPrice = price.ToString("G29"),
                    OrderSide = isBid ? "BUY" : "SELL",
                    OrderSize = quantity.ToString("G29"),
                    Type = "limit",
                    TimeStamp = GenerateNonce(),
                    RecvWindow = DefaultReceiveWindow
                };

                return JsonConvert.SerializeObject(payload);
            });
        }

        public CreateApiOrderResponseMessage CreateOrder(ApiKey apiKey, string symbol, string baseSymbol, decimal price, decimal quantity, bool isBid)
        {
            var contents = CreateOrderRaw(apiKey, symbol, baseSymbol, price, quantity, isBid);
            return contents != null
                ? JsonConvert.DeserializeObject<CreateApiOrderResponseMessage>(contents)
                : null;
        }        

        public string CancelOrderRaw(ApiKey apiKey, string symbol, string baseSymbol, string orderId)
        {
            const string Path = "order/cancel";
            const string Verb = "DELETE";            

            return AuthenticatedPost(TradeRoot, apiKey, Path, () =>
            {
                var combo = $"{symbol}_{baseSymbol}".ToUpper();

                var payload = new CancelOrderRequest
                {
                    OrderId = orderId,
                    OrderSymbol = combo,
                    TimeStamp = GenerateNonce(),
                    RecvWindow = DefaultReceiveWindow
                };

                return JsonConvert.SerializeObject(payload);
            }, Verb);
        }

        public CossApiGetCompletedOrdersResponse GetCompletedOrders(ApiKey apiKey, string symbol, string baseSymbol, int? limit = null, int? page = null)
        {
            var contents = GetCompletedOrdersRaw(apiKey, symbol, baseSymbol, limit, page);
            var data = JsonConvert.DeserializeObject<CossApiGetCompletedOrdersResponse>(contents);

            return data;
        }

        public string GetCompletedOrdersRaw(ApiKey apiKey, string symbol, string baseSymbol, int? limit = null, int? page = null)
        {
            const string Path = "order/list/completed";

            return AuthenticatedPost(TradeRoot, apiKey, Path, () =>
            {
                var combo = $"{symbol}_{baseSymbol}".ToUpper();

                var payload = new CossApiGetCompletedOrdersRequest
                {
                    Symbol = combo,
                    RecvWindow = DefaultReceiveWindow,
                    TimeStamp = GenerateNonce(),
                    Limit = limit,
                    Page = page
                };

                return JsonConvert.SerializeObject(payload);
            });
        }

        public string GetServerTimeRaw()
        {
            const string Path = "time";
            var url = $"{TradeRoot}{Path}";
            return HttpGet(url);
        }

        public long GetServerTime()
        {
            var contents = GetServerTimeRaw();
            var serverTimeResponse = JsonConvert.DeserializeObject<Dictionary<string, long>>(contents);
            return serverTimeResponse["server_time"];
        }

        public string GetAccountDetailsRaw(ApiKey apiKey)
        {
            const string Path = "account/details";
            // var url = $"{TradeRoot}{Path}";

            return AuthenticatedGet(TradeRoot, apiKey, Path);
        }

        private static long _nonceAdjustment = 0;

        public void SynchronizeTime()
        {
            var startTime = DateTime.UtcNow;
            var serverTime = GetServerTime();
            var stopTime = DateTime.UtcNow;

            var startTimeAsNonce = DateTimeUtil.GetUnixTimeStamp13Digit(startTime);
            var stopTimeAsNonce = DateTimeUtil.GetUnixTimeStamp13Digit(stopTime);

            var millisAfterStart = serverTime - startTimeAsNonce;
            var millisBeforeStop = stopTimeAsNonce - serverTime;

            var roundTrip = stopTimeAsNonce - startTimeAsNonce;

            if (serverTime < startTimeAsNonce)
            {
                _nonceAdjustment = serverTime - startTimeAsNonce - 25;
            }
        }

        private string GetTradingPairSymbol(string symbol, string baseSymbol)
        {
            if (string.IsNullOrWhiteSpace(symbol)) { throw new ArgumentNullException(nameof(symbol)); }
            if (string.IsNullOrWhiteSpace(baseSymbol)) { throw new ArgumentNullException(nameof(baseSymbol)); }

            return $"{symbol.Trim().ToUpper()}_{baseSymbol.Trim().ToUpper()}";
        }

        private static object NonceLock = new object();

        private string AuthenticatedGet(string rootUrl, ApiKey apiKey, string path, Dictionary<string, object> pay = null, string verb = null)
        {
            lock (NonceLock)
            {
                var unjoined = new Dictionary<string, object>();
                if (pay != null)
                {
                    foreach (var key in pay.Keys)
                    {
                        unjoined[key] = pay[key];
                    }
                }

                unjoined["recvWindow"] = DefaultReceiveWindow;
                unjoined["timestamp"] = GenerateNonce();

                var payload = string.Join("&", unjoined.Keys.OrderBy(key => key).Select(key => $"{key}={unjoined[key]}"));
                var url = $"{rootUrl}{path}?{payload}";

                var signature = SignPayload(apiKey.Secret, payload);

                var req = (HttpWebRequest)WebRequest.Create(url);

                req.ContentType = "application/json";
                req.Headers.Add("X-Requested-With", "XMLHttpRequest");
                req.Headers.Add("Authorization", apiKey.Key);
                req.Headers.Add("Signature", signature);

                if (!string.IsNullOrWhiteSpace(verb) && !string.Equals(verb, "GET", StringComparison.InvariantCultureIgnoreCase))
                { req.Method = verb; }

                var response = req.GetResponse();
                var responseStream = response.GetResponseStream();
                var reader = new StreamReader(responseStream);
                var contents = reader.ReadToEnd();

                return contents;
            }
        }

        private string AuthenticatedPost(string rootUrl, ApiKey apiKey, string path, Func<string> payloadGenerator, string verb = null)
        {
            lock (NonceLock)
            {
                var url = $"{rootUrl}{path}";

                var payloadText = payloadGenerator != null ? payloadGenerator (): null;
                var payloadBytes = Encoding.ASCII.GetBytes(payloadText);
                var signature = SignPayload(apiKey.Secret, payloadText);

                var client = new HttpClient();
                var req = (HttpWebRequest)WebRequest.Create(url);
                req.Method = !string.IsNullOrWhiteSpace(verb) ? verb : "POST";

                req.ContentType = "application/json";
                req.Headers.Add("X-Requested-With", "XMLHttpRequest");
                req.Headers.Add("Authorization", apiKey.Key);
                req.Headers.Add("Signature", signature);

                req.ContentLength = payloadBytes.Length;
                using (var reqStream = req.GetRequestStream())
                {
                    reqStream.Write(payloadBytes, 0, payloadBytes.Length);
                }

                var response = req.GetResponse();
                var responseStream = response.GetResponseStream();
                var reader = new StreamReader(responseStream);
                var contents = reader.ReadToEnd();

                return contents;
            }
        }

        private string SignPayload(string privateKey, string payload)
        {
            var privateKeyBytes = Encoding.ASCII.GetBytes(privateKey);
            var payloadBytes = Encoding.ASCII.GetBytes(payload);
            var sha = new HMACSHA256(privateKeyBytes);
            var signatureBytes = sha.ComputeHash(payloadBytes);
            var hexSignature = ByteArrayToHexString(signatureBytes);

            return hexSignature;
        }

        private static DateTime? LastSyncTime = null;
        private static object GenerateNonceLocker = new object();

        // 13 digit unix timestamp
        private string GenerateNonce()
        {
            lock (GenerateNonceLocker)
            {
                if (!LastSyncTime.HasValue || (DateTime.UtcNow - LastSyncTime >= TimeSpan.FromMinutes(10)))
                {
                    SynchronizeTime();
                    LastSyncTime = DateTime.UtcNow;
                }

                var nonce = DateTimeUtil.GetUnixTimeStamp13Digit() + _nonceAdjustment;
                return nonce.ToString();
            }
        }

        /// <summary>
        /// Converts an array of bytes to a Hex string.
        /// There is no prefix.
        /// All Hex digits are upper case.
        /// e.g. { 123, 234 } => "78EA"
        /// </summary>
        /// <param name="data">An array of bytes</param>
        /// <returns>The bytes represented as a hex string.</returns>
        public static string ByteArrayToHexString(byte[] data)
        {
            return string.Join(string.Empty, data.Select(queryHashedByte => string.Format("{0:x2}", queryHashedByte)));
        }

        private string HttpGet(string url)
        {
            var req = (HttpWebRequest)WebRequest.Create(url);

            using (var response = req.GetResponse())
            using (var responseStream = response.GetResponseStream())
            using (var reader = new StreamReader(responseStream))
            {
                return reader.ReadToEnd();
            }
        }
    }
}
