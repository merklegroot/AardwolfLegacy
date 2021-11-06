using arb_workflow_lib;
using client_lib;
using config_client_lib;
using env_config_lib;
using exchange_client_lib;
using log_lib;
using bitz_arb_service_lib.App;
using bitz_arb_service_lib.Handlers;
using bitz_arb_service_lib.Workers;
using rabbit_lib;
using StructureMap;
using System;
using workflow_client_lib;

namespace bitz_arb_service_con
{
    internal class BitzArbServiceRegistry : Registry
    {
        public BitzArbServiceRegistry()
        {
            var logRepoFactory = new Func<ILogRepo>(() =>
            {
                var getter = new Func<string>(() => new ConfigClient().GetConnectionString());

                return new LogRepo(getter);
            });

            For<ILogRepo>().Use(() => logRepoFactory());
            For<IRequestResponse>().Use<RequestResponse>();
            For<IRabbitConnectionFactory>().Use<RabbitConnectionFactory>();
            For<IBitzArbServiceRunner>().Use<BitzArbServiceRunner>();
            For<IBitzArbHandler>().Use<BitzArbHandler>();
            For<IBitzArbServiceApp>().Use<BitzArbServiceApp>();
            For<IEnvironmentConfigRepo>().Use<EnvironmentConfigRepo>();
            For<IBitzArbServiceApp>().Use<BitzArbServiceApp>();
            For<IArbWorkflowUtil>().Use<ArbWorkflowUtil>();
            For<IBitzArbWorker>().Use<BitzArbWorker>();
            For<IConfigClient>().Use<ConfigClient>();
            For<IExchangeClient>().Use<ExchangeClient>();
            For<IWorkflowClient>().Use<WorkflowClient>();
        }
    }
}
