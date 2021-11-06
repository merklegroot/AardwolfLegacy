using cache_lib.Models;
using dump_lib;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading;
using task_lib;
using trade_constants;
using trade_res;
using workflow_client_lib;
using workflow_service_con;

namespace workflow_client_tests
{
    [TestClass]
    public class WorkflowClientTests
    {
        private static bool UseTestQueue = true;

        private const string TestQueue = "workflow-service-test-queue";
        private WorkflowClient _client;

        [TestInitialize]
        public void Setup()
        {
            _client = new WorkflowClient();
            if (UseTestQueue)
            {
                _client.OverrideQueue(TestQueue);
                _client.OverrideTimeout(TimeSpan.FromMinutes(10));
                StartProgram();
            }
        }

        private void StartProgram()
        {
            var slim = new ManualResetEventSlim(false);

            var runner = new WorkflowServiceRunner();
            runner.OnStarted += () => { slim.Set(); };
            var task = LongRunningTask.Run(() =>
            {
                runner.Run(TestQueue, true);
            });

            slim.Wait();
        }

        [TestMethod]
        public void Workflow_client__get_arb__coss_binance_sonm__only_use_cache_unless_empty()
        {
            var result = _client.GetArb(IntegrationNameRes.Coss, IntegrationNameRes.Binance, CommodityRes.Sonm.Symbol, CachePolicy.OnlyUseCacheUnlessEmpty);
            result.Dump();
        }

        [TestMethod]
        public void Workflow_client__get_arb__coss_binance_sonm__allow_cache()
        {
            var result = _client.GetArb(IntegrationNameRes.Coss, IntegrationNameRes.Binance, CommodityRes.Sonm.Symbol, CachePolicy.AllowCache);
            result.Dump();
        }

        [TestMethod]
        public void Workflow_client__get_xem_usd_value_2__force_refresh()
        {
            var result = _client.GetUsdValueV2("XEM", CachePolicy.ForceRefresh);
            result.Dump();
        }

        [TestMethod]
        public void Workflow_client__get_xdce_usd_value_2__force_refresh()
        {
            var result = _client.GetUsdValueV2("XDCE", CachePolicy.ForceRefresh);
            result.Dump();
        }

        [TestMethod]
        public void Workflow_client__get_bchabc_usd_value_2__force_refresh()
        {
            var result = _client.GetUsdValueV2("BCHABC", CachePolicy.ForceRefresh);
            result.Dump();
        }

        [TestMethod]
        public void Workflow_client__get_valuation_dictionary__only_use_cache_unless_empty()
        {
            var valuationDictionary = _client.GetValuationDictionary(CachePolicy.OnlyUseCacheUnlessEmpty);
            valuationDictionary.Dump();
        }

        [TestMethod]
        public void Workflow_client__get_valuation_dictionary__force_refresh()
        {
            var valuationDictionary = _client.GetValuationDictionary(CachePolicy.ForceRefresh);
            valuationDictionary.Dump();
        }
    }
}
