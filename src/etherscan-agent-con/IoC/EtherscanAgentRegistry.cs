using browser_lib;
using config_client_lib;
using etherscan_agent_lib;
using trade_ioc;

namespace etherscan_agent_con
{
    public class EtherscanAgentRegistry : DefaultRegistry
    {
        public EtherscanAgentRegistry()
        {
            For<IConfigClient>().Use<ConfigClient>();
            For<IBrowserUtil>().Use<BrowserUtil>();
            For<IEtherscanAgentApp>().Use<EtherscanAgentApp>();
        }
    }
}
