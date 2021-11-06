using rabbit_lib;
using System.Collections.Generic;
using trade_constants;
using config_service_lib.Handlers;
using log_lib;
using service_lib;
using service_lib.Handlers;

namespace config_service_lib
{
    public class ConfigServiceApp : ServiceApp, IConfigServiceApp
    {   
        private readonly ISimpleRequestHandler _simpleRequestHandler;
        private readonly ISetMewWalletAddressHandler _setMewWalletAddressHandler;
        private readonly IGetConnectionStringHandler _getConnectionStringHandler;
        private readonly ISetConnectionStringHandler _setConnectionStringHandler;
        private readonly IApiKeyHandler _apiKeyHandler;
        private readonly ICcxtUrlHandler _getCcxtUrlHandler;
        private readonly IGetBitzAgentConfigHandler _getBitzAgentConfigHandler;
        private readonly IConfigHandler _configHandler;

        public override string ApplicationName => "Config Service";

        protected override List<IHandler> Handlers => new List<IHandler>
        {
            _simpleRequestHandler,
            _setMewWalletAddressHandler,
            _getConnectionStringHandler,
            _setConnectionStringHandler,
            _apiKeyHandler,
            _getCcxtUrlHandler,
            _getBitzAgentConfigHandler,
            _configHandler
        };

        protected override string BaseQueueName => TradeRabbitConstants.Queues.ConfigServiceQueue;

        protected override int MaxQueueVersion => ConfigServiceConstants.Version;

        public ConfigServiceApp(
            IRabbitConnectionFactory rabbitConnectionFactory,
            ISetMewWalletAddressHandler setMewWalletAddressHandler,
            ISimpleRequestHandler simpleRequestHandler,
            IGetConnectionStringHandler getConnectionStringHandler,
            ISetConnectionStringHandler setConnectionStringHandler,
            IApiKeyHandler apiKeyHandler,
            ICcxtUrlHandler getCcxtUrlHandler,
            IGetBitzAgentConfigHandler getBitzAgentConfigHandler,
            IConfigHandler configHandler,
            ILogRepo log)
            : base(rabbitConnectionFactory, log)
        {
            _setMewWalletAddressHandler = setMewWalletAddressHandler;
            _simpleRequestHandler = simpleRequestHandler;
            _getConnectionStringHandler = getConnectionStringHandler;
            _setConnectionStringHandler = setConnectionStringHandler;
            _apiKeyHandler = apiKeyHandler;
            _getCcxtUrlHandler = getCcxtUrlHandler;
            _getBitzAgentConfigHandler = getBitzAgentConfigHandler;
            _configHandler = configHandler;
        }
    }
}