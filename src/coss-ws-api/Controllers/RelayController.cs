using coss_ws_lib;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Cors;

namespace coss_ws_api.ServiceModel
{
    [EnableCors(origins: "*", headers: "*", methods: "*")]
    public class RelayController : ApiController
    {
        private readonly ICossWsWorkflow _cossWsWorkflow;
        public RelayController(ICossWsWorkflow cossWsWorkflow)
        {
            _cossWsWorkflow = cossWsWorkflow;
        }

        [HttpPost]
        [Route("api/message-received")]
        public HttpResponseMessage MessageReceived(MessageReceivedServiceModel serviceModel)
        {
            _cossWsWorkflow.OnMessageReceived(serviceModel.TimeStampUtc, serviceModel.Contract, serviceModel.MessageContents);
            return Request.CreateResponse(HttpStatusCode.OK);
        }
    }
}