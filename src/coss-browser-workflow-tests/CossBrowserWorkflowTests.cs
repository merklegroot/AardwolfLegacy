using coss_browser_workflow_lib;
using dump_lib;
using log_lib;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace coss_browser_workflow_tests
{
    [TestClass]
    public class CossBrowserWorkflowTests
    {
        private CossBrowserWorkflow _workflow;

        [TestInitialize]
        public void Setup()
        {
            var log = new Mock<ILogRepo>();
            _workflow = new CossBrowserWorkflow(log.Object);
        }

        [TestMethod]
        public void Coss_browser_workflow__get_coss_cookies()
        {
            var results = _workflow.GetCossCookies();
            results.Dump();
        }
    }
}
