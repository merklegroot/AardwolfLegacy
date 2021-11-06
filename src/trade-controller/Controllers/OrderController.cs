using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace trade_web.Controllers
{
    public class OrderController : BaseController
    {
        [HttpGet]
        [Route("api/get-open-orders")]
        public HttpResponseMessage GetMyOpenOrders()
        {
            var openOrders = _openOrderRepo.Get();
            return Request.CreateResponse(HttpStatusCode.OK, openOrders);
        }
    }
}