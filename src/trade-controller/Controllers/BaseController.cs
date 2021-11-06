using bit_z_lib;
using coss_lib;
using cryptocompare_lib;
using log_lib;
using System.Web.Http;
using trade_lib;
using web_util;
using config_lib;
using mongo_lib;
using hitbtc_lib;
using cryptopia_lib;
using tidex_integration_library;
using trade_lib.Cache;
using binance_lib;

namespace trade_web.Controllers
{
    public class BaseController : ApiController
    {
        protected readonly CossIntegration _cossIntegration;
        protected readonly BinanceIntegration _binanceIntegration;
        protected readonly BitzIntegration _bitzIntegration;
        protected readonly HitBtcIntegration _hitBtcIntegration;
        protected readonly CryptopiaIntegration _cryptopiaIntegration;
        protected readonly TidexIntegration _tidexIntegration;

        protected readonly ICryptoCompareRepo _cryptoCompareRepo;

        protected readonly OpenOrderRepo _openOrderRepo;
        protected readonly CossHoldingRepo _cossHoldingRepo;

        protected readonly LogRepo _logRepo;
        protected readonly ConfigRepo _configRepo;

        protected readonly IWebUtil _webUtil;

        protected readonly string _connectionString;

        private readonly MongoContext _webCacheContext;

        public BaseController()
        {
            _logRepo = new LogRepo();
            _configRepo = new ConfigRepo();

            var conn = _configRepo.GetConnectionString();
            _connectionString = !string.IsNullOrWhiteSpace(conn) ? conn : new MongoConnectionString();
            _webCacheContext = new MongoContext(_connectionString, "coin", "web-cache");

            _webUtil = new WebUtil();
            var cossCache = new SimpleWebCache(_webUtil, _webCacheContext, "coss");
            _cossIntegration = new CossIntegration(cossCache);

            var hitBtcApiKey = _configRepo.GetHitBtcApiKey();
            var hitBtcCache = new SimpleWebCache(_webUtil, _webCacheContext, "hitbtc");
            _hitBtcIntegration = new HitBtcIntegration(hitBtcApiKey, hitBtcCache);

            var cryptopiaCache = new SimpleWebCache(_webUtil, _webCacheContext, "cryptopia");
            _cryptopiaIntegration = new CryptopiaIntegration(cryptopiaCache);

            var tidexWebCache = new SimpleWebCache(_webUtil, _webCacheContext, "tidex");
            _tidexIntegration = new TidexIntegration(tidexWebCache);

            var binanceApiKey = _configRepo.GetBinanceApiKey();
            var binanceCache = new SimpleWebCache(_webUtil, _webCacheContext, "binance");
            _binanceIntegration = new BinanceIntegration(binanceApiKey, binanceCache, _logRepo);

            var bitzCache = new SimpleWebCache(_webUtil, _webCacheContext, "bit-z");
            _bitzIntegration = new BitzIntegration(bitzCache);

            var cryptoCompareCache = new SimpleWebCache(_webUtil, _webCacheContext, "crypto-compare");
            _cryptoCompareRepo = new CryptoCompareRepo(cryptoCompareCache);
            _cossHoldingRepo = new CossHoldingRepo();

            _openOrderRepo = new OpenOrderRepo();            
        }
    }
}