using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace coss_ws_api
{
    public class CossRelayController : ApiController
    {
        [HttpGet]
        [Route("api/ping")]
        public HttpResponseMessage Ping()
        {
            return Request.CreateResponse(HttpStatusCode.OK, "pong");
        }

        [HttpGet]
        [Route("api/test")]
        public HttpResponseMessage Test()
        {
            return Request.CreateResponse(HttpStatusCode.OK, "Testing 123");
        }        
    }
}