using env_config_lib;
using client_lib;
using log_lib;
using rabbit_lib;
using StructureMap;
using System;
using config_client_lib;
using exchange_client_lib;
using workflow_client_lib;

namespace service_test_con
{
    public class ServiceTestConRegistry : Registry
    {
        public ServiceTestConRegistry()
        {
            var logRepoFactory = new Func<ILogRepo>(() =>
            {
                var getter = new Func<string>(() => new ConfigClient().GetConnectionString());

                return new LogRepo(getter);
            });

            For<ILogRepo>().Use(() => logRepoFactory());

            For<IEnvironmentConfigRepo>().Use<EnvironmentConfigRepo>();
            
            For<IRequestResponse>().Use<RequestResponse>();
            For<IRabbitConnectionFactory>().Use<RabbitConnectionFactory>();

            For<IConfigClient>().Use<ConfigClient>();
            For<IExchangeClient>().Use<ExchangeClient>();            
            For<IWorkflowClient>().Use<WorkflowClient>();

            For<IServiceTestConApp>().Use<ServiceTestConApp>();
        }
    }
}
