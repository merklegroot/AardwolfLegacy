using cache_lib.Models;
using cryptocompare_client_lib;
using dump_lib;
using integration_workflow_lib;
using log_lib;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using web_util;
using config_client_lib;
using exchange_client_lib;
using currency_converter_lib;
using cache_lib;

namespace integration_workflow_integration_tests
{
    [TestClass]
    public class ValuationWorkflowTests
    {
        private ValuationWorkflow _workflow;

        [TestInitialize]
        public void Setup()
        {
            var webUtil = new WebUtil();
            var configClient = new ConfigClient();
            var log = new Mock<ILogRepo>();
            var cryptoCompareClient = new CryptoCompareClient();
            var exchangeClient = new ExchangeClient();

            var cacheUtil = new CacheUtil();
            var currencyConverter = new CurrencyConverterIntegration(new CurrencyConverterClient(configClient, webUtil, log.Object), configClient, cacheUtil, webUtil, log.Object); ;

            _workflow = new ValuationWorkflow(exchangeClient, currencyConverter, cryptoCompareClient, log.Object);
        }

        [TestMethod]
        public void Valuation_workflow__get_valuation_dictionary()
        {
            var results = _workflow.GetValuationDictionary(CachePolicy.AllowCache);
            results.Dump();
        }

        [TestMethod]
        public void Valuation_workflow__get_gbp__allow_cache()
        {
            var result = _workflow.GetUsdValueV2("GBP", CachePolicy.AllowCache);
            result.Dump();
        }

        [TestMethod]
        public void Valuation_workflow__get_usdt__force_refresh()
        {
            var result = _workflow.GetUsdValueV2("USDT", CachePolicy.ForceRefresh);
            result.Dump();
        }

        [TestMethod]
        public void Valuation_workflow__get_tusd__force_refresh()
        {
            var result = _workflow.GetUsdValueV2("TUSD", CachePolicy.ForceRefresh);
            result.Dump();
        }

        [TestMethod]
        public void Valuation_workflow__get_usdc__force_refresh()
        {
            var result = _workflow.GetUsdValueV2("USDC", CachePolicy.ForceRefresh);
            result.Dump();
        }

        [TestMethod]
        public void Valuation_workflow__get_coss__force_refresh()
        {
            var result = _workflow.GetUsdValueV2("COSS", CachePolicy.ForceRefresh);
            result.Dump();
        }

        [TestMethod]
        public void Valuation_workflow__get_rur__force_refresh()
        {
            var result = _workflow.GetUsdValueV2("RUR", CachePolicy.ForceRefresh);
            result.Dump();
        }
    }
}
