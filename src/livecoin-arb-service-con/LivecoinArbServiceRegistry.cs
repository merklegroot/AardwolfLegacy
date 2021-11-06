using arb_workflow_lib;
using client_lib;
using config_client_lib;
using env_config_lib;
using exchange_client_lib;
using livecoin_arb_service_lib.App;
using livecoin_arb_service_lib.Workers;
using log_lib;
using rabbit_lib;
using StructureMap;
using System;
using workflow_client_lib;

namespace livecoin_arb_service_con
{
    public class LivecoinArbServiceRegistry : Registry
    {
        public LivecoinArbServiceRegistry()
        {
            var logRepoFactory = new Func<ILogRepo>(() =>
            {
                var getter = new Func<string>(() => new ConfigClient().GetConnectionString());

                return new LogRepo(getter);
            });

            For<ILogRepo>().Use(() => logRepoFactory());
            For<IRequestResponse>().Use<RequestResponse>();
            For<IRabbitConnectionFactory>().Use<RabbitConnectionFactory>();
            For<ILivecoinArbServiceRunner>().Use<LivecoinArbServiceRunner>();
            For<ILivecoinArbServiceApp>().Use<LivecoinArbServiceApp>();
            For<IEnvironmentConfigRepo>().Use<EnvironmentConfigRepo>();
            For<IArbWorkflowUtil>().Use<ArbWorkflowUtil>();
            For<ILivecoinArbWorker>().Use<LivecoinArbWorker>();
            For<IConfigClient>().Use<ConfigClient>();
            For<IExchangeClient>().Use<ExchangeClient>();
            For<IWorkflowClient>().Use<WorkflowClient>();
        }
    }
}
