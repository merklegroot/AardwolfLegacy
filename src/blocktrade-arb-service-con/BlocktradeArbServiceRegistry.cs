using arb_workflow_lib;
using blocktrade_arb_service_lib.App;
using blocktrade_arb_service_lib.Workers;
using client_lib;
using config_client_lib;
using env_config_lib;
using exchange_client_lib;
using log_lib;
using rabbit_lib;
using StructureMap;
using System;
using workflow_client_lib;

namespace blocktrade_arb_service_con
{
    public class BlocktradeArbServiceRegistry : Registry
    {
        public BlocktradeArbServiceRegistry()
        {
            var logRepoFactory = new Func<ILogRepo>(() =>
            {
                var getter = new Func<string>(() => new ConfigClient().GetConnectionString());

                return new LogRepo(getter);
            });

            For<ILogRepo>().Use(() => logRepoFactory());
            For<IRequestResponse>().Use<RequestResponse>();
            For<IRabbitConnectionFactory>().Use<RabbitConnectionFactory>();
            For<IBlocktradeArbServiceRunner>().Use<BlocktradeArbServiceRunner>();
            For<IBlocktradeArbServiceApp>().Use<BlocktradeArbServiceApp>();
            For<IEnvironmentConfigRepo>().Use<EnvironmentConfigRepo>();
            For<IBlocktradeArbServiceApp>().Use<BlocktradeArbServiceApp>();
            For<IArbWorkflowUtil>().Use<ArbWorkflowUtil>();
            For<IBlocktradeArbWorker>().Use<BlocktradeArbWorker>();
            For<IConfigClient>().Use<ConfigClient>();
            For<IExchangeClient>().Use<ExchangeClient>();
            For<IWorkflowClient>().Use<WorkflowClient>();
        }
    }
}
