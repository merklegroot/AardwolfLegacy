using arb_workflow_lib;
using client_lib;
using config_client_lib;
using env_config_lib;
using exchange_client_lib;
using hitbtc_arb_service_lib.App;
using hitbtc_arb_service_lib.Workers;
using hitbtc_arb_service_lib.Workflow;
using log_lib;
using rabbit_lib;
using StructureMap;
using System;
using workflow_client_lib;

namespace hitbtc_arb_service_con
{
    public class HitbtcArbServiceRegistry : Registry
    {
        public HitbtcArbServiceRegistry()
        {
            var logRepoFactory = new Func<ILogRepo>(() =>
            {
                var getter = new Func<string>(() => new ConfigClient().GetConnectionString());

                return new LogRepo(getter);
            });

            For<ILogRepo>().Use(() => logRepoFactory());
            For<IRequestResponse>().Use<RequestResponse>();
            For<IRabbitConnectionFactory>().Use<RabbitConnectionFactory>();
            For<IHitbtcArbServiceRunner>().Use<HitbtcArbServiceRunner>();
            For<IHitBtcArbServiceApp>().Use<HitbtcArbServiceApp>();
            For<IEnvironmentConfigRepo>().Use<EnvironmentConfigRepo>();
            For<IHitBtcArbServiceApp>().Use<HitbtcArbServiceApp>();
            For<IArbWorkflowUtil>().Use<ArbWorkflowUtil>();
            For<IHitbtcArbWorker>().Use<HitbtcArbWorker>();
            For<IConfigClient>().Use<ConfigClient>();
            For<IExchangeClient>().Use<ExchangeClient>();
            For<IWorkflowClient>().Use<WorkflowClient>();

            For<IHitbtcArbWorkflowUtil>().Use<HitbtcArbWorkflowUtil>();
        }
    }
}
