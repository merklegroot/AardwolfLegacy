using System;
using cache_lib.Models;
using cryptocompare_lib;
using dump_lib;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using config_client_lib;
using web_util;

namespace cryptocompare_integration_tests
{
    [TestClass]
    public class CryptoCompareTests
    {
        private IWebUtil _webUtil;

        private ICryptoCompareIntegration _integration;

        [TestInitialize]
        public void Setup()
        {
            _webUtil = new WebUtil();

            _integration = new CryptoCompareIntegration(_webUtil, new ConfigClient());
        }

        [TestMethod]
        public void CryptoCompare__get_ark_prices()
        {
            _integration.GetPrices("ARK", CachePolicy.ForceRefresh).Dump();
        }

        [TestMethod]
        public void CryptoCompare__get_btc_price()
        {
            var price = _integration.GetPrice("BTC", "USD", CachePolicy.ForceRefresh);
            price.Dump();
        }

        [TestMethod]
        public void CryptoCompare__get_eth_btc_ratio()
        {
            var results = _integration.GetEthToBtcRatio(CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void CryptoCompare__get_eth_price()
        {
            var price = _integration.GetPrice("ETH", "USD", CachePolicy.ForceRefresh);
            price.Dump();
        }

        [TestMethod]
        public void CryptoCompare__get_usdt_price()
        {
            var price = _integration.GetPrice("USDT", "USD", CachePolicy.ForceRefresh);
            price.Dump();
        }

        [TestMethod]
        public void CryptoCompare__get_usdt_value()
        {
            var result = _integration.GetUsdValueV2("USDT", CachePolicy.ForceRefresh);
            result.Dump();
        }

        [TestMethod]
        public void CryptoCompare__get_stx_prices()
        {
            _integration.GetPrices("STX", CachePolicy.ForceRefresh).Dump();
        }

        [TestMethod]
        public void CryptoCompare__get_axp_prices()
        {
            var prices = _integration.GetPrices("AXP", CachePolicy.ForceRefresh);
            prices.Dump();

            // The ETH price uses an exponent.
            // This was failing before specifying a number format when parsing it.
            prices.ContainsKey("ETH").ShouldBe(true);
        }

        [TestMethod]
        public void CryptoCompare__get_prices_for_bad_symbol()
        {
            Exception exception = null;
            try
            {
                var result = _integration.GetPrices("TEST_SYM", CachePolicy.ForceRefresh);
                result.Dump();
            }
            catch(Exception ex)
            {
                ex.Dump();
                exception = ex;                
            }

            exception.ShouldNotBeNull();
        }
    }
}
