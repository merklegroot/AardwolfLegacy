using System.Threading;
using System.Threading.Tasks;
using cache_lib.Models;
using cryptocompare_client_lib;
using cryptocompare_service_con;
using dump_lib;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using web_util;
using client_lib;

namespace cryptocompare_client_tests
{
    [TestClass]
    public class CryptoCompareClientTests
    {
        private const string TestQueue = "CryptoCompareTestQueue";
        private CryptoCompareClient _client;

        [TestInitialize]
        public void Setup()
        {
            var webUtil = new WebUtil();
            var serviceInvoker = new ServiceInvoker(webUtil);
            _client = new CryptoCompareClient();
        }

        private void StartProgram()
        {
            var slim = new ManualResetEventSlim(false);

            var runner = new CryptoCompareServiceRunner();
            runner.OnStarted += () => { slim.Set(); };
            var task = new Task(() =>
            {
                runner.Run(TestQueue);
            }, TaskCreationOptions.LongRunning);
            task.Start();

            slim.Wait();
        }

        [TestMethod]
        public void CryptoCompare_client__get_lisk_value()
        {
            var result = _client.GetUsdValue("LSK", CachePolicy.AllowCache);
            result.Dump();
        }

        [TestMethod]
        public void CryptoCompare_client__get_lisk_prices()
        {
            var result = _client.GetPrices("LSK", CachePolicy.AllowCache);
            result.Dump();
        }

        [TestMethod]
        public void CryptoCompare_client__get_xem_prices()
        {
            var result = _client.GetPrices("XEM", CachePolicy.AllowCache);
            result.Dump();
        }
    }
}
