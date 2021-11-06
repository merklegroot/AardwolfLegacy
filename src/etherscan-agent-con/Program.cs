using etherscan_agent_lib;
using StructureMap;

namespace etherscan_agent_con
{
    class Program
    {
        static void Main(string[] args)
        {
            var container = Container.For<EtherscanAgentRegistry>();
            using (var app = container.GetInstance<IEtherscanAgentApp>())
            {
                app.Run();
                //(app as EtherscanAgentApp).RunTest();
            }
        }
    }
}
