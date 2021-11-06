using crypt_lib;
using date_time_lib;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using trade_model;

namespace qryptos_lib.Client
{
    public interface IQryptosClient
    {
        string GetOpenOrders(ApiKey apiKey);
        string GetOpenOrders(ApiKey apiKey, long productId);

        string BuyLimit(ApiKey apiKey, long productId, decimal price, decimal quantity);
        string SellLimit(ApiKey apiKey, long productId, decimal price, decimal quantity);
        string CancelOrder(ApiKey apiKey, string orderId);

        string GetCryptoAccounts(ApiKey apiKey);
        string GetCryptoAccount(ApiKey apiKey, long id);
        string GetTradingAccounts(ApiKey apiKey);

        string GetAccount(ApiKey apiKey, string symbol);
    }

    // https://developers.quoine.com/
    // https://liquid-docs.readthedocs.io/en/latest/restapi.html
    public class QryptosClient : IQryptosClient
    {
        private const int MaxLimit = 1000;

        public string BuyLimit(ApiKey apiKey, long productId, decimal price, decimal quantity)
        {
            return PlaceLimitOrder(apiKey, productId, "buy", price, quantity);
        }

        public string SellLimit(ApiKey apiKey, long productId, decimal price, decimal quantity)
        {
            return PlaceLimitOrder(apiKey, productId, "sell", price, quantity);
        }

        public string GetCryptoAccounts(ApiKey apiKey)
        {
            const string Path = "/crypto_accounts";
            return PerformAuthRequest(apiKey, Path);
        }

        public string GetCryptoAccount(ApiKey apiKey, long id)
        {
            var path = $"/crypto_accounts/{id}";
            return PerformAuthRequest(apiKey, path);
        }

        public string GetTradingAccounts(ApiKey apiKey)
        {
            const string Path = "/trading_accounts";
            return PerformAuthRequest(apiKey, Path);
        }

        public string GetOpenOrders(ApiKey apiKey)
        {
            var queryParams = new Dictionary<string, string>();
            queryParams["status"] = "live";

            var query = QueryToText(queryParams);

            var path = $"/orders{query}";

            return PerformAuthRequest(apiKey, path);
        }

        public string GetOpenOrders(ApiKey apiKey, long productId)
        {
            var queryParams = new Dictionary<string, string>();
            queryParams["product_id"] = productId.ToString();
            queryParams["status"] = "live";

            var query = QueryToText(queryParams);

            var path = $"/orders{query}";

            return PerformAuthRequest(apiKey, path);
        }

        public string CancelOrder(ApiKey apiKey, string orderId)
        {
            var path = $"/orders/{orderId}/cancel";
            return PerformAuthRequest(apiKey, path, "PUT");
        }

        public string GetAccount(ApiKey apiKey, string symbol)
        {
            var path = $"/accounts/{symbol.ToUpper()}";
            return PerformAuthRequest(apiKey, path);
        }

        public string PerformAuthRequest(
            ApiKey apiKey, 
            string path,
            string verb = "GET",
            string postText = null)
        {
            var url = $"https://api.liquid.com{path}";

            var headerText = "{\"alg\":\"HS256\"}";
            var headerBytes = Encoding.Default.GetBytes(headerText);
            var base64Header = Convert.ToBase64String(headerBytes);

            var authPayload = new QryptosAuthPayload
            {
                Path = path,
                Nonce = GetNonce(),
                TokenId = apiKey.Key
            };

            var serializedPayload = JsonConvert.SerializeObject(authPayload);
            var payloadBytes = Encoding.Default.GetBytes(serializedPayload);
            var base64Payload = Convert.ToBase64String(payloadBytes);
            var secretBytes = Encoding.Default.GetBytes(apiKey.Secret);

            var jwt = JwtUtil.Encode(serializedPayload, apiKey.Secret);

            var request = (HttpWebRequest)WebRequest.Create(url);
            if (verb != null && !string.Equals(verb, "GET", StringComparison.InvariantCultureIgnoreCase))
            {
                request.Method = verb;
            }

            request.Headers.Add("X-Quoine-API-Version", "2");
            request.Headers.Add("X-Quoine-Auth", jwt);
            request.ContentType = "application/json";
            if (!string.IsNullOrWhiteSpace(postText))
            {
                var postBytes = Encoding.ASCII.GetBytes(postText);
                request.ContentLength = postBytes.Length;

                using (var requestStream = request.GetRequestStream())
                {
                    requestStream.Write(postBytes, 0, postBytes.Length);
                }
            }

            using (var response = request.GetResponse())
            using (var responseStream = response.GetResponseStream())
            using (var reader = new StreamReader(responseStream))
            {
                return reader.ReadToEnd();
            }
        }

        private string PlaceLimitOrder(ApiKey apiKey, long productId, string side, decimal price, decimal quantity)
        {
            //var req = //new QryptosPlaceOrderRequest
            ////{
            //    //Order = 
            //    new QryptosPlaceOrderRequest.OrderPayload
            //    {
            //        ProductId = productId,
            //        OrderType = "limit",
            //        Price = price,
            //        Quantity = quantity,
            //        Side = side
            //    //}
            //};

            var req = new
            {
                order = new
                {
                    order_type = "limit",
                    product_id = productId,
                    side = side,
                    quantity = quantity.ToString(),
                    price = price.ToString()
                }
            };

            var serializedReq = JsonConvert.SerializeObject(req
                //, Formatting.Indented
                );

            const string Path = "/orders/";

            var responseText = PerformAuthRequest(apiKey, Path, "POST", serializedReq);

            return responseText;
        }

        private string QueryToText(Dictionary<string, string> queryParams)
        {
            if (queryParams == null || !queryParams.Any()) { return null; }

            return "?" + string.Join("&", queryParams.Keys.OrderBy(key => key).Select
                (key => $"{key}={queryParams[key]}"));
        }

        private string GetNonce() => DateTimeUtil.GetUnixMillisecondsTimeStamp().ToString();
    }
}
