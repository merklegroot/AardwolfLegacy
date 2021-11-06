using coss_agent_lib.Strategy;
using client_lib;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using workflow_client_lib;
using exchange_client_lib;

namespace coss_agent_integration_tests
{
    [TestClass]
    public class CossAutoOpenAskTests
    {
        private CossAutoOpenAsk _cossAutoOpenAsk;

        [TestInitialize]
        public void Setup()
        {
            var exchangeClient = new ExchangeClient();
            var workflowClient = new WorkflowClient();
            _cossAutoOpenAsk = new CossAutoOpenAsk(exchangeClient, workflowClient);
        }

        [TestMethod]
        public void Coss_auto_open_ask__lsk()
        {
            _cossAutoOpenAsk.ExecuteLisk();
        }
    }
}
