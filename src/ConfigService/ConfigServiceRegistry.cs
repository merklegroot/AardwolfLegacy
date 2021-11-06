using config_lib;
using config_service_lib;
using rabbit_lib;
using config_service_lib.Handlers;
using log_lib;
using StructureMap;
using env_config_lib;
using System;
using config_connection_string_lib;

namespace config_service_con
{
    public class ConfigServiceRegistry : Registry
    {
        public ConfigServiceRegistry()
        {
            For<IEnvironmentConfigRepo>().Use<EnvironmentConfigRepo>();
            // For<IGetConnectionString>().Use<ConfigRepo>();
            For<IConfigRepo>().Use<ConfigRepo>();

            var logRepoFactory = new Func<ILogRepo>(() =>
            {
                var getter = new Func<string>(() => new ConfigRepo().GetConnectionString());
                
                return new LogRepo(getter);
            });

            For<ILogRepo>().Use(() => logRepoFactory());
            For<IRabbitConnectionFactory>().Use<RabbitConnectionFactory>();

            For<IConfigServiceApp>().Use<ConfigServiceApp>();
            For<ISimpleRequestHandler>().Use<SimpleRequestHandler>();
            For<ISetMewWalletAddressHandler>().Use<SetMewWalletAddressHandler>();
            For<IGetConnectionStringHandler>().Use<GetConnectionStringHandler>();
            For<ISetConnectionStringHandler>().Use<SetConnectionStringHandler>();
            For<IApiKeyHandler>().Use<ApiKeyHandler>();
            For<ICcxtUrlHandler>().Use<CcxtUrlHandler>();
            For<IGetBitzAgentConfigHandler>().Use<GetBitzAgentConfigHandler>();

            For<IConfigHandler>().Use<ConfigHandler>();
        }
    }
}
