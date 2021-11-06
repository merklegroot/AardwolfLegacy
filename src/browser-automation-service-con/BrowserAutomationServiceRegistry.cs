using browser_automation_service_lib.App;
using browser_automation_service_lib.Handlers;
using browser_automation_service_lib.Workflow;
using client_lib;
using config_client_lib;
using env_config_lib;
using log_lib;
using rabbit_lib;
using StructureMap;
using System;

namespace browser_automation_service_con
{
    public class BrowserAutomationServiceRegistry : Registry
    {
        public BrowserAutomationServiceRegistry()
        {
            var logRepoFactory = new Func<ILogRepo>(() =>
            {
                var getter = new Func<string>(() => new ConfigClient().GetConnectionString());

                return new LogRepo(getter);
            });

            For<ILogRepo>().Use(() => logRepoFactory());
            For<IRequestResponse>().Use<RequestResponse>();
            For<IRabbitConnectionFactory>().Use<RabbitConnectionFactory>();
            For<IBrowserAutomationServiceRunner>().Use<BrowserAutomationServiceRunner>();
            For<IBrowserAutomationHandler>().Use<BrowserAutomationHandler>();
            For<IBrowserAutomationServiceApp>().Use<BrowserAutomationServiceApp>();
            For<IEnvironmentConfigRepo>().Use<EnvironmentConfigRepo>();
            For<IBrowserAutomationWorkflow>().Use<BrowserAutomationWorkflow>();
        }
    }
}
