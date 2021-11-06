// https://docs.kucoin.com <- This is new

using KucoinClientModelLib;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using trade_model;
using web_util;

namespace kucoin_lib.Client
{
    public interface IKucoinClient
    {
        string GetServerTimeRaw();

        string GetSymbolsRaw();
        string GetCurrenciesRaw();

        string GetOrderBookRaw(string symbol, string baseSymbol);

        string GetAccountsRaw(KucoinApiKey apiKey);

        KucoinClientGetAccountsResponse GetAccounts(KucoinApiKey apiKey);

        string GetDepositAddress(ApiKey apiKey, string symbol);

        string GetOpenOrders(ApiKey apiKey);

        string CancelOrder(ApiKey apiKey, string orderId, bool isBid, string nativeSymbol, string nativeBaseSymbol);

        KucoinClientCreateOrderResponse CreateOrder(ApiKey apiKey, string nativeSymbol, string nativeBaseSymbol, decimal price, decimal quantity, bool isBid);

        string CreateOrderRaw(ApiKey apiKey, string nativeSymbol, string nativeBaseSymbol, decimal price, decimal quantity, bool isBid);

        string GetAccountLedgersRaw(KucoinApiKey apiKey, string accountId);

        string GetFillsRaw(KucoinApiKey apiKey);

        string GetHistoricalOrdersRaw(KucoinApiKey apiKey, int? page = null);

        KucoinClientGetTradeHistoryResponse GetHistoricalOrders(KucoinApiKey apiKey, int? page = null);
    }

    public class KucoinClient : IKucoinClient
    {
        private const string BaseUrl = "https://openapi-v2.kucoin.com";
    
        private readonly IWebUtil _webUtil;

        public KucoinClient(IWebUtil webUtil)
        {
            _webUtil = webUtil;
        }

        public string GetServerTimeRaw()
        {
            // /api/v1/timestamp
            const string Url = "https://openapi-v2.kucoin.com/api/v1/timestamp";
            return _webUtil.Get(Url);
        }

        public string GetSymbolsRaw()
        {
            const string Url = "https://openapi-v2.kucoin.com/api/v1/symbols";
            return _webUtil.Get(Url);
        }

        public string GetCurrenciesRaw()
        {
            const string Url = "https://openapi-v2.kucoin.com/api/v1/currencies";
            return _webUtil.Get(Url);
        }

        public string GetAccountsRaw(KucoinApiKey apiKey)
        {
            return AuthenticatedRequest(apiKey, "/api/v1/accounts");
        }

        public KucoinClientGetAccountsResponse GetAccounts(KucoinApiKey apiKey)
        {
            var contents = GetAccountsRaw(apiKey);
            return !string.IsNullOrWhiteSpace(contents)
                ? JsonConvert.DeserializeObject<KucoinClientGetAccountsResponse>(contents)
                : null;
        }

        public string GetAccountLedgersRaw(KucoinApiKey apiKey, string accountId)
        {
            return AuthenticatedRequest(apiKey, $"/api/v1/accounts/{accountId}/ledgers");
        }

        public string GetFillsRaw(KucoinApiKey apiKey)
        {
            return AuthenticatedRequest(apiKey, $"/api/v1/fills");
        }

        public string GetHistoricalOrdersRaw(KucoinApiKey apiKey, int? page = null)
        {
            var query = page.HasValue ? $"?currentPage={page}" : string.Empty;
            var url = $"/api/v1/hist-orders{query}";

            return AuthenticatedRequest(apiKey, url);
        }

        public KucoinClientGetTradeHistoryResponse GetHistoricalOrders(KucoinApiKey apiKey, int? page = null)
        {
            var contents = GetHistoricalOrdersRaw(apiKey, page);
            return !string.IsNullOrWhiteSpace(contents)
                ? JsonConvert.DeserializeObject<KucoinClientGetTradeHistoryResponse>(contents)
                : null;
        }

        public string GetCurrentWithdrawalsRaw(KucoinApiKey apiKey)
        {
            return AuthenticatedRequest(apiKey, "/api/v1/withdrawals");
        }

        public string GetV1HistoricalWithdrawalsRaw(KucoinApiKey apiKey, int? page = null)
        {
            var query = page.HasValue ? $"?currentPage={page}" : string.Empty;
            var url = $"/api/v1/hist-withdrawals{query}";

            return AuthenticatedRequest(apiKey, url);
        }

        public KucoinClientGetWithdrawalHistoryResponse GetV1HistoricalWithdrawals(KucoinApiKey apiKey, int? page = null)
        {
            var contents = GetHistoricalOrdersRaw(apiKey, page);
            return !string.IsNullOrWhiteSpace(contents)
                ? JsonConvert.DeserializeObject<KucoinClientGetWithdrawalHistoryResponse>(contents)
                : null;
        }

