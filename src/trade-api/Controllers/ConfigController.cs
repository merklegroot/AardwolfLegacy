using config_model;
using Newtonsoft.Json;
using System;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using trade_model;
using config_client_lib;
using trade_res;
using config_lib;
using trade_constants;
using trade_model.ArbConfig;

namespace trade_api.Controllers
{
    public class ConfigController : ApiController
    {
        private readonly IConfigClient _configClient;
        private readonly IConfigRepo _configRepo;

        public ConfigController(
            IConfigClient configClient,
            IConfigRepo configRepo
            )
        {
            _configClient = configClient;
            _configRepo = configRepo;
        }

        [HttpGet]
        [Route("api/get-mew-wallet-address")]
        public HttpResponseMessage GetMewWalletAddress()
        {
            var address = _configClient.GetMewWalletAddress();
            return Request.CreateResponse(HttpStatusCode.OK, address);
        }

        public class UpdateConfigServiceModel
        {
            public string MyEtherWalletAddress { get; set; }
        }

        [HttpPost]
        [Route("api/set-mew-wallet-address")]
        public HttpResponseMessage SetMewWalletAddress(UpdateConfigServiceModel serviceModel)
        {
            _configClient.SetMewWalletAddress(serviceModel.MyEtherWalletAddress);
            return GetMewWalletAddress();
        }

        [HttpPost]
        [Route("api/get-connection-string")]
        public HttpResponseMessage GetConnectionString()
        {
            var vm = new { ConnectionString = _configClient.GetConnectionString() };
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

            _configClient.SetConnectionString(serviceModel.ConnectionString);

            return GetConnectionString();
        }

        [HttpPost]
        [Route("api/get-coinbase-api-key")]
        public HttpResponseMessage GetCoinbaseApiKey()
        {
            var vm = new { ApiKey = _configClient.GetApiKey(IntegrationNameRes.Coinbase) };
            return Request.CreateResponse(HttpStatusCode.OK, vm);
        }

        [HttpPost]
        [Route("api/get-binance-api-key")]
        public HttpResponseMessage GetBinanceApiKey()
        {
            var vm = new { ApiKey = _configClient.GetBinanceApiKey() };
            return Request.CreateResponse(HttpStatusCode.OK, vm);
        }

        [HttpPost]
        [Route("api/get-hit-btc-api-key")]
        public HttpResponseMessage GetHitBtcApikey()
        {
            var vm = new { ApiKey = _configClient.GetHitBtcApiKey() };
            return Request.CreateResponse(HttpStatusCode.OK, vm);
        }

        [HttpPost]
        [Route("api/get-infura-api-key")]
        public HttpResponseMessage GetInfuraApiKey()
        {
            var vm = new { ApiKey = _configClient.GetApiKey(IntegrationNameRes.Infura) };
            return Request.CreateResponse(HttpStatusCode.OK, vm);
        }

        [Route("api/set-infura-api-key")]
        public HttpResponseMessage SetInfuraApikey(SetApiKeyServiceModel serviceModel)
        {
            if (serviceModel == null || serviceModel.ApiKey == null) { throw new ArgumentException(); }
            _configClient.SetApiKey(IntegrationNameRes.Infura, serviceModel.ApiKey?.Key, serviceModel.ApiKey?.Secret);

            return GetInfuraApiKey();
        }

        [HttpPost]
        [Route("api/get-cryptopia-api-key")]
        public HttpResponseMessage GetCryptopiaApikey()
        {
            var vm = new { ApiKey = _configClient.GetCryptopiaApiKey() };
            return Request.CreateResponse(HttpStatusCode.OK, vm);
        }

        [HttpPost]
        [Route("api/get-qryptos-api-key")]
        public HttpResponseMessage GetQryptosApikey()
        {
            var vm = new { ApiKey = _configClient.GetQryptosApiKey() };
            return Request.CreateResponse(HttpStatusCode.OK, vm);
        }

        public class SetApiKeyServiceModel
        {
            public string Exchange { get; set; }
            public ApiKey ApiKey { get; set; }
        }

