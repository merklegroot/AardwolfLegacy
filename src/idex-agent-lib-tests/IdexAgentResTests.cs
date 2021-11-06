using dump_lib;
using idex_agent_lib;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace idex_agent_lib_tests
{
    [TestClass]
    public class IdexAgentResTests
    {
        [TestMethod]
        public void IdexAgentRes__non_binance_intersections()
        {
            var results = IdexAgentRes.NonBinanceIntersections;
            results.Dump();
        }
    }
}
