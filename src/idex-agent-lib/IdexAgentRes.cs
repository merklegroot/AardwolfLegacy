using res_util_lib;
using System.Collections.Generic;

namespace idex_agent_lib
{
    public class IdexAgentRes
    {
        public static List<string> BinanceIntersection
            => ResUtil.Get<List<string>>("idex-binance-symbols", typeof(IdexAgentRes).Assembly);

        public static Dictionary<string, List<string>> NonBinanceIntersections
            => ResUtil.Get<Dictionary<string, List<string>>>("idex-non-binance-symbols", typeof(IdexAgentRes).Assembly);
    }
}