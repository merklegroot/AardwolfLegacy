using cache_lib.Models;
using dump_lib;
using iridium_lib;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading;
using System.Threading.Tasks;
using valuation_client_lib;
using valuation_service_con;
using web_util;

namespace valuation_lib_tests
{
    [TestClass]
    public class ValuationClientTests
    {
        private const string ValuationTestQueue = "ValuationTestQueue";
        private ValuationClient _client;

        [TestInitialize]
        public void Setup()
        {
            var webUtil = new WebUtil();
            var serviceInvoker = new ServiceInvoker(webUtil);
            _client = new ValuationClient();
        }

        private void StartProgram()
        {
            var slim = new ManualResetEventSlim(false);

            var runner = new ValuationServiceRunner();
            runner.OnStarted += () => { slim.Set(); };
            var task = new Task(() =>
            {
                runner.Run(ValuationTestQueue);
            }, TaskCreationOptions.LongRunning);
            task.Start();

            slim.Wait();
        }

        [TestMethod]
        public void Valuation_client__get_lisk_value()
        {
            var result = _client.GetUsdValue("LSK", CachePolicy.AllowCache);
            result.Dump();
        }
    }
}
