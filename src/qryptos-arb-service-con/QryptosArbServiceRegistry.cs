using arb_workflow_lib;
using client_lib;
using config_client_lib;
using env_config_lib;
using exchange_client_lib;
using log_lib;
using qryptos_arb_service_lib.App;
using qryptos_arb_service_lib.Handlers;
using qryptos_arb_service_lib.Workers;
using rabbit_lib;
using StructureMap;
using System;
using workflow_client_lib;

namespace qryptos_arb_service_con
{
    internal class QryptosArbServiceRegistry : Registry
    {
        public QryptosArbServiceRegistry()
        {
            var logRepoFactory = new Func<ILogRepo>(() =>
            {
                var getter = new Func<string>(() => new ConfigClient().GetConnectionString());

                return new LogRepo(getter);
            });

            For<ILogRepo>().Use(() => logRepoFactory());
            For<IRequestResponse>().Use<RequestResponse>();
            For<IRabbitConnectionFactory>().Use<RabbitConnectionFactory>();
            For<IQryptosArbServiceRunner>().Use<QryptosArbServiceRunner>();
            For<IQryptosArbHandler>().Use<QryptosArbHandler>();
            For<IQryptosArbServiceApp>().Use<QryptosArbServiceApp>();
            For<IEnvironmentConfigRepo>().Use<EnvironmentConfigRepo>();
            For<IQryptosArbServiceApp>().Use<QryptosArbServiceApp>();
            For<IArbWorkflowUtil>().Use<ArbWorkflowUtil>();
            For<IQryptosArbWorker>().Use<QryptosArbWorker>();
            For<IConfigClient>().Use<ConfigClient>();
            For<IExchangeClient>().Use<ExchangeClient>();
            For<IWorkflowClient>().Use<WorkflowClient>();
        }
    }
}
