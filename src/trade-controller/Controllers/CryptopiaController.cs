using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace trade_web.Controllers
{
    public class CryptopiaController : BaseController
    {
        [HttpGet]
        [HttpPost]
        [Route("api/get-cryptopia-withdrawl-fees")]
        public HttpResponseMessage GetCryptopiaWithdrawlFees()
        {
            var fees = _cryptopiaIntegration.GetWithdrawlFees();
            var vm = new List<dynamic>();

            foreach (var key in fees.Keys)
            {
                var fee = fees[key];
                vm.Add(new { Symbol = key, Fee = fee });
            }

            return Request.CreateResponse(HttpStatusCode.OK, vm);
        }
    }
}
