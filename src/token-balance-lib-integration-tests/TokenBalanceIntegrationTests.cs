using cache_lib.Models;
using dump_lib;
using client_lib;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using token_balance_lib;
using trade_contracts;
using trade_contracts.Constants;
using trade_model;
using trade_res;
using web_util;
using config_client_lib;
using exchange_client_lib;

namespace token_balance_lib_integration_tests
{
    [TestClass]
    public class TokenBalanceIntegrationTests
    {
        private TokenBalanceIntegration _tokenBalanceIntegration;
        private IExchangeClient _exchangeClient;
        private string _ethWalletAddress;

        [TestInitialize]
        public void Setup()
        {
            var configClient = new ConfigClient();
            var webUtil = new WebUtil();

            _ethWalletAddress = configClient.GetMewWalletAddress();
            _exchangeClient = new ExchangeClient();         
            _tokenBalanceIntegration = new TokenBalanceIntegration(webUtil, configClient);
        }

        [TestMethod]
        public void Token_balance__get_sub_balance__only_use_cache_unless_empty()
        {
            var results = _tokenBalanceIntegration.GetTokenBalance(_ethWalletAddress, CommodityRes.Substratum.ContractId, CachePolicy.OnlyUseCacheUnlessEmpty);
            results.Dump();
        }

        [TestMethod]
        public void Token_balance__get_sub_balance__force_refresh()
        {
            var results = _tokenBalanceIntegration.GetTokenBalance(_ethWalletAddress, CommodityRes.Substratum.ContractId, CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Token_balance__find_coss_unprocessed_deposited()
        {
            var cachePolicy = CachePolicy.AllowCache;
            var cossCommodities = _exchangeClient.GetCommoditiesForExchange(Exchanges.Coss, CachePolicy.AllowCache);

            foreach (var commodity in cossCommodities)
            {
                try
                {
                    if (string.Equals(commodity.Symbol, "ICX", StringComparison.InvariantCultureIgnoreCase))
                    {
                        continue;
                    }

                    if (!(commodity.ContractAddress ?? string.Empty).ToUpper().StartsWith("0x".ToUpper()))
                    {
                        continue;
                    }

                    var depositAddress = _exchangeClient.GetDepositAddress(Exchanges.Coss, commodity.Symbol, CachePolicy.AllowCache);
                    if (string.IsNullOrWhiteSpace(depositAddress?.Address))
                    {
                        Console.WriteLine($"Deposit address for {commodity.Symbol} is unavailable.");
                        continue;
                    }

                    var balance = _tokenBalanceIntegration.GetTokenBalance(depositAddress.Address, commodity.ContractAddress, cachePolicy);
                    Console.WriteLine($"{commodity.Symbol}: {balance}");
                }
                catch (Exception exception)
                {
                    Console.WriteLine($"Exception occurred for {commodity.Symbol}");
                    Console.WriteLine(exception);
                }
            }
        }
    }
}
