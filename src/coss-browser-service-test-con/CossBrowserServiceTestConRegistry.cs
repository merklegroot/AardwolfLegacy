using coss_browser_service_client;
using coss_browser_workflow_lib;
using config_client_lib;
using log_lib;
using rabbit_lib;
using StructureMap;
using System;
using client_lib;
using exchange_client_lib;
using env_config_lib;

namespace coss_browser_service_test_con
{
    public class CossBrowserServiceTestConRegistry : Registry
    {
        public CossBrowserServiceTestConRegistry()
        {
            var logRepoFactory = new Func<ILogRepo>(() =>
            {
                var getter = new Func<string>(() => new ConfigClient().GetConnectionString());

                return new LogRepo(getter);
            });

            For<IEnvironmentConfigRepo>().Use<EnvironmentConfigRepo>();

            For<ILogRepo>().Use(() => logRepoFactory());

            For<ICossBrowserServiceTestConApp>().Use<CossBrowserServiceTestConApp>();
            For<ICossBrowserWorkflow>().Use<CossBrowserWorkflow>();

            For<IRequestResponse>().Use<RequestResponse>();
            For<IRabbitConnectionFactory>().Use<RabbitConnectionFactory>();

            For<IConfigClient>().Use<ConfigClient>();
            For<ICossBrowserClient>().Use<CossBrowserClient>();
            For<IExchangeClient>().Use<ExchangeClient>();
        }
    }
}
