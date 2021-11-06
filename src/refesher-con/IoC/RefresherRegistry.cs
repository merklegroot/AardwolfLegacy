using client_lib;
using config_client_lib;
using config_connection_string_lib;
using cryptocompare_lib;
using env_config_lib;
using exchange_client_lib;
using iridium_lib;
using log_lib;
using rabbit_lib;
using refesher_lib;
using refresher_lib;
using StructureMap;
using web_util;

namespace refesher_con.IoC
{
    public class RefresherRegistry : Registry
    {
        public RefresherRegistry()
        {
            For<IEnvironmentConfigRepo>().Use<EnvironmentConfigRepo>();
            For<IRabbitConnectionFactory>().Use<RabbitConnectionFactory>();
            For<IWebUtil>().Use<WebUtil>();
            For<IServiceInvoker>().Use<ServiceInvoker>();
            For<IRequestResponse>().Use<RequestResponse>();

            For<IConfigClient>().Use<ConfigClient>();
            For<IExchangeClient>().Use<ExchangeClient>();

            For<ICryptoCompareIntegration>().Use<CryptoCompareIntegration>();
            For<IGetConnectionString>().Use<ConfigClient>();
            For<ILogRepo>().Use(() => new LogRepo());

            For<IRefresherApp>().Use<RefresherApp>();
        }
    }
}
