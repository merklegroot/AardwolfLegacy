using config_model;
using config_service_con;
using dump_lib;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using System;
using System.Threading;
using System.Threading.Tasks;
using config_client_lib;
using trade_res;
using trade_model.ArbConfig;

namespace config_client_lib_tests
{
    [TestClass]
    public class ConfigClientTests
    {
        private const string ConfigTestQueue = "ConfigTestQueue";
        private static bool ShouldUseTestQueue = true;
        private IConfigClient _client;

        [TestInitialize]
        public void Setup()
        {
            _client = new ConfigClient();
            if (ShouldUseTestQueue)
            {
                _client.OverrideQueue(ConfigTestQueue);
            }

            StartProgram();
        }

        private void StartProgram()
        {
            if (!ShouldUseTestQueue) { return; }

            var slim = new ManualResetEventSlim(false);

            var runner = new ConfigServiceRunner();
            runner.OnStarted += () => { slim.Set(); };
            var task = new Task(() =>
            {
                runner.Run(ConfigTestQueue);
            }, TaskCreationOptions.LongRunning);
            task.Start();

            slim.Wait();
        }

        [TestMethod]
        public void Config_client__get_connection_string()
        {
            var result = _client.GetConnectionString();
            result.Dump();
        }

        [TestMethod]
        public void Config_client__ping()
        {
            var result = _client.Ping();
            result.Dump();
        }

        [TestMethod]
        public void Config_client__get_bitz_trade_password()
        {
            var result = _client.GetBitzTradePassword();
            result.Dump();
        }

        [TestMethod]
        public void Config_client__set_bitz_trade_password()
        {
            var original = _client.GetBitzTradePassword();

            const string UpdatedPassword = "testing123";
            _client.SetBitzTradePassword(UpdatedPassword);

            var modifiedResult = _client.GetBitzTradePassword();

            _client.SetBitzTradePassword(original);
            var afterReset = _client.GetBitzTradePassword();

            modifiedResult.ShouldBe(UpdatedPassword);
            afterReset.ShouldBe(original);
        }

        [TestMethod]
        public void Config_client__get_mew_wallet_file_name()
        {
            var result = _client.GetMewWalletFileName();
            result.Dump();
        }

        [TestMethod]
        public void Config_client__get_mew_wallet_address()
        {
            var result = _client.GetMewWalletAddress();
            result.Dump();
        }

        [TestMethod]
        public void Config_client__set_binance_arb_config()
        {
            var shouldRun = false;
            if (!shouldRun)
            {
                throw new ApplicationException("This test alters an arb configuration and must be run manually.");
            }
            
            var arbConfig = new BinanceArbConfig
            {
                IsEnabled = true,
                ArkSaleTarget = "BTC",
                TusdSaleTarget = "BTC"
            };

            _client.SetBinanceArbConfig(arbConfig);

            var retrievedArbConfig = _client.GetBinanceArbConfig();

            retrievedArbConfig.Dump();
        }

        [TestMethod]
        public void Config_client__disable_binance_arb_service()
        {
            var arbConfig = _client.GetBinanceArbConfig();
            arbConfig.IsEnabled = false;

            _client.SetBinanceArbConfig(arbConfig);
        }

        [TestMethod]
        public void Config_client__set_mew_wallet_address()
        {
            var shouldRun = false;
            if (!shouldRun)
            {
                throw new ApplicationException("This test alters the mew address and should only be run manualy.");
            }

            var originalAddress = _client.GetMewWalletAddress();

            const string TestAddress = "12345";
            _client.SetMewWalletAddress(TestAddress);

            var result = _client.GetMewWalletAddress();
            result.Dump();
            result.ShouldBe(TestAddress);

            _client.SetMewWalletAddress(originalAddress);
            result.ShouldBe(originalAddress);
        }

        [TestMethod]
        public void Config_client__set_connetion_string()
        {
            var shouldRun = false;
            if (!shouldRun)
            {
                throw new ApplicationException("This test alters the mew address and should only be run manualy.");
            }

            var original = _client.GetConnectionString();

            const string TestConnectionString = "12345";
            _client.SetConnectionString(TestConnectionString);

            var result = _client.GetConnectionString();
            result.Dump();
            result.ShouldBe(TestConnectionString);

            _client.SetConnectionString(original);
            var again = _client.GetConnectionString();

            again.ShouldBe(original);
        }

        [TestMethod]
        public void Config_client__get_binance_api_key()
        {
            var result = _client.GetApiKey("binance");
            result.Dump();
        }

        [TestMethod]
        public void Config_client__get_ccxt_url()
        {
            var result = _client.GetCcxtUrl();
            result.Dump();
        }

        [TestMethod]
        public void Config_client__get_coss_credentials()
        {
            var result = _client.GetCossCredentials();
            result.Dump();
        }

        [TestMethod]
        public void Config_client__get_bitz_login_credentials()
        {
            var result = _client.GetBitzLoginCredentials();
            result.Dump();
        }

        [TestMethod]
        public void Config_client__get_coss_agent_config()
        {
            var result = _client.GetCossAgentConfig();
            result.Dump();
        }

        [TestMethod]
        public void Config_client__set_coss_agent_config()
        {
            var shouldRun = false;
            if (!shouldRun)
            {
                throw new ApplicationException("This test modifies the configuration and should only be run manualy.");
            }

            var original = _client.GetCossAgentConfig();

            _client.SetCossAgentConfig(new CossAgentConfig
            {
                EthThreshold = 123,
                IsCossAutoTradingEnabled = true,
                TokenThreshold = 567
            });

            var modified = _client.GetCossAgentConfig();
            modified.EthThreshold.ShouldBe(123);
            modified.IsCossAutoTradingEnabled.ShouldBe(true);
            modified.TokenThreshold.ShouldBe(567);

            _client.SetCossAgentConfig(original);
        }

        [TestMethod]
        public void Config_client__get_coinbase_api_key()
        {
            var result = _client.GetApiKey(ExchangeNameRes.Coinbase);
            result.Dump();
        }

        [TestMethod]
        public void Config_client__set_coinbase_api_key()
        {
            var originalApiKey = _client.GetApiKey(ExchangeNameRes.Coinbase);

            const string ModifiedApiKey = "sfsfdsdf";
            const string ModifiedApiSecret = "sdfsdsdf";

            try
            {
                _client.SetApiKey(ExchangeNameRes.Coinbase, ModifiedApiKey, ModifiedApiSecret);
                var updatedApiKey = _client.GetApiKey(ExchangeNameRes.Coinbase);

                updatedApiKey.Key.ShouldBe(ModifiedApiKey);
                updatedApiKey.Secret.ShouldBe(ModifiedApiSecret);
            }
            finally
            {
                _client.SetApiKey(ExchangeNameRes.Coinbase, originalApiKey?.Key, originalApiKey?.Secret);
            }
        }
    }
}
