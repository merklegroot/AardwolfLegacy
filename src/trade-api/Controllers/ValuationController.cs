using cache_lib.Models;
using integration_workflow_lib;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using workflow_client_lib;

namespace trade_api.Controllers
{
    public class ValuationController : ApiController
    {
        private readonly IWorkflowClient _workflowClient;

        public ValuationController(IWorkflowClient workflowClient)
        {
            _workflowClient = workflowClient;
        }

        public class GetValuationDictionaryServiceModel
        {
            public bool ForceRefresh { get; set; }
        }

        [HttpPost]
        [Route("api/get-valuation-dictionary")]
        public HttpResponseMessage GetValuationDictionary(GetValuationDictionaryServiceModel serviceModel)
        {
            var cachePolicy = serviceModel != null && serviceModel.ForceRefresh
                ? CachePolicy.ForceRefresh
                : CachePolicy.AllowCache;

            var valuationDictionary = _workflowClient.GetValuationDictionary(cachePolicy);
            return Request.CreateResponse(HttpStatusCode.OK, valuationDictionary);
        }
    }
}