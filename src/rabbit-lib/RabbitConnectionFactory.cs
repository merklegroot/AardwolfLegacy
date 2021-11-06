using env_config_lib;
using RabbitMQ.Client;

namespace rabbit_lib
{
    public class RabbitConnectionFactory : IRabbitConnectionFactory
    {
        private const string DefaultRabbitHostName = "localhost";

        private readonly IEnvironmentConfigRepo _envConfigRepo;

        public RabbitConnectionFactory(IEnvironmentConfigRepo envConfigRepo)
        {
            _envConfigRepo = envConfigRepo;
        }

        public IRabbitConnection Connect()
        {
            var config = _envConfigRepo.GetRabbitClientConfig();
            var rabbitHostName = config?.Host;
            var userName = config?.UserName;
            var password = config?.Password;

            var effectiveHostName = !string.IsNullOrWhiteSpace(rabbitHostName)
                ? rabbitHostName.Trim() : DefaultRabbitHostName;

            var connectionFactory = new ConnectionFactory
            {
                HostName = effectiveHostName
            };

            if (!string.IsNullOrWhiteSpace(userName))
            {
                connectionFactory.UserName = userName;
            }

            if (!string.IsNullOrWhiteSpace(password))
            {
                connectionFactory.Password = password;
            }

            var connection = connectionFactory.CreateConnection();
            var channel = connection.CreateModel();

            return new RabbitConnection
            {
                _connection = connection,
                _channel = channel
            };
        }

        public void OverrideConfigKey(string configKey)
        {
            _envConfigRepo.OverrideConfigKey(configKey);
        }

        public void UseDefaultConfigKey()
        {
            _envConfigRepo.UseDefaultConfigKey();
        }
    }
}
