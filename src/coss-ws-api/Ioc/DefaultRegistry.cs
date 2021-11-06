using client_lib;
using config_client_lib;
using coss_ws_lib;
using env_config_lib;
using exchange_client_lib;
using log_lib;
using rabbit_lib;
using StructureMap;
using System;
using workflow_client_lib;

namespace coss_ws_api.Ioc
{
    public class DefaultRegistry : Registry
    {
        public DefaultRegistry()
        {
            var logRepoFactory = new Func<ILogRepo>(() =>
            {
                var getter = new Func<string>(() => new ConfigClient().GetConnectionString());
                return new LogRepo(getter);
            });

            For<ILogRepo>().Use(() => logRepoFactory());
            For<IRequestResponse>().Use<RequestResponse>();
            For<IRabbitConnectionFactory>().Use<RabbitConnectionFactory>();
            For<IEnvironmentConfigRepo>().Use<EnvironmentConfigRepo>();
            For<IConfigClient>().Use<ConfigClient>();
            For<IExchangeClient>().Use<ExchangeClient>();
            For<IWorkflowClient>().Use<WorkflowClient>();

            For<ICossWsWorkflow>().Use<CossWsWorkflow>();
        }
    }
}