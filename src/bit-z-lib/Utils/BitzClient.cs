using binary_lib;
using bit_z_lib.Models;
using bit_z_lib.Utils;
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

namespace bit_z_lib
{
    public interface IBitzClient
    {
        string GetBalances(ApiKey apiKey);
        BitzGetOpenOrdersResponse GetUserNowEntrustSheet(ApiKey apiKey, int pageNumber, int pageSize);
        List<BitzGetOpenOrdersResponse> GetOpenOrderResponses(ApiKey apiKey);
        string SellLimit(ApiKey apiKey, string bitzTradingPassword, string symbol, string baseSymbol, decimal quantity, decimal price);
        string BuyLimit(ApiKey apiKey, string bitzTradingPassword, string symbol, string baseSymbol, decimal quantity, decimal price);
        string PlaceLimit(ApiKey apiKey, string bitzTradingPassword, string symbol, string baseSymbol, decimal quantity, decimal price, bool isBid);
        string CancelOrder(ApiKey apiKey, string orderId);
    }

    // https://apidoc.bit-z.pro/en/
    // https://support.bit-z.pro/hc/en-us/categories/115000213454-Guide
    // https://www.bitz.com/fee?welcome -- Fee list
    // Fee for buying is 0%
    // Fee for selling is 0.2%
    public class BitzClient : IBitzClient
    {
        private const string ApiRoot = "https://apiv2.bitz.com/";
        private const string BitzBuyCode = "1";
        private const string BitzSellCode = "2";

        public string GetBalances(ApiKey apiKey)
        {
            var url = $"{ApiRoot}Assets/getUserAssets";
            var contents = BitzAuthenticatedRequest<string>(apiKey, url);
            return contents;
        }

        public string SellLimit(ApiKey apiKey, string bitzTradingPassword, string symbol, string baseSymbol, decimal quantity, decimal price)
        {
            return PlaceLimit(apiKey, bitzTradingPassword, symbol, baseSymbol, quantity, price, false);
        }

        public string BuyLimit(ApiKey apiKey, string bitzTradingPassword, string symbol, string baseSymbol, decimal quantity, decimal price)
        {
            return PlaceLimit(apiKey, bitzTradingPassword, symbol, baseSymbol, quantity, price, true);
        }

        public string PlaceLimit(ApiKey apiKey, string bitzTradingPassword, string symbol, string baseSymbol, decimal quantity, decimal price, bool isBid)
        {
            // https://apiv2.bitz.com/Trade/addEntrustSheet
            var url = $"{ApiRoot}Trade/addEntrustSheet";

            /*
apiKey	yes	string	user request for apiKey
timeStamp	yes	string	current timeStamp
nonce	yes	string	random 6 bit character
sign	yes	string	signature of request parameter
type	yes	string	buy type 1 buy 2 sell
price	yes	float	commission price
number	yes	float	quantity entrusted
symbol	yes	string	transaction pair eth_btc、ltc_btc
tradePwd	yes	string	transaction password
            */

            var combo = $"{symbol.ToLower()}_{baseSymbol.ToLower()}";

            var queryParams = new Dictionary<string, string>
            {
                { "type", isBid ? BitzBuyCode : BitzSellCode },
                { "price", price.ToString() },
                { "number", quantity.ToString() },
                { "symbol", combo },
                { "tradePwd", bitzTradingPassword },
            };

            return BitzAuthenticatedRequest<string>(apiKey, url, queryParams);
        }

        // https://apidoc.bit-z.pro/en/market-trade-data/Get-now-trust.html
        public BitzGetOpenOrdersResponse GetUserNowEntrustSheet(ApiKey apiKey, int pageNumber, int pageSize)
        {
            var url = $"{ApiRoot}Trade/getUserNowEntrustSheet";
            var queryParams = new Dictionary<string, string>
            {
                { "pageSize", pageSize.ToString() },
                { "pageNumber", pageNumber.ToString() }
            };

            return BitzAuthenticatedRequest<BitzGetOpenOrdersResponse>(apiKey, url);
        }

