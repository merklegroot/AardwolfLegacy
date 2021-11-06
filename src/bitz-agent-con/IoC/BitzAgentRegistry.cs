using bit_z_lib;
using bitz_agent_lib.App;
using cache_lib;
using config_lib;
using trade_ioc;

namespace bitz_agent_con.IoC
{
    public class BitzAgentRegistry : DefaultRegistry
    {
        public BitzAgentRegistry()
        {
            For<IBitzAgentConfigRepo>().Use<ConfigRepo>();
            For<IBitzAgentDriver>().Use<BitzAgentDriver>();
            For<IBitzAgentApp>().Use<BitzAgentApp>();
            For<IBitzClient>().Use<BitzClient>();
            For<ICacheUtil>().Use<CacheUtil>();
        }
    }
}
