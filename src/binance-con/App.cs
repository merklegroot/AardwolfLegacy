using binance_lib;
using config_lib;
using log_lib;
using mongo_lib;
using Newtonsoft.Json;
using System;
using trade_lib.Cache;
using trade_model;
using trade_node_integration;
using web_util;

namespace binance_con
{
    public class App
    {
        private readonly ILogRepo _log;
        private readonly BinanceIntegration _integration;
        
        public App()
        {
            var webUtil = new WebUtil();
            _log = new LogRepo();

            var databaseContext = new MongoDatabaseContext(new MongoConnectionString(), "integration-tests");
            var webCacheContext = new MongoCollectionContext(databaseContext, "web-cache");
            var binanceCache = new SimpleWebCache(webUtil, webCacheContext, "binance");
            var configRepo = new ConfigRepo();
            var nodeUtil = new TradeNodeUtil(configRepo, webUtil, _log);
            _integration = new BinanceIntegration(webUtil, configRepo, configRepo, nodeUtil, _log);
        }

        public void Run()
        {
            var result = _integration.GetOrderBook(new TradingPair("EOS", "ETH"), CachePolicy.ForceRefresh);
            var contents = JsonConvert.SerializeObject(result);
            Console.WriteLine(contents);
        }
    }
}
