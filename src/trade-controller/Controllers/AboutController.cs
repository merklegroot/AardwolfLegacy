using System;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace trade_web.Controllers
{
    public class AboutController : ApiController
    {
        [HttpGet]
        [Route("api/about")]
        public HttpResponseMessage GetAbout()
        {
            var vm = new { Server = Environment.MachineName };
            return Request.CreateResponse(HttpStatusCode.OK, vm);
        }
    }
}
