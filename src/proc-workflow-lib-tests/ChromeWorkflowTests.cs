using proc_worfklow_lib;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace proc_workflow_lib_tests
{
    [TestClass]
    public class ChromeWorkflowTests
    {
        private ChromeWorkflow _chromeWorkflow;

        [TestInitialize]
        public void Setup()
        {
            _chromeWorkflow = new ChromeWorkflow();
        }

        [TestMethod]
        public void Chrome_workflow__launch()
        {
            _chromeWorkflow.LaunchWaitClose("https://profile.coss.io");
        }
    }
}
