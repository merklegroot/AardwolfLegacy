using browser_automation_client_lib;
using browser_lib;
using trade_ioc;

namespace mew_agent_con
{
    internal class MewAgentRegistry : DefaultRegistry
    {
        public MewAgentRegistry()
        {            
            For<IBrowserAutomationClient>().Use<BrowserAutomationClient>();
            For<IBrowserUtil>().Use<BrowserUtil>();
            For<IMewBrowser>().Use<MewBrowser>();
            For<IMewApp>().Use<MewApp>();
        }
    }
}
