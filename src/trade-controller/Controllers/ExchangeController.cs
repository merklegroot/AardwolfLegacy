using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using trade_res;

namespace trade_web.Controllers
{
    public class ExchangeController : BaseController
    {
        [HttpGet]
        [Route("api/exchanges")]
        public HttpResponseMessage GetAssets()
        {
            var items = Exchange.All.OrderBy(item => item.Name).ToList();

            return Request.CreateResponse(HttpStatusCode.OK, items);
        }
    }
}
