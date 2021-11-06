using System;
using System.Net.Http;
using System.Web.Http;
using System.Net;
using System.Linq;
using Newtonsoft.Json;
using log_lib;
using client_lib;
using trade_contracts;
using trade_api.Utils;
using cache_lib.Models;
using exchange_client_lib;

namespace trade_api.Controllers
{
    public class OrderBookController : ApiController
    {   
        private readonly IExchangeClient _exchangeClient;

        private readonly ILogRepo _logRepo;

        public OrderBookController(
            IExchangeClient exchangeClient,
            ILogRepo logRepo)
        {
            _exchangeClient = exchangeClient;
            _logRepo = logRepo;
        }

        public class GetOrderBookServiceModel
        {
            public string Symbol { get; set; }
            public string BaseSymbol { get; set; }
            public string Exchange { get; set; }
            public bool ForceRefresh { get; set; }
            public string CachePolicy { get; set; }
        }

        [HttpPost]
        [Route("api/get-order-book")]
        public HttpResponseMessage GetOrderBook(GetOrderBookServiceModel serviceModel)
        {
            try
            {
                if (serviceModel == null) { throw new ArgumentNullException(nameof(serviceModel)); }
                if (string.IsNullOrWhiteSpace(serviceModel.Exchange)) { throw new ArgumentNullException(nameof(serviceModel.Exchange)); }
                if (string.IsNullOrWhiteSpace(serviceModel.Symbol)) { throw new ArgumentNullException(nameof(serviceModel.Symbol)); }
                if (string.IsNullOrWhiteSpace(serviceModel.BaseSymbol)) { throw new ArgumentNullException(nameof(serviceModel.BaseSymbol)); }
                              
                CachePolicy? cacheParseAttempt = null;
                if (!string.IsNullOrWhiteSpace(serviceModel.CachePolicy))
                {
                    cacheParseAttempt = CachePolicyParser.ParseCachePolicy(serviceModel.CachePolicy);
                }

                const CachePolicy DefaultCachePolicy = CachePolicy.AllowCache;
                var cachePolicy = cacheParseAttempt
                    ?? (serviceModel.ForceRefresh ? CachePolicy.ForceRefresh: DefaultCachePolicy);

                var orderBook = _exchangeClient.GetOrderBook(serviceModel.Exchange, serviceModel.Symbol, serviceModel.BaseSymbol, cachePolicy);                
                return Request.CreateResponse(HttpStatusCode.OK, orderBook);
            }
            catch (Exception exception)
            {
                try
                {
                    _logRepo.Error($"OrderBookController.GetOrderBook() failed for exchange Exchange {serviceModel?.Exchange}, Symbol {serviceModel?.Symbol}, Base Symbol: {serviceModel.BaseSymbol}, Force Refresh: {serviceModel.ForceRefresh}, Cache Policy: \"{serviceModel.CachePolicy}\"");
                } catch { }

                _logRepo.Error(exception);
                var json = JsonConvert.SerializeObject(serviceModel, Formatting.Indented);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, $"Failed to get order book for{Environment.NewLine}{json}");
            }
        }

        public class GetCachedOrderBooksServiceModel
        {
            public string Exchange { get; set; }
        }

        [HttpPost]
        [Route("api/get-cached-order-books")]
        public HttpResponseMessage GetCachedOrderBooks(GetCachedOrderBooksServiceModel serviceModel)
        {
            if (serviceModel == null) { throw new ArgumentNullException(nameof(serviceModel)); }
            if (string.IsNullOrWhiteSpace(serviceModel.Exchange)) { throw new ArgumentNullException(serviceModel.Exchange); }

            return Request.CreateResponse(HttpStatusCode.OK, _exchangeClient.GetCachedOrderBooks(serviceModel.Exchange));
        }
    }
}