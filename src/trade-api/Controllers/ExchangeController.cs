using cache_lib.Models;
using exchange_client_lib;
using res_util_lib;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using trade_api.Utils;
using trade_res;

namespace trade_api.Controllers
{
    public class ExchangeController : ApiController
    {
        private readonly IExchangeClient _exchangeClient;

        public ExchangeController(IExchangeClient exchangeClient)
        {
            _exchangeClient = exchangeClient;
        }

        [HttpGet]
        [HttpPost]
        [Route("api/get-exchanges")]
        public HttpResponseMessage GetExchanges()
        {
            return Request.CreateResponse(HttpStatusCode.OK, _exchangeClient.GetExchanges());
        }

        public class GetTradingPairsForExchangeServiceModel
        {
            public string Exchange { get; set; }
            public bool ForceRefresh { get; set; }
            public string CachePolicy { get; set; }
        }

        [HttpPost]
        [Route("api/get-trading-pairs-for-exchange")]
        public HttpResponseMessage GetTradingPairsForExchange(GetTradingPairsForExchangeServiceModel serviceModel)
        {
            var cachePolicy = CachePolicyParser.ParseCachePolicy(
                    serviceModel?.CachePolicy,
                    serviceModel?.ForceRefresh ?? false,
                    CachePolicy.AllowCache);

            var tradingPairs = _exchangeClient.GetTradingPairs(serviceModel.Exchange, cachePolicy);

            return Request.CreateResponse(HttpStatusCode.OK, tradingPairs);
        }

        // this method is temporary and should only be used by the Exchange client.
        [HttpPost]
        [Route("api/get-cryptocompare-symbols")]
        public HttpResponseMessage GetCryptoCompareSymbols()
        {
            var symbols = ResUtil.Get<List<string>>("cryptocompare-symbols.json", typeof(TradeResDummy).Assembly).Distinct().ToList();
            return Request.CreateResponse(HttpStatusCode.OK, symbols);
        }
    }
}
