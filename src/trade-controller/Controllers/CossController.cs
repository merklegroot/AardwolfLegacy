using System.Net;
using System.Net.Http;
using System.Web.Http;
using trade_model;

namespace trade_web.Controllers
{
    public class CossController : BaseController
    {
        [HttpGet]
        [Route("api/get-coss-order-book")]
        public HttpResponseMessage GetCossOrderBook(string symbol, string baseSymbol)
        {
            var orderBook = _cossIntegration.GetOrderBook(new TradingPair(symbol, baseSymbol));
            return Request.CreateResponse(HttpStatusCode.OK, orderBook);
        }
    }
}
