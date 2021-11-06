using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace trade_web.Controllers
{
    public class HistoryController : BaseController
    {
        [HttpPost]
        [Route("api/get-history")]
        public HttpResponseMessage GetHistory()
        {
            var history = _binanceIntegration.GetMyTradeHistory();
            return Request.CreateResponse(HttpStatusCode.OK, history);
        }
    }
}
