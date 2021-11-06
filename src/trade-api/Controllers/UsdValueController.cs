using cache_lib.Models;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using trade_api.Utils;
using workflow_client_lib;

namespace trade_api.Controllers
{
    public class UsdValueController : ApiController
    {
        private readonly IWorkflowClient _workflowClient;

        public UsdValueController(IWorkflowClient workflowClient)
        {
            _workflowClient = workflowClient;
        }

        public class GetValueServiceModel
        {
            public string Symbol { get; set; }
            public bool ForceRefresh { get; set; }
        }

        [HttpPost]
        [Route("api/get-usdvalue")]
        public HttpResponseMessage GetUsdValue(GetValueServiceModel serviceModel)
        {
            if (serviceModel == null) { throw new ArgumentNullException(nameof(serviceModel)); }
            if (string.IsNullOrWhiteSpace(serviceModel.Symbol)) { throw new ArgumentNullException(nameof(serviceModel.Symbol)); }

            var cachePolicy = serviceModel.ForceRefresh ? CachePolicy.ForceRefresh : CachePolicy.AllowCache;

            var value = _workflowClient.GetUsdValue(serviceModel.Symbol, cachePolicy);

            return Request.CreateResponse(HttpStatusCode.OK, value);
        }

        public class UsdValueViewModel
        {
            public decimal? UsdValue { get; set; }
            public DateTime? AsOfUtc { get; set; }
        }

        public class GetUsdValueV2ServiceModel
        {
            public string Symbol { get; set; }
            public string CachePolicy { get; set; }
        }

        [HttpPost]
        [Route("api/get-usdvalue-v2")]
        public HttpResponseMessage GetUsdValueV2(GetUsdValueV2ServiceModel serviceModel)
        {
            if (serviceModel == null) { throw new ArgumentNullException(nameof(serviceModel)); }
            if (string.IsNullOrWhiteSpace(serviceModel.Symbol)) { throw new ArgumentNullException(nameof(serviceModel.Symbol)); }

            var cachePolicy = CachePolicyParser.ParseCachePolicy(serviceModel.CachePolicy, false, CachePolicy.AllowCache);

            var (UsdValue, AsOfUtc) = _workflowClient.GetUsdValueV2(serviceModel.Symbol, cachePolicy);
            var vm = new UsdValueViewModel
            {
                UsdValue = UsdValue,
                AsOfUtc = AsOfUtc
            };

            return Request.CreateResponse(HttpStatusCode.OK, vm);
        }

        private Dictionary<string, decimal> YepWereHardcodingThis = new Dictionary<string, decimal>(StringComparer.InvariantCultureIgnoreCase)
        {
            { "USD", 1.0m },
            { "ETH", 756.73m },
            { "BTC", 14156.40m },
            { "LTC", 232.10m },
            { "BCH", 2533.01m },
            { "ZEC", 505.51m },
            { "XRP", 2.30m }
        };

        [HttpPost]
        [Route("api/get-historic-usdvalue-v2")]
        public HttpResponseMessage GetHistoricUsdValueV2(GetUsdValueV2ServiceModel serviceModel)
        {
            if (serviceModel == null) { throw new ArgumentNullException(nameof(serviceModel)); }
            if (string.IsNullOrWhiteSpace(serviceModel.Symbol)) { throw new ArgumentNullException(nameof(serviceModel.Symbol)); }

            var cachePolicy = CachePolicyParser.ParseCachePolicy(serviceModel.CachePolicy, false, CachePolicy.AllowCache);

            if (!YepWereHardcodingThis.ContainsKey(serviceModel.Symbol))
            {
                throw new ApplicationException($"Unexpected symbol \"{serviceModel.Symbol}\"");
            }

            var value = YepWereHardcodingThis[serviceModel.Symbol];

            var vm = new UsdValueViewModel
            {
                UsdValue = value,
                AsOfUtc = DateTime.UtcNow
            };

            return Request.CreateResponse(HttpStatusCode.OK, vm);
        }
    }
}