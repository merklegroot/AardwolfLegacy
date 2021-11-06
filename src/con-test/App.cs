using binance_lib;
using cache_lib;
using config_lib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using test_shared;
using web_util;

namespace con_test
{
    public class App
    {
        private readonly IConfigRepo _configRepo;
        private readonly BinanceIntegration _binanceIntegration;

        public App()
        {
            _configRepo = new ConfigRepo();
            var apiKey = _configRepo.GetBinanceApiKey();
            var connectionString = _configRepo.GetConnectionString();
            var binanceCache = new MongoCache("coin", "binance-web-cache", connectionString);
            var webUtil = new WebUtil();

            _binanceIntegration = new BinanceIntegration(apiKey, binanceCache, webUtil);
        }

        public void Run()
        {
            var serverTime = _binanceIntegration.Client.GetServerTime();
            Console.WriteLine($"ServerTime: {serverTime.ToJson()}");

            CheckAccount();
        }

        private void CheckAccount()
        {
            var accountInfo = _binanceIntegration.GetAccountInfo();
            Console.WriteLine("Account Info:");
            accountInfo.Dump();
        }
    }
}
