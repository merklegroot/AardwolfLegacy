using arb_workflow_lib;
using config_client_lib;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace arb_workflow_lib_unit_tests
{
    [TestClass]
    public class ArbWorkflowUtilTests
    {
        private Mock<IConfigClient> _configClient;
        private IArbWorkflowUtil _arbWorkflowUtil;

        [TestInitialize]
        public void Setup()
        {
            // _arbWorkflowUtil = new ArbWorkflowUtil()
        }

        [TestMethod]
        public void TestMethod1()
        {
        }
    }
}