        [HttpPost]
        [Route("api/set-binance-api-key")]
        public HttpResponseMessage SetBinanceApikey(SetApiKeyServiceModel serviceModel)
        {
            if (serviceModel == null || serviceModel.ApiKey == null) { throw new ArgumentException(); }
            _configClient.SetApiKey(IntegrationNameRes.Binance, serviceModel.ApiKey?.Key, serviceModel.ApiKey?.Secret);

            return GetBinanceApiKey();
        }

        [HttpPost]
        [Route("api/set-coinbase-api-key")]
        public HttpResponseMessage SetCoinbaseApiKey(SetApiKeyServiceModel serviceModel)
        {
            if (serviceModel == null || serviceModel.ApiKey == null) { throw new ArgumentException(); }
            _configClient.SetApiKey(IntegrationNameRes.Coinbase, serviceModel.ApiKey?.Key, serviceModel.ApiKey?.Secret);

            return GetCoinbaseApiKey();
        }

        [HttpPost]
        [Route("api/set-blocktrade-api-key")]
        public HttpResponseMessage SetBlocktradeApiKey(SetApiKeyServiceModel serviceModel)
        {
            if (serviceModel == null || serviceModel.ApiKey == null) { throw new ArgumentException(); }
            _configClient.SetApiKey(IntegrationNameRes.Blocktrade, serviceModel.ApiKey?.Key, serviceModel.ApiKey?.Secret);

            return GetCoinbaseApiKey();
        }

        [HttpPost]
        [Route("api/set-hit-btc-api-key")]
        public HttpResponseMessage SetHitBtcApikey(SetApiKeyServiceModel serviceModel)
        {
            if (serviceModel == null || serviceModel.ApiKey == null) { throw new ArgumentException(); }
            _configClient.SetApiKey(IntegrationNameRes.HitBtc, serviceModel.ApiKey?.Key, serviceModel.ApiKey?.Secret);

            return GetHitBtcApikey();
        }

        [HttpPost]
        [Route("api/set-cryptopia-api-key")]
        public HttpResponseMessage SetCryptopiaApikey(SetApiKeyServiceModel serviceModel)
        {
            if (serviceModel == null || serviceModel.ApiKey == null) { throw new ArgumentException(); }
            _configClient.SetApiKey(IntegrationNameRes.Cryptopia, serviceModel.ApiKey?.Key, serviceModel.ApiKey?.Secret);

            return GetCryptopiaApikey();
        }

        [HttpPost]
        [Route("api/set-qryptos-api-key")]
        public HttpResponseMessage SetQryptosApikey(SetApiKeyServiceModel serviceModel)
        {
            if (serviceModel == null || serviceModel.ApiKey == null) { throw new ArgumentException(); }
            _configClient.SetApiKey(IntegrationNameRes.Qryptos, serviceModel.ApiKey?.Key, serviceModel.ApiKey?.Secret);

            return GetQryptosApikey();
        }

        [HttpPost]
        [Route("api/get-kraken-api-key")]
        public HttpResponseMessage GetKrakenApikey()
        {
            var vm = new { ApiKey = _configClient.GetKrakenApiKey() };
            return Request.CreateResponse(HttpStatusCode.OK, vm);
        }

        [HttpPost]
        [Route("api/set-kraken-api-key")]
        public HttpResponseMessage SetKrakenApikey(SetApiKeyServiceModel serviceModel)
        {
            if (serviceModel == null || serviceModel.ApiKey == null) { throw new ArgumentException(); }
            _configClient.SetApiKey(IntegrationNameRes.Kraken, serviceModel.ApiKey?.Key, serviceModel.ApiKey?.Secret);

            return GetKrakenApikey();
        }

        [HttpPost]
        [Route("api/get-etherscan-api-key")]
        public HttpResponseMessage GetEtherscanApiKey()
        {
            var vm = new { ApiKey = _configClient.GetEtherscanApiKey() };
            return Request.CreateResponse(HttpStatusCode.OK, vm);
        }

        [HttpPost]
        [Route("api/set-etherscan-api-key")]
        public HttpResponseMessage SetEtherscanApiKey(SetApiKeyServiceModel serviceModel)
        {
            if (serviceModel == null || serviceModel.ApiKey == null) { throw new ArgumentException(); }
            _configClient.SetApiKey(IntegrationNameRes.Etherscan, serviceModel.ApiKey?.Key, serviceModel.ApiKey?.Secret);

            return GetEtherscanApiKey();
        }

