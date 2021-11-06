using System;
using dump_lib;
using browser_automation_client_lib;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using System.Threading;
using browser_automation_service_con;
using task_lib;
using System.Diagnostics;

namespace browser_automation_client_tests
{
    [TestClass]
    public class BrowserAutomationClientTests
    {
        // private static bool UseTestQueue = true;
        private static bool UseTestQueue = false;

        private const string TestQueue = "BrowserAutomationTestQueue";

        private BrowserAutomationClient _client;

        [TestInitialize]
        public void Setup()
        {
            _client = new BrowserAutomationClient();
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

            var runner = new BrowserAutomationServiceRunner();
            runner.OnStarted += () => { slim.Set(); };
            var task = LongRunningTask.Run(() =>
            {
                runner.Run(TestQueue);
            });

            slim.Wait();
        }

        [TestMethod]
        public void Browser_automation_client__navigate_and_get_contents__asdf()
        {
            var contents = _client.NavigateAndGetContents("http://asdf.com");
            contents.Dump();

            contents.ShouldNotBeNullOrWhiteSpace();
        }

        [TestMethod]
        public void Browser_automation_client__get_hitbtc_health_status_contents()
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            var contents = _client.GetHitBtcHealthStatusContents();
            stopWatch.Stop();

            Console.WriteLine($"It took {stopWatch.Elapsed.ToString()} to get the hitbtc health status contents.");
            contents.Dump();

            contents.ShouldNotBeNullOrWhiteSpace();
        }

        [TestMethod]
        public void Browser_automation_client__ping()
        {
            var response = _client.Ping();
            response.Dump();
        }
    }
}
