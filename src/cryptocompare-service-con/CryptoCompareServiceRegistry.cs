using client_lib;
using config_client_lib;
using config_connection_string_lib;
using cryptocompare_lib;
using cryptocompare_service_lib.App;
using cryptocompare_service_lib.Handlers;
using env_config_lib;
using log_lib;
using rabbit_lib;
using StructureMap;
using System;
using wait_for_it_lib;
using web_util;

namespace cryptocompare_service_con
{
    public class CryptoCompareServiceRegistry : Registry
    {
        public CryptoCompareServiceRegistry()
        {
            For<IEnvironmentConfigRepo>().Use<EnvironmentConfigRepo>();

            var logRepoFactory = new Func<ILogRepo>(() =>
            {
                var getter = new Func<string>(() => new ConfigClient().GetConnectionString());

                return new LogRepo(getter);
            });

            For<ILogRepo>().Use(() => logRepoFactory());

            For<IWaitForIt>().Use<WaitForIt>();
            For<IWebUtil>().Use<WebUtil>();

            For<IRabbitConnectionFactory>().Use<RabbitConnectionFactory>();
            For<IRequestResponse>().Use<RequestResponse>();

            For<IGetConnectionString>().Use<ConfigClient>();
            For<IConfigClient>().Use<ConfigClient>();

            For<ICryptoCompareIntegration>().Use<CryptoCompareIntegration>();

            For<ICryptoCompareServiceApp>().Use<CryptoCompareServiceApp>();
            For<ICryptoCompareHandler>().Use<CryptoCompareHandler>();
        }
    }
}