        [HttpPost]
        [Route("api/get-twitter-api-key")]
        public HttpResponseMessage GetTwitterApiKey()
        {
            var vm = new { ApiKey = _configClient.GetTwitterApiKey() };
            return Request.CreateResponse(HttpStatusCode.OK, vm);
        }

        [HttpPost]
        [Route("api/set-twitter-api-key")]
        public HttpResponseMessage SetTwitterApiKey(SetApiKeyServiceModel serviceModel)
        {
            if (serviceModel == null || serviceModel.ApiKey == null) { throw new ArgumentException(); }
            _configClient.SetApiKey(IntegrationNameRes.Twitter, serviceModel.ApiKey?.Key, serviceModel.ApiKey?.Secret);

            return GetTwitterApiKey();
        }

        [HttpPost]
        [Route("api/get-coss-credentials")]
        public HttpResponseMessage GetCossCredentials()
        {            
            var vm = _configClient.GetCossCredentials();
            return Request.CreateResponse(HttpStatusCode.OK, vm);
        }

        [HttpPost]
        [Route("api/set-coss-credentials")]
        public HttpResponseMessage SetCossCredentials(UsernameAndPassword serviceModel)
        {
            _configRepo.SetCossCredentials(serviceModel);
            return GetCossCredentials();
        }

        [HttpPost]
        [Route("api/get-coss-email-credentials")]
        public HttpResponseMessage GetCossEmailCredentials()
        {
            var vm = _configRepo.GetCossEmailCredentials();
            return Request.CreateResponse(HttpStatusCode.OK, vm);
        }

        [HttpPost]
        [Route("api/set-coss-email-credentials")]
        public HttpResponseMessage SetCossEmailCredentials(UsernameAndPassword serviceModel)
        {
            _configRepo.SetCossEmailCredentials(serviceModel);
            return GetCossEmailCredentials();
        }

        [HttpPost]
        [Route("api/get-bitz-login-credentials")]
        public HttpResponseMessage GetBitzLoginCredentials()
        {
            var vm = _configRepo.GetBitzLoginCredentials();
            return Request.CreateResponse(HttpStatusCode.OK, vm);
        }

        [HttpPost]
        [Route("api/set-bitz-login-credentials")]
        public HttpResponseMessage SetBitzLoginCredentials(UsernameAndPassword serviceModel)
        {
            _configRepo.SetBitzLoginCredentials(serviceModel);
            return GetBitzLoginCredentials();
        }

        [HttpPost]
        [Route("api/get-kucoin-email-credentials")]
        public HttpResponseMessage GetKucoinEmailCredentials()
        {
            var vm = _configRepo.GetKucoinEmailCredentials();
            return Request.CreateResponse(HttpStatusCode.OK, vm);
        }

        [HttpPost]
        [Route("api/set-kucoin-email-credentials")]
        public HttpResponseMessage SetKucoinEmailCredentials(UsernameAndPassword serviceModel)
        {
            _configRepo.SetKucoinEmailCredentials(serviceModel);
            return GetCossEmailCredentials();
        }

        [HttpPost]
        [Route("api/set-livecoin-api-key")]
        public HttpResponseMessage SetLivecoinApikey(SetApiKeyServiceModel serviceModel)
        {
            if (serviceModel == null || serviceModel.ApiKey == null) { throw new ArgumentException(); }
            _configClient.SetApiKey(IntegrationNameRes.Livecoin, serviceModel.ApiKey?.Key, serviceModel.ApiKey?.Secret);

            return GetLivecoinApikey();
        }

        [HttpPost]
        [Route("api/get-livecoin-api-key")]
        public HttpResponseMessage GetLivecoinApikey()
        {
            var vm = new { ApiKey = _configClient.GetLivecoinApiKey() };
            return Request.CreateResponse(HttpStatusCode.OK, vm);
        }

