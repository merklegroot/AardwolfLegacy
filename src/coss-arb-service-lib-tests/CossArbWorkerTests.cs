using arb_workflow_lib;
using cache_lib.Models;
using config_client_lib;
using coss_arb_lib;
using coss_arb_service_lib.Workflows;
using dump_lib;
using exchange_client_lib;
using log_lib;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using trade_constants;
using workflow_client_lib;

namespace coss_arb_service_lib_tests
{
    [TestClass]
    public class CossArbWorkerTests
    {
        private ExchangeClient _exchangeClient;
        private CossArbWorker _worker;

        [TestInitialize]
        public void Setup()
        {
            var configClient = new ConfigClient();
            _exchangeClient = new ExchangeClient();
            var workflowClient = new WorkflowClient();
            var log = new LogRepo(true);
            var cossArbUtil = new CossArbUtil(configClient, _exchangeClient, workflowClient, log);
            var arbWorkflowUtil = new ArbWorkflowUtil(configClient, _exchangeClient, workflowClient, log);

            _worker = new CossArbWorker(configClient, _exchangeClient, cossArbUtil, arbWorkflowUtil, log);
        }

        [TestMethod]
        public void CossArbWorker__make_sure_we_can_get_open_orders()
        {
            var results = _exchangeClient.GetOpenOrdersForTradingPairV2(IntegrationNameRes.Coss, "REQ", "ETH", CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void CossArbWorker__auto_symbol_process_v2__req()
        {
            _worker.AutoSymbolProcessV2Symbol("REQ", IntegrationNameRes.Binance, true);
        }

        [TestMethod]
        public void CossArbWorker__auto_symbol_process_v2__fyn()
        {
            _worker.AutoSymbolProcessV2Symbol("FYN", IntegrationNameRes.HitBtc, true);
        }

        [TestMethod]
        public void CossArbWorker__auto_symbol_process_v2__h2o()
        {
            _worker.AutoSymbolProcessV2Symbol("H2O", IntegrationNameRes.Idex, true);
        }

        [TestMethod]
        public void CossArbWorker__auto_symbol_process_v2__wish__cryptopia()
        {
            _worker.AutoSymbolProcessV2Symbol("WISH", IntegrationNameRes.Cryptopia, true);
        }

        [TestMethod]
        public void CossArbWorker__auto_symbol_process_v2__can()
        {
            _worker.AutoSymbolProcessV2Symbol("CAN", IntegrationNameRes.KuCoin, true);
        }

        [TestMethod]
        public void CossArbWorker__auto_symbol_process_v2__zen()
        {
            _worker.AutoSymbolProcessV2Symbol("ZEN", IntegrationNameRes.Binance, true);
        }

        [TestMethod]
        public void CossArbWorker__auto_symbol_process_v2()
        {
            _worker.AutoSymbolProcessV2Dictionary(null, true);
        }

        //[TestMethod]
        //public void CossArbWorker__acquire_ltc_processor()
        //{
        //    _worker.AcquireLtcProcessor();
        //}

        [TestMethod]
        public void CossArbWorker__acquire_coss_processor()
        {
            _worker.AcquireCossProcessor();
        }
    }
}
