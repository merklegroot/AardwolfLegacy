using balance_lib;
using cache_lib.Models;
using client_lib;
using exchange_client_lib;
using System;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using trade_api.Utils;

namespace trade_api.Controllers
{
    public class BalanceController : ApiController
    {
        private readonly IExchangeClient _exchangeClient;
        public BalanceController(IExchangeClient exchangeClient)
        {
            _exchangeClient = exchangeClient;
        }

        public class GetBalanceForExchangeServiceModel
        {
            public string Exchange { get; set; }
            public string CachePolicy { get; set; }
        }

        [HttpPost]
        [Route("api/get-balance-for-exchange")]
        public HttpResponseMessage GetBalanceForExchange(GetBalanceForExchangeServiceModel serviceModel)
        {
            if (serviceModel == null) { throw new ArgumentNullException(nameof(serviceModel)); }

            var cachePolicy = CachePolicyParser.ParseCachePolicy(serviceModel.CachePolicy, false, CachePolicy.OnlyUseCache);
            var result = _exchangeClient.GetBalances(serviceModel.Exchange, cachePolicy);

            return Request.CreateResponse(HttpStatusCode.OK, result);
        }

        [HttpPost]
        [Route("api/get-balance-for-commodity-and-exchange")]
        public HttpResponseMessage GetBalanceForCommodityAndExchange(GetHoldingForCommodityAndExchangeServiceModel serviceModel)
        {
            var cachePolicy = serviceModel.ForceRefresh ? CachePolicy.ForceRefresh : CachePolicy.OnlyUseCacheUnlessEmpty;
            var holding = _exchangeClient.GetBalance(serviceModel.Exchange, serviceModel.Symbol, cachePolicy);

            return Request.CreateResponse(HttpStatusCode.OK, holding);
        }
    }
}