        public string GetDepositAddress(ApiKey apiKey, string symbol)
        {
            // GET /api/v1/deposit-addresses?currency=<currency>

            // var endpoint = $"/v1/account/{symbol}/wallet/address";
            //var endpoint = $"/v1/deposit-addresses?currency={symbol}";
            //return AuthenticatedRequest(apiKey, endpoint);

            throw new NotImplementedException();
        }

        public string GetOpenOrders(ApiKey apiKey)
        {
            //// https://api.kucoin.com/v1/order/active-map
            //const string Endpoint = "/v1/order/active-map";

            //return AuthenticatedRequest(apiKey, Endpoint);

            throw new NotImplementedException();
        }

        public string CancelOrder(ApiKey apiKey, string orderId, bool isBid, string nativeSymbol, string nativeBaseSymbol)
        {
            //const string Endpoint = "/v1/cancel-order";

            //var combo = $"{nativeSymbol.ToUpper()}-{nativeBaseSymbol.ToUpper()}";

            //var query = new Dictionary<string, string>
            //{
            //    { "orderOid", orderId },
            //    { "symbol", combo },
            //    { "type", isBid ? "BUY" : "SELL" }
            //};

            //return AuthenticatedRequest(apiKey, Endpoint, query, "POST");

            throw new NotImplementedException();
        }

        public KucoinClientCreateOrderResponse CreateOrder(ApiKey apiKey, string nativeSymbol, string nativeBaseSymbol, decimal price, decimal quantity, bool isBid)
        {
            var response = CreateOrderRaw(apiKey, nativeSymbol, nativeBaseSymbol, price, quantity, isBid);
            return !string.IsNullOrWhiteSpace(response)
                ? JsonConvert.DeserializeObject<KucoinClientCreateOrderResponse>(response)
                : null;
        }

        public string CreateOrderRaw(ApiKey apiKey, string nativeSymbol, string nativeBaseSymbol, decimal price, decimal quantity, bool isBid)
        {
            //var combo = $"{nativeSymbol.ToUpper()}-{nativeBaseSymbol.ToUpper()}";

            //string Endpoint = $"/v1/order"; //?symbol={combo}";

            //var query = new Dictionary<string, string>
            //{
            //    { "amount", quantity.ToString() },
            //    { "price", price.ToString() },
            //    { "symbol", combo },
            //    { "type", isBid ? "BUY" : "SELL" }
            //};

            //return AuthenticatedRequest(apiKey, Endpoint, query, "POST");

            throw new NotImplementedException();
        }        

        public string GetOrderBookRaw(string symbol, string baseSymbol)
        {
            var combo = $"{symbol.ToUpper()}-{baseSymbol.ToUpper()}";
            var url = $"https://openapi-v2.kucoin.com/api/v1/market/orderbook/level2_20?symbol={combo}";

            return _webUtil.Get(url);
        }

        private string AuthenticatedRequest(
            KucoinApiKey apiKey, 
            string endpoint,
            Dictionary<string, string> query = null,
            string verb = "GET")
        {
            const string Host = "https://openapi-v2.kucoin.com";
            var fullUri = $"{Host}{endpoint}";

            var req = (HttpWebRequest)WebRequest.Create(fullUri);
            if (!string.IsNullOrWhiteSpace(verb)) { req.Method = verb; }

            var keys = query?.Keys != null ? query.Keys.OrderBy(item => item).ToList() : new List<string>();

            var queryString = query != null && query.Any()
                ? string.Join("&", query.Keys.OrderBy(key => key).Select(key => $"{key}={query[key]}").ToList())
                : null;

            var nonce = GenerateNonce();

            var signature = GetSignature(apiKey, endpoint, verb, nonce, queryString);

            req.Headers.Add("KC-API-KEY", apiKey.PublicKey);
            req.Headers.Add("KC-API-TIMESTAMP", nonce);
            req.Headers.Add("KC-API-SIGN", signature);
            req.Headers.Add("KC-API-PASSPHRASE", apiKey.Passphrase);

            using (var response = req.GetResponse())
            using (var responseStream = response.GetResponseStream())
            using (var streamReader = new StreamReader(responseStream))
            {
                return streamReader.ReadToEnd();
            }
        }

        private string GetSignature(
            KucoinApiKey apiKey,
            string endpoint,
            string verb,
            string nonce,
            string queryString)
        {
            var effectiveVerb = string.IsNullOrWhiteSpace(verb) ? "GET" : verb.Trim().ToUpper();

            var stringForSigning = $"{nonce}{effectiveVerb}{endpoint}{queryString}";
            var bytesForSigning = Encoding.UTF8.GetBytes(stringForSigning);

            var privateKeyBytes = Encoding.UTF8.GetBytes(apiKey.PrivateKey);
            using (var hmac = new HMACSHA256(privateKeyBytes))
            {
                return Convert.ToBase64String(hmac.ComputeHash(bytesForSigning));
            }            
        }

        private string GenerateNonce()
        {
            var dawnOfComputing = new DateTime(1970, 1, 1);
            var currentTime = DateTime.UtcNow;
            
            return ((long)(currentTime - dawnOfComputing).TotalMilliseconds).ToString();
        }
    }
}
