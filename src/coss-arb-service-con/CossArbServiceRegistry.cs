using coss_arb_lib;
using coss_arb_service_lib.App;
using coss_arb_service_lib.Handlers;
using coss_arb_service_lib.Workflows;
using env_config_lib;
using client_lib;
using log_lib;
using rabbit_lib;
using StructureMap;
using System;
using workflow_client_lib;
using config_client_lib;
using exchange_client_lib;
using arb_workflow_lib;

namespace coss_arb_service_con
{
    public class CossArbServiceRegistry : Registry
    {
        public CossArbServiceRegistry()
        {
            var logRepoFactory = new Func<ILogRepo>(() =>
            {
                var getter = new Func<string>(() => new ConfigClient().GetConnectionString());

                return new LogRepo(getter);
            });

            For<ILogRepo>().Use(() => logRepoFactory());
            For<IRequestResponse>().Use<RequestResponse>();
            For<IRabbitConnectionFactory>().Use<RabbitConnectionFactory>();
            For<ICossArbServiceRunner>().Use<CossArbServiceRunner>();
            For<ICossArbHandler>().Use<CossArbHandler>();
            For<ICossArbServiceApp>().Use<CossArbServiceApp>();
            For<IEnvironmentConfigRepo>().Use<EnvironmentConfigRepo>();
            For<ICossArbServiceApp>().Use<CossArbServiceApp>();
            For<ICossArbUtil>().Use<CossArbUtil>();
            For<ICossArbWorker>().Use<CossArbWorker>();

            For<IConfigClient>().Use<ConfigClient>();
            For<IExchangeClient>().Use<ExchangeClient>();
            For<IWorkflowClient>().Use<WorkflowClient>();

            For<IArbWorkflowUtil>().Use<ArbWorkflowUtil>();
        }
    }
}
