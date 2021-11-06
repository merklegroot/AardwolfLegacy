using System;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using trade_model;

namespace trade_web.Controllers
{
    public class ConfigController : BaseController
    {
        [HttpGet]
        [Route("api/config")]
        public HttpResponseMessage GetConfiguration()
        {
            var ethAddress = _configRepo.GetEthAddress();

            var vm = new { MyEtherWalletAddress = ethAddress };
            return Request.CreateResponse(HttpStatusCode.OK, vm);
        }

        public class UpdateConfigServiceModel
        {
            public string MyEtherWalletAddress { get; set; }
        }

        [HttpPost]
        [Route("api/set-my-ether-wallet-address")]
        public HttpResponseMessage SetMyEtherWalletAddress(UpdateConfigServiceModel serviceModel)
        {
            _configRepo.SetEthAddress(serviceModel.MyEtherWalletAddress);

            return GetConfiguration();
        }

        [HttpPost]
        [Route("api/get-connection-string")]
        public HttpResponseMessage GetConnectionString()
        {
            var connectionString = _configRepo.GetConnectionString();

            var vm = new { ConnectionString = connectionString };
            return Request.CreateResponse(HttpStatusCode.OK, vm);
        }

        public class SetConnectionStringServiceModel
        {
            public string ConnectionString { get; set; }
        }

        [HttpPost]
        [Route("api/set-connection-string")]
        public HttpResponseMessage SetConnectionString(SetConnectionStringServiceModel serviceModel)
        {
            if (serviceModel == null) { throw new ArgumentNullException(nameof(serviceModel)); }
            if (string.IsNullOrWhiteSpace(serviceModel.ConnectionString)) { throw new ArgumentNullException($"{nameof(serviceModel)}.{nameof(serviceModel.ConnectionString)}"); }

            _configRepo.SetConnectionString(serviceModel.ConnectionString);

            return GetConnectionString();
        }

        [HttpPost]
        [Route("api/get-binance-api-key")]
        public HttpResponseMessage GetBinanceApikey()
        {
            var vm = new { ApiKey = _configRepo.GetBinanceApiKey() };
            return Request.CreateResponse(HttpStatusCode.OK, vm);
        }

        [HttpPost]
        [Route("api/get-hit-btc-api-key")]
        public HttpResponseMessage GetHitBtcApikey()
        {
            var vm = new { ApiKey = _configRepo.GetHitBtcApiKey() };
            return Request.CreateResponse(HttpStatusCode.OK, vm);
        }

        public class SetApiKeyServiceModel { public ApiKey ApiKey { get; set; } }

        [HttpPost]
        [Route("api/set-binance-api-key")]
        public HttpResponseMessage SetBinanceApikey(SetApiKeyServiceModel serviceModel)
        {
            if (serviceModel == null || serviceModel.ApiKey == null) { throw new ArgumentException(); }
            _configRepo.SetBinanceApiKey(serviceModel.ApiKey);

            return GetBinanceApikey();
        }

        [HttpPost]
        [Route("api/set-hit-btc-api-key")]
        public HttpResponseMessage SetHitBtcApikey(SetApiKeyServiceModel serviceModel)
        {
            if (serviceModel == null || serviceModel.ApiKey == null) { throw new ArgumentException(); }
            _configRepo.SetHitBtcApiKey(serviceModel.ApiKey);
            _hitBtcIntegration.SetApiKey(serviceModel.ApiKey);

            return GetHitBtcApikey();
        }
    }
}
