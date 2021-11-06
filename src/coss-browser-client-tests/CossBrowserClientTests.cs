using coss_browser_service_client;
using coss_browser_service_con;
using dump_lib;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace coss_browser_client_tests
{
    [TestClass]
    public class CossBrowserClientTests
    {
        // private static bool UseTestQueue = true;
        private static bool UseTestQueue = false;
        private const string TestQueue = "CossBrowserTestQueue";

        private CossBrowserClient _client;

        [TestInitialize]
        public void Setup()
        {
            _client = new CossBrowserClient();
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

            var runner = new CossBrowserServiceRunner();
            runner.OnStarted += () => { slim.Set(); };
            var task = new Task(() =>
            {
                runner.Run(TestQueue);
            }, TaskCreationOptions.LongRunning);
            task.Start();

            slim.Wait();
        }

        [TestMethod]
        public void Coss_browser_client__get_cookies()
        {
            var cookies = _client.GetCookies();
            cookies.Dump();
        }

        [TestMethod]
        public void Coss_browser_client__ping()
        {
            var result = _client.Ping();
            result.Dump();
        }
    }
}