        [Route("api/set-kucoin-api-key")]
        public HttpResponseMessage SetKucoinApikey(SetApiKeyServiceModel serviceModel)
        {
            if (serviceModel == null || serviceModel.ApiKey == null) { throw new ArgumentException(); }
            _configClient.SetApiKey(IntegrationNameRes.KuCoin, serviceModel.ApiKey?.Key, serviceModel.ApiKey?.Secret);

            return GetKucoinApiKey();
        }

        [HttpPost]
        [Route("api/get-kucoin-api-key")]
        public HttpResponseMessage GetKucoinApiKey()
        {
            var vm = new { ApiKey = _configClient.GetKucoinApiKey() };
            return Request.CreateResponse(HttpStatusCode.OK, vm);
        }

        [Route("api/set-bitz-api-key")]
        public HttpResponseMessage SetBitzApikey(SetApiKeyServiceModel serviceModel)
        {
            if (serviceModel == null || serviceModel.ApiKey == null) { throw new ArgumentException(); }
            _configClient.SetApiKey(IntegrationNameRes.Bitz, serviceModel.ApiKey?.Key, serviceModel.ApiKey?.Secret);

            return GetBitzApiKey();
        }

        [HttpPost]
        [Route("api/get-bitz-api-key")]
        public HttpResponseMessage GetBitzApiKey()
        {
            var vm = new { ApiKey = _configClient.GetBitzApiKey() };
            return Request.CreateResponse(HttpStatusCode.OK, vm);
        }

        [HttpPost]
        [Route("api/get-coss-api-key")]
        public HttpResponseMessage GetCossApiKey()
        {
            var vm = new { ApiKey = _configClient.GetApiKey(IntegrationNameRes.Coss) };
            return Request.CreateResponse(HttpStatusCode.OK, vm);
        }

        [HttpPost]
        [Route("api/get-blocktrade-api-key")]
        public HttpResponseMessage GetBlocktradeApiKey()
        {
            var vm = new { ApiKey = _configClient.GetApiKey(IntegrationNameRes.Blocktrade) };
            return Request.CreateResponse(HttpStatusCode.OK, vm);
        }

        [Route("api/set-coss-api-key")]
        public HttpResponseMessage SetCossApikey(SetApiKeyServiceModel serviceModel)
        {
            if (serviceModel == null || serviceModel.ApiKey == null) { throw new ArgumentException(); }
            _configClient.SetApiKey(IntegrationNameRes.Coss, serviceModel.ApiKey?.Key, serviceModel.ApiKey?.Secret);

            return GetCossApiKey();
        }

        public class GetApiKeyServicelModel
        {
            public string Exchange { get; set; }
        }

        [HttpPost]
        [Route("api/get-api-key")]
        public HttpResponseMessage GetApiKey(GetApiKeyServicelModel serviceModel)
        {
            if (serviceModel == null) { throw new ArgumentNullException(nameof(serviceModel)); }
            if (string.IsNullOrWhiteSpace(serviceModel.Exchange)) { throw new ArgumentNullException(nameof(serviceModel.Exchange)); }

            var exchange = serviceModel.Exchange.Trim();

            var vm = new { ApiKey = _configClient.GetApiKey(exchange) };
            return Request.CreateResponse(HttpStatusCode.OK, vm);
        }

        [Route("api/set-api-key")]
        public HttpResponseMessage SetApikey(SetApiKeyServiceModel serviceModel)
        {
            if (serviceModel == null) { throw new ArgumentNullException(nameof(serviceModel)); }
            if (string.IsNullOrWhiteSpace(serviceModel.Exchange)) { throw new ArgumentNullException(nameof(serviceModel.Exchange)); }

            var exchange = serviceModel.Exchange.Trim();

            _configClient.SetApiKey(exchange, serviceModel.ApiKey?.Key, serviceModel.ApiKey?.Secret);

            return GetApiKey(new GetApiKeyServicelModel { Exchange = serviceModel.Exchange });
        }

        [HttpPost]
        [Route("api/get-bitz-trade-password")]
        public HttpResponseMessage GetBitzTradePassword()
        {
            var vm = _configClient.GetBitzTradePassword();
            return Request.CreateResponse(HttpStatusCode.OK, vm);
        }

