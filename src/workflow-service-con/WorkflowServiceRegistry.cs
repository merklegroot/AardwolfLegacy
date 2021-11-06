using config_connection_string_lib;
using cryptocompare_client_lib;
using env_config_lib;
using integration_workflow_lib;
using client_lib;
using log_lib;
using rabbit_lib;
using StructureMap;
using System;
using web_util;
using workflow_service_lib.App;
using workflow_service_lib.Handlers;
using exchange_client_lib;
using config_client_lib;
using currency_converter_lib;
using cache_lib;

namespace workflow_service_con
{
    public class WorkflowServiceRegistry : Registry
    {
        public WorkflowServiceRegistry()
        {
            For<IEnvironmentConfigRepo>().Use<EnvironmentConfigRepo>();

            var logRepoFactory = new Func<ILogRepo>(() =>
            {
                var getter = new Func<string>(() => new ConfigClient().GetConnectionString());
                return new LogRepo(getter);
            });

            For<ILogRepo>().Use(() => logRepoFactory());

            For<IWebUtil>().Use<WebUtil>();
            For<IServiceInvoker>().Use<ServiceInvoker>();
            For<IRabbitConnectionFactory>().Use<RabbitConnectionFactory>();
            For<IRequestResponse>().Use<RequestResponse>();
            For<IGetConnectionString>().Use<ConfigClient>();

            For<IConfigClient>().Use<ConfigClient>();
            For<IExchangeClient>().Use<ExchangeClient>();
            For<ICryptoCompareClient>().Use<CryptoCompareClient>();

            For<IArbitrageWorkflow>().Use<ArbitrageWorkflow>();
            For<IWorkflowServiceApp>().Use<WorkflowServiceApp>();

            For<IWorkflowHandler>().Use<WorkflowHandler>();
            For<IValuationWorkflow>().Use<ValuationWorkflow>();

            For<ICacheUtil>().Use<CacheUtil>();
            For<ICurrencyConverterClient>().Use<CurrencyConverterClient>();
            For<ICurrencyConverterIntegration>().Use<CurrencyConverterIntegration>();           
        }
    }
}
