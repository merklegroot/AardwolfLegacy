using dump_lib;
using env_config_lib;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using rabbit_lib;
using System;
using System.Threading;
using test_shared;
using trade_constants;
using trade_contracts;

namespace kucoin_agent_integration_tests
{
    [TestClass]
    public class KucoinAgentIntegrationTests
    {
        private IRabbitConnectionFactory _rabbitConnectionFactory;

        [TestInitialize]
        public void Setup()
        {
            var envConfig = new EnvironmentConfigRepo();
            _rabbitConnectionFactory = new RabbitConnectionFactory(envConfig);
        }

        [TestMethod]
        public void Kucoin_agent__open_url()
        {
            using (var conn = _rabbitConnectionFactory.Connect())
            {
                var message = new OpenUrlRequestMessage
                {
                    Url = "http://www.asdf.com"
                };

                conn.PublishContract(TradeRabbitConstants.Queues.KucoinAgentQueue, message);
            }
        }

        [TestMethod]
        public void Kucoin_agent__get_status()
        {
            var slim = new ManualResetEventSlim();
            var req = new GetStatusRequestMessage();

            using (var conn = _rabbitConnectionFactory.Connect())
            {
                Console.WriteLine($"Listening to response queue {req.ResponseQueue}");
                conn.Listen(req.ResponseQueue, resp =>
                {
                    Console.WriteLine("Received response.");
                    resp.Dump();
                    slim.Set();
                }, true);

                conn.PublishContract(TradeRabbitConstants.Queues.KucoinAgentQueue, req);

                if (!slim.Wait(TimeSpan.FromSeconds(30)))
                {
                    Assert.Fail("Did not receive response.");
                }
            }
        }
    }
}