        public class SetPasswordServiceModel
        {
            public string Password { get; set; }
        }

        [HttpPost]
        [Route("api/set-bitz-trade-password")]
        public HttpResponseMessage SetBitzTradePassword(SetPasswordServiceModel serviceModel)
        {
            if (serviceModel == null) { throw new ArgumentNullException(nameof(serviceModel)); }
            _configClient.SetBitzTradePassword(serviceModel.Password);

            return GetBitzTradePassword();
        }

        [HttpPost]
        [Route("api/get-kucoin-trade-password")]
        public HttpResponseMessage GetKucoinTradePassword()
        {
            var vm = _configClient.GetKucoinTradePassword();
            return Request.CreateResponse(HttpStatusCode.OK, vm);
        }


        [HttpPost]
        [Route("api/get-kucoin-api-passphrase")]
        public HttpResponseMessage GetKucoinApiPassphrase()
        {
            var vm = _configClient.GetKucoinApiPassphrase();
            return Request.CreateResponse(HttpStatusCode.OK, vm);
        }

        [HttpPost]
        [Route("api/set-kucoin-trade-password")]
        public HttpResponseMessage SetKucoinTradePassword(SetPasswordServiceModel serviceModel)
        {
            if (serviceModel == null) { throw new ArgumentNullException(nameof(serviceModel)); }
            _configClient.SetKucoinTradePassword(serviceModel.Password);

            return GetKucoinTradePassword();
        }

        [HttpPost]
        [Route("api/set-kucoin-api-passphrase")]
        public HttpResponseMessage SetKucoinApiPassphrase(SetPasswordServiceModel serviceModel)
        {
            if (serviceModel == null) { throw new ArgumentNullException(nameof(serviceModel)); }
            _configClient.SetKucoinApiPassphrase(serviceModel.Password);

            return GetKucoinApiPassphrase();
        }

        [HttpPost]
        [Route("api/get-mew-password")]
        public HttpResponseMessage GetMewPassword()
        {
            var vm = _configClient.GetMewPassword();
            return Request.CreateResponse(HttpStatusCode.OK, vm);
        }

        [HttpPost]
        [Route("api/set-mew-password")]
        public HttpResponseMessage SetMewPassword(SetPasswordServiceModel serviceModel)
        {
            if (serviceModel == null) { throw new ArgumentNullException(nameof(serviceModel)); }
            _configClient.SetMewPassword(serviceModel.Password);
            return GetMewPassword();
        }

        [HttpPost]
        [Route("api/get-mew-wallet-filename")]
        public HttpResponseMessage GetMewWalletFileName()
        {
            var walletFileName = _configClient.GetMewWalletFileName();
            return Request.CreateResponse(HttpStatusCode.OK, walletFileName);
        }

        [HttpPost]
        [Route("api/set-mew-wallet-filename")]
        public HttpResponseMessage SetMewWalletFileName(SetFileNameServiceModel serviceModel)
        {
            if (serviceModel == null) { throw new ArgumentNullException(nameof(serviceModel)); }
            _configClient.SetMewWalletFileName(serviceModel.FileName);
            return GetMewWalletFileName();
        }

        [HttpPost]
        [Route("api/get-coss-agent-config")]
        public HttpResponseMessage GetCossAgentConfig()
        {
            var config = _configClient.GetCossAgentConfig()
                ?? new CossAgentConfig();
            return Request.CreateResponse(HttpStatusCode.OK, config);
        }

        public class SetCossAgentConfigServiceModel
        {
            public bool IsCossAutoTradingEnabled { get; set; }
            public decimal EthThreshold { get; set; }
            public decimal TokenThreshold { get; set; }
        }

        [HttpPost]
        [Route("api/set-coss-agent-config")]
        public HttpResponseMessage SetCossAgentConfig(SetCossAgentConfigServiceModel serviceModel)
        {
            if (serviceModel == null) { throw new ArgumentNullException(nameof(serviceModel)); }

            var config = _configClient.GetCossAgentConfig() ?? new CossAgentConfig();
            config.IsCossAutoTradingEnabled = serviceModel.IsCossAutoTradingEnabled;
            config.EthThreshold = serviceModel.EthThreshold;
            config.TokenThreshold = serviceModel.TokenThreshold;

            _configClient.SetCossAgentConfig(config);

            return GetCossAgentConfig();
        }

