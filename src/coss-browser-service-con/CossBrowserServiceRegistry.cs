using client_lib;
using config_client_lib;
using config_connection_string_lib;
using coss_browser_service_lib.App;
using coss_browser_service_lib.Handlers;
using coss_browser_service_lib.Repo;
using coss_browser_service_lib.Workers;
using coss_browser_workflow_lib;
using env_config_lib;
using log_lib;
using proc_worfklow_lib;
using rabbit_lib;
using StructureMap;
using System;

namespace coss_browser_service_con
{
    public class CossBrowserServiceRegistry : Registry
    {
        public CossBrowserServiceRegistry()
        {
            var logRepoFactory = new Func<ILogRepo>(() =>
            {
                var getter = new Func<string>(() => new ConfigClient().GetConnectionString());

                return new LogRepo(getter);
            });

            For<ILogRepo>().Use(() => logRepoFactory());
            For<IRequestResponse>().Use<RequestResponse>();
            For<IRabbitConnectionFactory>().Use<RabbitConnectionFactory>();
            For<ICossBrowserServiceRunner>().Use<CossBrowserServiceRunner>();
            For<ICossBrowserHandler>().Use<CossBrowserHandler>();
            For<ICossBrowserServiceApp>().Use<CossBrowserServiceApp>();
            For<IEnvironmentConfigRepo>().Use<EnvironmentConfigRepo>();
            For<ICossBrowserWorkflow>().Use<CossBrowserWorkflow>();
            For<IGetConnectionString>().Use<ConfigClient>();

            For<ICossCookieRepo>().Use<CossCookieRepo>();
            For<IChromeWorkflow>().Use<ChromeWorkflow>();
            For<IChromeProcessWorker>().Use<ChromeProcessWorker>();
        }
    }
}
