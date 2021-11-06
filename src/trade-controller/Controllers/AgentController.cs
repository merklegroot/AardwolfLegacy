using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace trade_web.Controllers
{
    public class AgentController : BaseController
    {
        [HttpGet]
        [Route("api/agent-status")]
        public HttpResponseMessage GetAgentStatus()
        {
            var vm = new { Info = "Not Implemented." };
            return Request.CreateResponse(HttpStatusCode.OK, vm);
        }
    }
}
