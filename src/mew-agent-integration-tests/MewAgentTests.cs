using env_config_lib;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using rabbit_lib;
using trade_constants;
using trade_contracts;

namespace mew_agent_integration_tests
{
    [TestClass]
    public class MewAgentTests
    {
        private IRabbitConnectionFactory _rabbitConnectionFactory;

        [TestInitialize]
        public void Setup()
        {
            var envConfig = new EnvironmentConfigRepo();
            _rabbitConnectionFactory = new RabbitConnectionFactory(envConfig);
        }

        [TestMethod]
        public void Mew_agent__login()
        {
            using (var conn = _rabbitConnectionFactory.Connect())
            {
                var message = new MewLoginRequestMessage();
                conn.PublishContract(TradeRabbitConstants.Queues.MewAgentQueue, message);
            }
        }
    }
}
