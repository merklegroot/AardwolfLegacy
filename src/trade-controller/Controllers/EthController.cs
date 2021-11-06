using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace trade_web.Controllers
{
    public class EthController : BaseController
    {
        [HttpGet]
        [Route("api/eth-wallet")]
        public HttpResponseMessage GetWalletAddress()
        {
            var walletAddress = _configRepo.GetEthAddress();
            return Request.CreateResponse(HttpStatusCode.OK, walletAddress);
        }

        [HttpGet]
        [Route("api/eth-to-btc-ratio")]
        public HttpResponseMessage GetEthToBtcRatio()
        {
            var ratio = _cryptoCompareRepo.GetEthToBtcRatio();
            return Request.CreateResponse(HttpStatusCode.OK, ratio);
        }
    }
}
