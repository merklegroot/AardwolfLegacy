using config_lib;
using service_lib.Handlers;
using trade_contracts;
using trade_contracts.Messages.Config;

namespace config_service_lib.Handlers
{
    public interface IGetBitzAgentConfigHandler : IRequestResponseHandler<GetBitzAgentConfigRequestMessage, GetBitzAgentConfigResponseMessage> { }
    public class GetBitzAgentConfigHandler : IGetBitzAgentConfigHandler
    {
        public readonly IConfigRepo _configRepo;

        public GetBitzAgentConfigHandler(IConfigRepo configRepo)
        {
            _configRepo = configRepo;
        }

        public GetBitzAgentConfigResponseMessage Handle(GetBitzAgentConfigRequestMessage message)
        {
            var bitzAgentConfig = _configRepo.GetBitzAgentConfig();
            var configContract = bitzAgentConfig != null 
                ? new AgentConfigContract
                {
                    EthThreshold = bitzAgentConfig.EthThreshold,
                    IsAutoTradingEnabled = bitzAgentConfig.IsAutoTradingEnabled,
                    TokenThreshold = bitzAgentConfig.TokenThreshold
                }
                : null;

            return new GetBitzAgentConfigResponseMessage
            {
                BitzAgentConfig = configContract
            };
        }
    }
}
