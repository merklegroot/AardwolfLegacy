using cryptocompare_lib;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using config_client_lib;

namespace trade_api.Controllers
{
    public class EthController : ApiController
    {
        private readonly IConfigClient _configClient;
        private readonly ICryptoCompareIntegration _cryptoCompareRepo;

        public EthController(
            IConfigClient configClient,
            ICryptoCompareIntegration cryptoCompareRepo)
        {
            _cryptoCompareRepo = cryptoCompareRepo;
            _configClient = configClient;
        }

        [HttpGet]
        [HttpPost]
        [Route("api/eth-wallet")]
        public HttpResponseMessage GetWalletAddress()
        {
            var walletAddress = _configClient.GetMewWalletAddress();
            return Request.CreateResponse(HttpStatusCode.OK, walletAddress);
        }
    }
}
