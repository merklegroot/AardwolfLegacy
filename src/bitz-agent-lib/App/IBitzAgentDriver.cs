using System.Collections.Generic;

namespace bitz_agent_lib.App
{
    public interface IBitzAgentDriver
    {
        void AutoOpenOrder();
        void AutoOpenOrder(List<string> limitCommodities);
    }
}
