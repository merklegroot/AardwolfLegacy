using hitbtc_lib.Client.ClientModels;
using hitbtc_lib.Models;
using Newtonsoft.Json;
using RestSharp;
using RestSharp.Authenticators;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using trade_model;
using web_util;

namespace hitbtc_lib.Client
{
    // https://api.hitbtc.com/
    public class HitBtcClient : IHitBtcClient
    {
        private readonly IWebUtil _webUtil;

        public HitBtcClient(IWebUtil webUtil)
        {
            _webUtil = webUtil;
        }

        public string GetSymbols()
        {
            const string Url = "https://api.hitbtc.com/api/2/public/symbol";
            return _webUtil.Get(Url);
        }

        public string GetCurrenciesRaw()
        {
            const string Url = "https://api.hitbtc.com/api/2/public/currency";
            return _webUtil.Get(Url);
        }

        public string GetDepositAddress(ApiKey apiKey, string nativeSymbol)
        {
            const string Root = "https://api.hitbtc.com/api/2";
            var url = $"{Root}/account/crypto/address/{nativeSymbol}";
            return AuthenticatedRequest(apiKey, url);
        }

        public string GetOpenOrdersRaw(ApiKey apiKey)
        {
            const string Url = "https://api.hitbtc.com/api/2/order";
            return AuthenticatedRequest(apiKey, Url);
        }

        public string BuyLimitRaw(ApiKey apiKey, string tradingPairSymbol, decimal quantity, decimal price)
        {
            const string Side = "buy";

            var payload = new HitBtcClientCreateOrderRequest
            {
                Symbol = tradingPairSymbol,
                Side = Side,
                Price = price,
                Quantity = quantity
            };

            var query = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("symbol", tradingPairSymbol),
                new KeyValuePair<string, string>("side", Side),
                new KeyValuePair<string, string>("quantity", quantity.ToString("G29")),
                new KeyValuePair<string, string>("price", price.ToString("G29")),
            };

            var restClient = new RestClient("https://api.hitbtc.com")
            {
                Authenticator = new HttpBasicAuthenticator(apiKey.Key, apiKey.Secret)
            };

            var request = new RestRequest("/api/2/order", Method.POST)
            {
                RequestFormat = DataFormat.Json
            };

            request.AddParameter("symbol", tradingPairSymbol);
            request.AddParameter("side", Side);
            request.AddParameter("quantity", quantity);
            request.AddParameter("price", price);

            var response = restClient.Execute(request);
            if (!response.IsSuccessful)
            {
                var message = $"REQUEST ERROR (Status Code: {response.StatusCode}; Content: {response.Content})";
                throw new Exception(message);
            }

            return response.Content;
        }

        public string SellLimitRaw(ApiKey apiKey, string tradingPairSymbol, decimal quantity, decimal price)
        {
            const string Url = "https://api.hitbtc.com/api/2/order";
            const string Side = "sell";

            var restClient = new RestClient("https://api.hitbtc.com")
            {
                Authenticator = new HttpBasicAuthenticator(apiKey.Key, apiKey.Secret)
            };

            var request = new RestRequest("/api/2/order", Method.POST)
            {
                RequestFormat = DataFormat.Json
            };

            request.AddParameter("symbol", tradingPairSymbol);
            request.AddParameter("side", Side);
            request.AddParameter("quantity", quantity);
            request.AddParameter("price", price);
            //request.AddParameter("type", "market");
            //request.AddParameter("timeInForce", "IOC");

            var response = restClient.Execute(request);
            if (!response.IsSuccessful)
            {
                var message = $"REQUEST ERROR (Status Code: {response.StatusCode}; Content: {response.Content})";
                throw new Exception(message);
            }

            return response.Content;
        }

        public string GetTradeHistoryRaw(ApiKey apiKey)
        {
            var url = "https://api.hitbtc.com/api/2/history/trades?limit=1000"; //?symbol=ETHBTC";
            return AuthenticatedRequest(apiKey, url);
        }

        public List<HitBtcApiTradeHistoryItem> GetTradeHistory(ApiKey apiKey)
        {
            var contents = GetTradeHistoryRaw(apiKey);
            return !string.IsNullOrWhiteSpace(contents)
                ? JsonConvert.DeserializeObject<List<HitBtcApiTradeHistoryItem>>(contents)
                : null;
        }

        public string GetTransactionsHistoryRaw(ApiKey apiKey)
        {
            var url = "https://api.hitbtc.com/api/2/account/transactions?limit=1000";
            return AuthenticatedRequest(apiKey, url, null);
        }

        public List<HitBtcClientTransactionItem> GetTransactionsHistory(ApiKey apiKey)
        {
            var contents = GetTransactionsHistoryRaw(apiKey);
            return !string.IsNullOrWhiteSpace(contents)
                ? JsonConvert.DeserializeObject<List<HitBtcClientTransactionItem>>(contents)
                : null;
        }

        public string AuthenticatedRequest(
            ApiKey apiKey,
            string url,
            string verb = "GET",
            string payload = null)
        {
            var uri = new Uri(url);
            var req = WebRequest.Create(uri);
            if (!string.IsNullOrWhiteSpace(verb)) { req.Method = verb.Trim().ToUpper(); }

            if (!string.IsNullOrWhiteSpace(payload))
            {
                req.ContentType = "application/x-www-form-urlencoded";
                // req.ContentType = "application/json";

                var data = Encoding.UTF8.GetBytes(payload);
                req.ContentLength = data.Length;

                using (var requestStream = req.GetRequestStream())
                {
                    requestStream.Write(data, 0, data.Length);
                }
            }

            var userName = apiKey.Key;
            var password = apiKey.Secret;
            req.Headers["Authorization"] = "Basic " + Convert.ToBase64String(Encoding.Default.GetBytes(userName + ":" + password));

            using (var resp = req.GetResponse())
            {
                if (resp == null) { throw new ApplicationException($"Received null response from request to \"{url}\"."); }
                using (var stream = resp.GetResponseStream())
                {
                    if (stream == null) { throw new ApplicationException($"Received null response stream from request to \"{url}\"."); }

                    using (var reader = new StreamReader(stream))
                    {
                        return reader.ReadToEnd();
                    }
                }
            }
        }

        public string CancelOrderRaw(ApiKey apiKey, string clientOrderId)
        {
            var url = $"https://api.hitbtc.com/api/2/order/{clientOrderId}";
            return AuthenticatedRequest(apiKey, url, "DELETE");
        }
    }
}
