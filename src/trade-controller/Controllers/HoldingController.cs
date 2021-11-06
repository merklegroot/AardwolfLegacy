using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using trade_lib;
using trade_model;

namespace trade_web.Controllers
{
    public class HoldingController : BaseController
    {
        [HttpPost]
        [Route("api/get-coss-holdings")]
        public HttpResponseMessage GetCossHoldings()
        {
            var info = _cossHoldingRepo.Get();
            return Request.CreateResponse(HttpStatusCode.OK, info);
        }

        [HttpPost]
        [Route("api/get-holdings")]
        public HttpResponseMessage GetHoldings()
        {
            var holdingIntegrations = new List<ITradeIntegration>
            {
                _cossIntegration,
                _binanceIntegration,
                _hitBtcIntegration
            };

            var holdingTasks = holdingIntegrations.Select(integration => Task.Run(() => integration.GetHoldings()));
            var holdings = holdingTasks.Select(task => task.Result).ToList();

            return Request.CreateResponse(HttpStatusCode.OK, holdings);
        }
    }
}
