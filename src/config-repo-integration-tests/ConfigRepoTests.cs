using System;
using System.Collections.Generic;
using System.Linq;
using config_lib;
using dump_lib;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using trade_model;

namespace config_repo_integration_tests
{
    [TestClass]
    public class ConfigRepoTests
    {
        private ConfigRepo _configRepo;

        [TestInitialize]
        public void Setup()
        {
            _configRepo = new ConfigRepo();
        }
        
        [TestMethod]
        public void Config_repo__get_hitbtc_api_key()
        {
            var result = _configRepo.GetHitBtcApiKey();
            result.Dump();
        }
    }
}
