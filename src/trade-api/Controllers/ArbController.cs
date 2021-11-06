using cache_lib.Models;
using log_lib;
using System;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using trade_api.Utils;
using workflow_client_lib;

namespace trade_api.Controllers
{
    public class ArbController : ApiController
    {
        private readonly IWorkflowClient _workflowClient;
        private readonly ILogRepo _log;

        public ArbController(
            IWorkflowClient workflowClient,
            ILogRepo log)
        {
            _workflowClient = workflowClient;
            _log = log;
        }

        public class GetArbServiceModel
        {
            public string ExchangeA { get; set; }
            public string ExchangeB { get; set; }
            public string Symbol { get; set; }
            public bool ForceRefresh { get; set; }
            public string CachePolicy { get; set; }
        }

        [HttpPost]
        [Route("api/get-arb")]
        public HttpResponseMessage GetArb(GetArbServiceModel serviceModel)
        {
            try
            {
                if (serviceModel == null) { throw new ArgumentNullException(nameof(serviceModel)); }
                if (string.IsNullOrWhiteSpace(serviceModel.ExchangeA)) { throw new ArgumentNullException(nameof(serviceModel.ExchangeA)); }
                if (string.IsNullOrWhiteSpace(serviceModel.ExchangeB)) { throw new ArgumentNullException(nameof(serviceModel.ExchangeB)); }
                if (string.IsNullOrWhiteSpace(serviceModel.Symbol)) { throw new ArgumentNullException(nameof(serviceModel.Symbol)); }

                var cachePolicy = CachePolicyParser.ParseCachePolicy(serviceModel.CachePolicy, serviceModel.ForceRefresh, CachePolicy.OnlyUseCacheUnlessEmpty);
                var result = _workflowClient.GetArb(serviceModel.ExchangeA, serviceModel.ExchangeB, serviceModel.Symbol, cachePolicy);

                return Request.CreateResponse(HttpStatusCode.OK, result);
            }
            catch(Exception exception)
            {
                _log.Error($"GetArb() Failed for ExchangeA: {serviceModel?.ExchangeA}, ExchangeB: {serviceModel?.ExchangeB}, Symbol: {serviceModel?.Symbol}, ForceRefresh: {serviceModel?.ForceRefresh}, CachePolicy: {serviceModel?.CachePolicy}");
                _log.Error(exception);
                throw;
            }
}
    }
}