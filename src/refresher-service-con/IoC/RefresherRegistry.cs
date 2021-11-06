using client_lib;
using config_client_lib;
using config_connection_string_lib;
using cryptocompare_client_lib;
using env_config_lib;
using exchange_client_lib;
using log_lib;
using rabbit_lib;
using refresher_service_lib.App;
using StructureMap;
using web_util;
using workflow_client_lib;

namespace refesher_service_con.IoC
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
            For<IWorkflowClient>().Use<WorkflowClient>();
            For<ICryptoCompareClient>().Use<CryptoCompareClient>();
            For<IExchangeClient>().Use<ExchangeClient>();

            For<IGetConnectionString>().Use<ConfigClient>();
            For<ILogRepo>().Use(() => new LogRepo());

            For<IRefresherServiceApp>().Use<RefresherServiceApp>();
        }
    }
}
