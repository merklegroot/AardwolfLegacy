using System;

namespace etherscan_agent_lib
{
    public interface IEtherscanAgentApp : IDisposable
    {
        void Run();
    }
}
