using client_lib;
using config_client_lib;
using env_config_lib;
using exchange_client_lib;
using iridum_con.App;
using rabbit_lib;
using StructureMap;
using web_util;

namespace iridum_con
{
    public class IridiumRegistry : Registry
    {
        public IridiumRegistry()
        {
            For<IEnvironmentConfigRepo>().Use<EnvironmentConfigRepo>();
            For<IRabbitConnectionFactory>().Use<RabbitConnectionFactory>();
            For<IWebUtil>().Use<WebUtil>();
            For<IServiceInvoker>().Use<ServiceInvoker>();
            For<IRequestResponse>().Use<RequestResponse>();

            For<IConfigClient>().Use<ConfigClient>();
            For<IExchangeClient>().Use<ExchangeClient>();

            For<IIridiumApp>().Use<IridiumApp>();
        }
    }
}
