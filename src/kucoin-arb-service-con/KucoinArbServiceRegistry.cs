using arb_workflow_lib;
using client_lib;
using config_client_lib;
using env_config_lib;
using exchange_client_lib;
using log_lib;
using kucoin_arb_service_lib.App;
using kucoin_arb_service_lib.Handlers;
using kucoin_arb_service_lib.Workers;
using rabbit_lib;
using StructureMap;
using System;
using workflow_client_lib;

namespace kucoin_arb_service_con
{
    internal class KucoinArbServiceRegistry : Registry
    {
        public KucoinArbServiceRegistry()
        {
            var logRepoFactory = new Func<ILogRepo>(() =>
            {
                var getter = new Func<string>(() => new ConfigClient().GetConnectionString());

                return new LogRepo(getter);
            });

            For<ILogRepo>().Use(() => logRepoFactory());
            For<IRequestResponse>().Use<RequestResponse>();
            For<IRabbitConnectionFactory>().Use<RabbitConnectionFactory>();
            For<IKucoinArbServiceRunner>().Use<KucoinArbServiceRunner>();
            For<IKucoinArbHandler>().Use<KucoinArbHandler>();
            For<IKucoinArbServiceApp>().Use<KucoinArbServiceApp>();
            For<IEnvironmentConfigRepo>().Use<EnvironmentConfigRepo>();
            For<IKucoinArbServiceApp>().Use<KucoinArbServiceApp>();
            For<IArbWorkflowUtil>().Use<ArbWorkflowUtil>();
            For<IKucoinArbWorker>().Use<KucoinArbWorker>();
            For<IConfigClient>().Use<ConfigClient>();
            For<IExchangeClient>().Use<ExchangeClient>();
            For<IWorkflowClient>().Use<WorkflowClient>();
        }
    }
}
