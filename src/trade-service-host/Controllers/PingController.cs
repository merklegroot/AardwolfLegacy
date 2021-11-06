using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace trade_service_host.Controllers
{
    public class PingController : ApiController
    {
        [HttpGet]
        [HttpPost]
        [Route("api/ping")]
        public HttpResponseMessage Ping()
        {
            if (WebApiApplication.IsRunnerStarted)
            {
                return Request.CreateResponse(HttpStatusCode.OK, "Started.");
            }

            return Request.CreateResponse(HttpStatusCode.OK, "Not started.");
        }
    }
}