using arb_workflow_lib;
using client_lib;
using config_client_lib;
using env_config_lib;
using exchange_client_lib;
using binance_arb_service_lib.App;
using binance_arb_service_lib.Workers;
using log_lib;
using rabbit_lib;
using StructureMap;
using System;
using workflow_client_lib;

namespace binance_arb_service_con
{
    public class BinanceArbServiceRegistry : Registry
    {
        public BinanceArbServiceRegistry()
        {
            var logRepoFactory = new Func<ILogRepo>(() =>
            {
                var getter = new Func<string>(() => new ConfigClient().GetConnectionString());

                return new LogRepo(getter);
            });

            For<ILogRepo>().Use(() => logRepoFactory());
            For<IRequestResponse>().Use<RequestResponse>();
            For<IRabbitConnectionFactory>().Use<RabbitConnectionFactory>();
            For<IBinanceArbServiceRunner>().Use<BinanceArbServiceRunner>();
            For<IBinanceArbServiceApp>().Use<BinanceArbServiceApp>();
            For<IEnvironmentConfigRepo>().Use<EnvironmentConfigRepo>();
            For<IBinanceArbServiceApp>().Use<BinanceArbServiceApp>();
            For<IArbWorkflowUtil>().Use<ArbWorkflowUtil>();
            For<IBinanceArbWorker>().Use<BinanceArbWorker>();
            For<IConfigClient>().Use<ConfigClient>();
            For<IExchangeClient>().Use<ExchangeClient>();
            For<IWorkflowClient>().Use<WorkflowClient>();
        }
    }
}