        [HttpPost]
        [Route("api/get-bitz-agent-config")]
        public HttpResponseMessage GetBitzAgentConfig()
        {
            var config = _configRepo.GetBitzAgentConfig()
                ?? new AgentConfig();
            return Request.CreateResponse(HttpStatusCode.OK, config);
        }

        public class SetBitzAgentConfigServiceModel
        {
            public bool IsAutoTradingEnabled { get; set; }
            public decimal EthThreshold { get; set; }
            public decimal TokenThreshold { get; set; }
        }

        [HttpPost]
        [Route("api/set-bitz-agent-config")]
        public HttpResponseMessage SetBitzAgentConfig(SetBitzAgentConfigServiceModel serviceModel)
        {
            if (serviceModel == null) { throw new ArgumentNullException(nameof(serviceModel)); }

            var config = _configRepo.GetBitzAgentConfig() ?? new AgentConfig();
            config.IsAutoTradingEnabled = serviceModel.IsAutoTradingEnabled;
            config.EthThreshold = serviceModel.EthThreshold;
            config.TokenThreshold = serviceModel.TokenThreshold;

            _configRepo.SetBitzAgentConfig(config);

            return GetBitzAgentConfig();
        }
        
        [HttpPost]
        [Route("api/get-ccxt-url")]
        public HttpResponseMessage GetCcxtUrl()
        {
            var result = _configClient.GetCcxtUrl();
            return Request.CreateResponse(HttpStatusCode.OK, result);
        }

        public class SetCcxtUrlServiceModel
        {
            [JsonProperty("url")]
            public string Url { get; set; }
        }

        [HttpPost]
        [Route("api/set-ccxt-url")]
        public HttpResponseMessage SetCcxtUrl(SetCcxtUrlServiceModel serviceModel)
        {
            if (serviceModel == null) { throw new ArgumentException(nameof(serviceModel)); }
            if (string.IsNullOrWhiteSpace(serviceModel.Url)) { throw new ArgumentNullException(nameof(serviceModel.Url)); }

            _configClient.SetCcxtUrl(serviceModel.Url.Trim());

            return GetCcxtUrl();
        }

        [HttpPost]
        [Route("api/get-binance-arb-config")]
        public HttpResponseMessage GetBinanceArbConfig()
        {
            var result = _configClient.GetBinanceArbConfig();
            return Request.CreateResponse(HttpStatusCode.OK, result);
        }

        public class BinanceArbConfigServiceModel
        {
            public bool IsEnabled { get; set; }
            public string ArkSaleTarget { get; set; }
            public string TusdSaleTarget { get; set; }
            public string EthSaleTarget { get; set; }
            public string LtcSaleTarget { get; set; }
            public string WavesSaleTarget { get; set; }
            public string NeoSaleTarget { get; set; }
            public string BtcSaleTarget { get; set; }
        }

        [HttpPost]
        [Route("api/set-binance-arb-config")]
        public HttpResponseMessage SetBinanceArbConfig(BinanceArbConfigServiceModel serviceModel)
        {
            if (serviceModel == null) { throw new ArgumentNullException(nameof(serviceModel)); }

            var config = new BinanceArbConfig
            {
                IsEnabled = serviceModel.IsEnabled,
                ArkSaleTarget = serviceModel.ArkSaleTarget,
                TusdSaleTarget = serviceModel.TusdSaleTarget,
                EthSaleTarget = serviceModel.EthSaleTarget,
                LtcSaleTarget = serviceModel.LtcSaleTarget,
                WavesSaleTarget = serviceModel.WavesSaleTarget,
                NeoSaleTarget = serviceModel.NeoSaleTarget,
                BtcSaleTarget = serviceModel.BtcSaleTarget
            };

            _configClient.SetBinanceArbConfig(config);

            return GetBinanceArbConfig();
        }

        public class SetFileNameServiceModel
        {
            public string FileName { get; set; }
        }
    }
}
