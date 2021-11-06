using cache_lib.Models;
using client_lib;
using exchange_client_lib;
using log_lib;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using trade_api.Utils;
using trade_model;

namespace trade_api.Controllers
{
    public class OrderController : ApiController
    {
        private readonly IExchangeClient _exchangeClient;
        private readonly ILogRepo _log;

        public OrderController(
            IExchangeClient exchangeClient,
            ILogRepo log)
        {
            _exchangeClient = exchangeClient;
            _log = log;
        }

        public class GetOpenOrdersServieModel
        {
            public string Exchange { get; set; }
            public bool ForceRefresh { get; set; }
            public string CachePolicy { get; set; }
        }
            
        public class OpenOrderViewModel : OpenOrderForTradingPair
        {
            public decimal? BestBidPrice { get; set; }
            public decimal? BestAskPrice { get; set; }
        }

        [HttpPost]
        [Route("api/get-open-orders")]
        public HttpResponseMessage GetOpenOrders(GetOpenOrdersServieModel serviceModel)
        {
            try
            {
                if (serviceModel == null) { throw new ArgumentNullException(nameof(serviceModel)); }
                if (string.IsNullOrWhiteSpace(serviceModel.Exchange)) { throw new ArgumentNullException(nameof(serviceModel.Exchange)); }

                var cachePolicy = CachePolicyParser.ParseCachePolicy(
                        serviceModel?.CachePolicy,
                        serviceModel?.ForceRefresh ?? false,
                        CachePolicy.OnlyUseCache);

                var openOrders = (_exchangeClient.GetOpenOrders(serviceModel.Exchange, cachePolicy)
                    ?? new List<OpenOrderForTradingPair>())
                    .OrderBy(item => item.Symbol).ToList();

                var vmItems = new List<OpenOrderViewModel>();
                foreach (var openOrder in openOrders)
                {
                    var vmItem = JsonConvert.DeserializeObject<OpenOrderViewModel>(JsonConvert.SerializeObject(openOrder));
                    vmItems.Add(vmItem);
                }

                return Request.CreateResponse(HttpStatusCode.OK, vmItems);
            }
            catch (Exception exception)
            {
                _log.Error(exception);
                throw;
            }
        }

        public class GetOpenOrdersV2ServieModel
        {
            public string Exchange { get; set; }
        }

        [HttpPost]
        [Route("api/get-open-orders-v2")]
        public HttpResponseMessage GetOpenOrdersV2(GetOpenOrdersV2ServieModel serviceModel)
        {
            try
            {
                if (serviceModel == null) { throw new ArgumentNullException(nameof(serviceModel)); }
                if (string.IsNullOrWhiteSpace(serviceModel.Exchange)) { throw new ArgumentNullException(nameof(serviceModel.Exchange)); }

                var results = _exchangeClient.GetOpenOrdersV2(serviceModel.Exchange);

                return Request.CreateResponse(HttpStatusCode.OK, results);
            }
            catch (Exception exception)
            {
                _log.Error(exception);
                throw;
            }
        }


        public class GetOpenOrdersForTradingPairV2ServieModel
        {
            public string Exchange { get; set; }
            public string CachePolicy { get; set; }
            public string Symbol { get; set; }
            public string BaseSymbol { get; set; }
        }

        [HttpPost]
        [Route("api/get-open-orders-for-trading-pair-v2")]
        public HttpResponseMessage GetOpenOrdersForTradingPairV2(GetOpenOrdersForTradingPairV2ServieModel serviceModel)
        {
            try
            {
                if (serviceModel == null) { throw new ArgumentNullException(nameof(serviceModel)); }
                if (string.IsNullOrWhiteSpace(serviceModel.Exchange)) { throw new ArgumentNullException(nameof(serviceModel.Exchange)); }

                var cachePolicy = CachePolicyParser.ParseCachePolicy(serviceModel.CachePolicy, CachePolicy.OnlyUseCacheUnlessEmpty);
                var results = _exchangeClient.GetOpenOrdersForTradingPairV2(serviceModel.Exchange, serviceModel.Symbol, serviceModel.BaseSymbol, cachePolicy);

                return Request.CreateResponse(HttpStatusCode.OK, results);
            }
            catch (Exception exception)
            {
                _log.Error(exception);
                throw;
            }
        }
    }
}