using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace trade_web.Controllers
{
    public class HitBtcController : BaseController
    {
        [HttpPost]
        [Route("api/get-hitbtc-trading-pairs")]
        public HttpResponseMessage GetHitBtcTradingPairs()
        {
            var fees =_hitBtcIntegration.GetWithdrawlFees();
            var vm = fees.Keys.Select(key => new { Symbol = key, Fee = fees[key] });

            // var tradingPairs = _hitBtcIntegration.GetTradingPairs();
            return Request.CreateResponse(HttpStatusCode.OK, vm);
        }

        // get-hitbtc-deposit-addresses
        [HttpPost]
        [Route("api/get-hitbtc-deposit-addresses")]
        public HttpResponseMessage GetHitBtcDepositAddresses()
        {
            var data = _hitBtcIntegration.GetDepositAddresses();
            return Request.CreateResponse(HttpStatusCode.OK, data);
        }
    }
}