        public List<BitzGetOpenOrdersResponse> GetOpenOrderResponses(ApiKey apiKey)
        {
            const int PageSize = 10;
            const int MaxPages = 10;
            int pageNumber = 1;

            var responses = new List<BitzGetOpenOrdersResponse>();

            BitzGetOpenOrdersResponse response = null;
            do
            {
                response = GetUserNowEntrustSheet(apiKey, pageNumber, PageSize);
                if (response != null) { responses.Add(response); }

                pageNumber++;
            } while (
                (response?.Data?.PageInfo?.TotalCount ?? 0) >= PageSize
                && pageNumber <= MaxPages);

            return responses;
        }

        public string CancelOrder(ApiKey apiKey, string orderId)
        {
            const string Url = "https://apiv2.bitz.com/Trade/cancelEntrustSheet";

            var dictionary = new Dictionary<string, string>
            {
                { "entrustSheetId", orderId }
            };

            var response = BitzAuthenticatedRequest<string>(apiKey, Url, dictionary);
            return response;
        }

        public string GetHistory(ApiKey apiKey, DateTime? startTime = null, DateTime? endTime = null, int? page = null)
        {
            const string Url = "https://apiv2.bitz.com/Trade/getUserHistoryEntrustSheet";

            var dictionary = new Dictionary<string, string>
            {
                //page    No  integer current page number

                //pageSize    No  integer Number of impressions per page Maximum 100
                { "pageSize", "100" }
                //startTime   No  string  Start timestamp
                //endTime No  string  End timestamp
            };

            if (page.HasValue)
            {
                dictionary["page"] = page.Value.ToString();
            }

            if (startTime.HasValue)
            {
                dictionary["startTime"] = DateTimeUtil.GetUnixTimeStamp(startTime.Value).ToString();
            }

            if (endTime.HasValue)
            {
                dictionary["endTime"] = DateTimeUtil.GetUnixTimeStamp(endTime.Value).ToString();
            }

            var response = BitzAuthenticatedRequest<string>(apiKey, Url, dictionary);
            return response;
        }

        private T BitzAuthenticatedRequest<T>(
            ApiKey apiKey,
            string url,
            Dictionary<string, string> queryParams = null)
            where T : class
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

            return HttpPostWithParameters<T>(url, effectiveQueryParams);
        }

        private T HttpPostWithParameters<T>(string url, Dictionary<string, string> queryParameters)
            where T : class
        {
            var request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";

            if (queryParameters != null && queryParameters.Any())
            {
                var queryText = CombineQueryParameters(queryParameters);
                if (!string.IsNullOrWhiteSpace(queryText))
                {
                    var queryData = Encoding.UTF8.GetBytes(queryText);
                    request.ContentLength = queryData.Length;

                    using (var requestStream = request.GetRequestStream())
                    {
                        requestStream.Write(queryData, 0, queryData.Length);
                    }
                }
            }

            using (var response = (HttpWebResponse)request.GetResponse())
            using (var reader = new StreamReader(response.GetResponseStream()))
            {
                var responseText = reader.ReadToEnd();
                if (typeof(T) == typeof(string))
                {
                    return responseText as T;
                }                

                return !string.IsNullOrWhiteSpace(responseText)
                    ? JsonConvert.DeserializeObject<T>(responseText)
                    : default(T);
            }
        }

        private string BuildSignature(string privateKey, Dictionary<string, string> queryDictionary)
        {
            var signatureText = CombineQueryParameters(queryDictionary) + privateKey;
            var signatureHash = Md5Util.GetMd5Hash(signatureText);
            return BinaryUtil.ByteArrayToHexString(signatureHash);
        }

        /// <summary>
        /// Combines query parameters in a Url format with the & separator.
        /// It does not include the ? prefix.
        /// 
        /// For example:
        /// Given that queryParmeters is { { "firstName": "bob" }, { "lastName": "smith" } }
        /// Then this method should return "firstName=bob&lastName=smith"
        /// </summary>
        private string CombineQueryParameters(Dictionary<string, string> queryParameters)
        {
            var sortedKeys = queryParameters.Keys.OrderBy(queryKey => queryKey).ToList();
            return string.Join("&", sortedKeys.Select(queryParam => $"{queryParam}={queryParameters[queryParam]}"));
        }
    }
}
