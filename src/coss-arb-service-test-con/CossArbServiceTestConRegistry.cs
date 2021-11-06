using coss_browser_service_client;
using env_config_lib;
using client_lib;
using log_lib;
using rabbit_lib;
using StructureMap;
using System;
using config_client_lib;
using exchange_client_lib;

namespace coss_arb_service_test_con
{
    public class CossArbServiceTestConRegistry : Registry
    {
        public CossArbServiceTestConRegistry()
        {
            var logRepoFactory = new Func<ILogRepo>(() =>
            {
                var getter = new Func<string>(() => new ConfigClient().GetConnectionString());

                return new LogRepo(getter);
            });

            For<ILogRepo>().Use(() => logRepoFactory());

            For<IEnvironmentConfigRepo>().Use<EnvironmentConfigRepo>();
            
            For<IRequestResponse>().Use<RequestResponse>();
            For<IRabbitConnectionFactory>().Use<RabbitConnectionFactory>();

            For<ICossBrowserClient>().Use<CossBrowserClient>();
            For<IExchangeClient>().Use<ExchangeClient>();
            For<IConfigClient>().Use<ConfigClient>();

            For<ICossArbServiceTestConApp>().Use<CossArbServiceTestConApp>();
        }
    }
}
