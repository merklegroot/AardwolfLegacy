using config_client_lib;
using idex_agent_lib;
using idex_client_lib;
using idex_data_lib;
using idex_integration_lib;
using log_lib;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using web_util;

namespace idex_agent_lib_integration_tests
{
    [TestClass]
    public class IdexAgentWorkflowTests
    {
        private IdexAgentWorkflow _idexAgentWorkflow;

        [TestInitialize]
        public void Setup()
        {
            var webUtil = new WebUtil();
            var configClient = new ConfigClient();
            var holdingsRepo = new IdexHoldingsRepo(configClient);
            var orderBookRepo = new IdexOrderBookRepo(configClient);
            var openOrdersRepo = new IdexOpenOrdersRepo(configClient);
            var historyRepo = new IdexHistoryRepo(configClient);
            var idexClient = new IdexClient(webUtil);
            var log = new Mock<ILogRepo>();

            var idex = new IdexIntegration(webUtil, configClient, holdingsRepo, orderBookRepo, openOrdersRepo, historyRepo, idexClient, log.Object);
            _idexAgentWorkflow = new IdexAgentWorkflow(configClient, idex);
        }

        [TestMethod]
        public void Idex_agent_workflow()
        {
            _idexAgentWorkflow.Execute();
        }
    }
}
