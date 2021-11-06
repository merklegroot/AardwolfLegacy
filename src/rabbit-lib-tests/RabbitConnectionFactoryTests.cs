using env_config_lib;
using env_config_lib.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using rabbit_lib;
using Shouldly;

namespace rabbit_lib_tests
{
    [TestClass]
    public class RabbitConnectionFactoryTests
    {
        private EnvironmentConfigRepo _envConfig;
        private RabbitConnectionFactory _rabbitConnectionFactory;

        [TestInitialize]
        public void Setup()
        {
            _envConfig = new EnvironmentConfigRepo();
            _rabbitConnectionFactory = new RabbitConnectionFactory(_envConfig);
        }

        [TestMethod]
        public void Rabbit_connection_factory__connect_with_defaults()
        {
            using (var conn = _rabbitConnectionFactory.Connect())
            {
                conn.ShouldNotBeNull();
                conn.IsOpen.ShouldBe(true);
            }
        }

        [TestMethod]
        public void Rabbit_connection_factory__connect_with_credentials()
        {
            var clientConfig = _envConfig.GetRabbitClientConfig();

            using (var conn = _rabbitConnectionFactory.Connect())
            {
                conn.ShouldNotBeNull();
                conn.IsOpen.ShouldBe(true);
            }
        }
    }
}
