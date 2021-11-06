using BlocktradeExchangeLib.Models;
using BlocktradeExchangeLib.Requests;
using BlocktradeExchangeLib.Util;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace BlocktradeExchangeLib
{
    // https://trade.blocktrade.com/api_documentation
    public class BlocktradeClient : IBlocktradeClient
    {
        private const string ApiRoot = "https://trade.blocktrade.com/api/v1/";
        private readonly IWebClient _webClient;

        public BlocktradeClient(IWebClient webUtil = null)
        {
            _webClient = webUtil ?? new WebClient();
        }

        public string GetTradingAssetsRaw()
        {
            var url = $"{ApiRoot}trading_assets";
            return _webClient.HttpGet(url);
        }

        public List<BlocktradeAsset> GetTradingAssets()
        {
            var contents = GetTradingAssetsRaw();
            return !string.IsNullOrWhiteSpace(contents)
                ? JsonConvert.DeserializeObject<List<BlocktradeAsset>>(contents)
                : new List<BlocktradeAsset>();
        }

        public string GetTradingPairsRaw()
        {
            var url = $"{ApiRoot}trading_pairs";
            return _webClient.HttpGet(url);
        }

        public List<BlocktradeTradingPair> GetTradingPairs()
        {
            var contents = GetTradingPairsRaw();
            return !string.IsNullOrWhiteSpace(contents)
                ? JsonConvert.DeserializeObject<List<BlocktradeTradingPair>>(contents)
                : new List<BlocktradeTradingPair>();
        }

        public string GetOrderBookRaw(int tradingPairId)
        {
            var url = $"{ApiRoot}order_book/{tradingPairId}";
            return _webClient.HttpGet(url);
        }

        public BlocktradeOrderBook GetOrderBook(int tradingPairId)
        {
            var contents = GetOrderBookRaw(tradingPairId);
            return !string.IsNullOrWhiteSpace(contents)
                ? JsonConvert.DeserializeObject<BlocktradeOrderBook>(contents)
                : new BlocktradeOrderBook();
        }

        public string GetUserRaw(BlocktradeApiKey apiKey)
        {
            var url = $"{ApiRoot}user";
            return AuthenticatedGet(apiKey, url);
        }

        public BlocktradeUser GetUser(BlocktradeApiKey apiKey)
        {
            var contents = GetUserRaw(apiKey);
            return !string.IsNullOrWhiteSpace(contents)
                ? JsonConvert.DeserializeObject<BlocktradeUser>(contents)
                : new BlocktradeUser();
        }

        public string GetUserPortfoliosRaw(BlocktradeApiKey apiKey)
        {
            var url = $"{ApiRoot}portfolios";
            return AuthenticatedGet(apiKey, url);
        }

        public List<BlocktradePortfolio> GetUserPortfolios(BlocktradeApiKey apiKey)
        {
            var contents = GetUserPortfoliosRaw(apiKey);
            return !string.IsNullOrWhiteSpace(contents)
                ? JsonConvert.DeserializeObject<List<BlocktradePortfolio>>(contents)
                : new List<BlocktradePortfolio>();
        }

        public string CancelActiveRaw(BlocktradeApiKey apiKey)
        {
            var url = $"{ApiRoot}orders/cancel_active";
            return AuthenticatedPost(apiKey, url);
        }

        public BlocktradeMessage CancelActive(BlocktradeApiKey apiKey)
        {
            var contents = CancelActiveRaw(apiKey);
            return !string.IsNullOrWhiteSpace(contents)
                ? JsonConvert.DeserializeObject<BlocktradeMessage>(contents)
                : new BlocktradeMessage();
        }

        public string GetUserOrdersRaw(BlocktradeApiKey apiKey, List<string> statuses = null)
        {            
            var url = $"{ApiRoot}orders";
            if(statuses != null && statuses.Any())
            {
                var joined = string.Join(",", statuses);
                var encoded = HttpUtility.UrlEncode(joined);
                url += $"?status={encoded}";       
            }

            return AuthenticatedGet(apiKey, url);
        }

        public string GetUserActivitiesRaw(BlocktradeApiKey apiKey)
        {
            var url = $"{ApiRoot}activities";

            return AuthenticatedGet(apiKey, url);
        }

        public string GetUserActivityDetailRaw(BlocktradeApiKey apiKey, long id)
        {
            var url = $"{ApiRoot}activities/{id}";

            return AuthenticatedGet(apiKey, url);
        }

        public BlocktradeGetUserOrdersResponse GetUserOrders(BlocktradeApiKey apiKey, List<string> statuses = null)
        {
            var contents = GetUserOrdersRaw(apiKey);
            return !string.IsNullOrWhiteSpace(contents)
                ? JsonConvert.DeserializeObject<BlocktradeGetUserOrdersResponse>(contents)
                : new BlocktradeGetUserOrdersResponse();
        }

        public string CancelOrderRaw(BlocktradeApiKey apiKey, long orderId)
        {
            var url = $"{ApiRoot}orders/{orderId}/cancel";
            return AuthenticatedPost(apiKey, url);
        }

        public BlocktradeMessage CancelOrder(BlocktradeApiKey apiKey, long orderId)
        {
            var contents = CancelOrderRaw(apiKey, orderId);

            return !string.IsNullOrWhiteSpace(contents)
                ? JsonConvert.DeserializeObject<BlocktradeMessage>(contents)
                : new BlocktradeMessage();
        }

        public string PlaceOrderRaw(
            BlocktradeApiKey apiKey,
            int portfolioId,
            string direction,
            string type,
            int tradingPairId,
            decimal amount,
            decimal price,
            string timeInForce,
            string stopPrice)
        {
            var url = $"{ApiRoot}orders";

            var requestModel = new PlaceOrderRequest
            {
                PortfolioId = portfolioId,
                Direction = direction,
                Type = type,
                TradingPairId = tradingPairId,
                Amount = amount.ToString("G29"),
                Price = price.ToString("G29"),
                TimeInForce = timeInForce,
                StopPrice = stopPrice                
            };

            var json = JsonConvert.SerializeObject(requestModel);
            return AuthenticatedPost(apiKey, url, json);
        }

        public BlocktradePlaceOrderResponse PlaceOrder(
            BlocktradeApiKey apiKey,
            int portfolioId,
            string direction,
            string type,
            int tradingPairId,
            decimal amount,
            decimal price,
            string timeInForce,
            string stopPrice)
        {
            var response = PlaceOrderRaw(apiKey, portfolioId, direction, type, tradingPairId, amount, price, timeInForce, stopPrice);

            return !string.IsNullOrWhiteSpace(response)
                ? JsonConvert.DeserializeObject<BlocktradePlaceOrderResponse>(response)
                : new BlocktradePlaceOrderResponse();
        }

        public string PlaceLimitBidRaw(
            BlocktradeApiKey apiKey,
            string baseSymbol,
            string quoteSymbol,
            decimal quantity,
            decimal price)
        {
            return PlaceLimitRaw(apiKey, true, baseSymbol, quoteSymbol, quantity, price);
        }

        public BlocktradePlaceOrderResponse PlaceLimitBid(
            BlocktradeApiKey apiKey,
            string baseSymbol,
            string quoteSymbol,
            decimal quantity,
            decimal price)
        {
            var response = PlaceLimitBidRaw(apiKey, baseSymbol, quoteSymbol, quantity, price);

            return !string.IsNullOrWhiteSpace(response)
                ? JsonConvert.DeserializeObject<BlocktradePlaceOrderResponse>(response)
                : new BlocktradePlaceOrderResponse();
        }

        public string PlaceLimitAskRaw(
            BlocktradeApiKey apiKey,
            string baseSymbol,
            string quoteSymbol,
            decimal quantity,
            decimal price)
        {
            return PlaceLimitRaw(apiKey, false, baseSymbol, quoteSymbol, quantity, price);
        }

        public BlocktradePlaceOrderResponse PlaceLimitAsk(
            BlocktradeApiKey apiKey,
            string baseSymbol,
            string quoteSymbol,
            decimal quantity,
            decimal price)
        {
            var response = PlaceLimitAskRaw(apiKey, baseSymbol, quoteSymbol, quantity, price);

            return !string.IsNullOrWhiteSpace(response)
                ? JsonConvert.DeserializeObject<BlocktradePlaceOrderResponse>(response)
                : new BlocktradePlaceOrderResponse();
        }

        private string PlaceLimitRaw(
            BlocktradeApiKey apiKey,
            bool isBid,
            string baseSymbol,
            string quoteSymbol,
            decimal quantity,
            decimal price)
        {
            const int PortfolioId = 1;
            const string Type = BlocktradeOrderType.Limit;
            const string TimeInForce = BlocktradeTimeInForce.GoodUntilCancelled;
            var direction = isBid ? BlocktradeDirection.Buy : BlocktradeDirection.Sell;

            var tradingAssets = GetTradingAssets();

            var baseAsset = tradingAssets.Single(queryTradingAsset => string.Equals(queryTradingAsset.IsoCode, baseSymbol, StringComparison.InvariantCultureIgnoreCase));
            var quoteAsset = tradingAssets.Single(queryTradingAsset => string.Equals(queryTradingAsset.IsoCode, quoteSymbol, StringComparison.InvariantCultureIgnoreCase));

            var tradingPairs = GetTradingPairs();
            var matchingTradingPair = tradingPairs.Single(queryTradingPair => queryTradingPair.BaseAssetId == baseAsset.Id && queryTradingPair.QuoteAssetId == quoteAsset.Id);

            return PlaceOrderRaw(apiKey, PortfolioId, direction, Type, matchingTradingPair.Id, quantity, price, TimeInForce, null);
        }

        /// <summary>
        /// Generates a 13-digit unix timestamp to be used as the nonce.
        /// </summary>
        private static long GenerateNonce()
        {
            var currentTime = DateTime.UtcNow;
            return (long)(currentTime.Subtract(new DateTime(1970, 1, 1)))
                .TotalMilliseconds;
        }

        private static string Sign(string secretKey, string message)
        {
            var encoding = new UTF8Encoding();
            var keyByte = encoding.GetBytes(secretKey);

            var hmac = new HMACSHA256(keyByte);

            var messageBytes = encoding.GetBytes(message);
            var hashedMessage = hmac.ComputeHash(messageBytes);

            return BinaryUtil.HexEncode(hashedMessage).ToUpper();
        }

        private string AuthenticatedPost(BlocktradeApiKey apiKey, string url, string body = null)
        {
            var nonce = GenerateNonce();
            var message = $"{apiKey.PublicKey}.{nonce}.";
            if (!string.IsNullOrWhiteSpace(body)) { message += body; }
            var signature = Sign(apiKey.PrivateKey, message);

            var request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "POST";
            request.ContentType = "application/json";
            request.Accept = "application/json";
            request.Headers.Add("X-Api-Key", apiKey.PublicKey);
            request.Headers.Add("X-Nonce", nonce.ToString());
            request.Headers.Add("X-Signature", signature);

            if (!string.IsNullOrWhiteSpace(body))
            {
                var encodedBody = Encoding.UTF8.GetBytes(body);

                request.ContentLength = encodedBody.Length;
                using (var requestStream = request.GetRequestStream())
                {
                    requestStream.Write(encodedBody, 0, encodedBody.Length);
                }
            }
            else
            {
                request.ContentLength = 0;
            }

            using (var response = request.GetResponse())
            using (var responseStream = response.GetResponseStream())
            using (var reader = new StreamReader(responseStream))
            {
                return reader.ReadToEnd();
            }
        }

        private string AuthenticatedGet(BlocktradeApiKey apiKey, string url)
        {
            var nonce = GenerateNonce();
            var message = $"{apiKey.PublicKey}.{nonce}.";
            var signature = Sign(apiKey.PrivateKey, message);

            var request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "GET";
            request.ContentLength = 0;
            request.ContentType = "application/json";
            request.Accept = "application/json";
            request.Headers.Add("X-Api-Key", apiKey.PublicKey);
            request.Headers.Add("X-nonce", nonce.ToString());
            request.Headers.Add("X-Signature", signature);

            using (var response = request.GetResponse())
            using (var responseStream = response.GetResponseStream())
            using (var reader = new StreamReader(responseStream))
            {
                return reader.ReadToEnd();
            }
        }
    }
}
