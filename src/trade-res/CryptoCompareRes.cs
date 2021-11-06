using res_util_lib;
using System.Collections.Generic;

namespace trade_res
{
    public static class CryptoCompareRes
    {
        public static List<string> CryptoCompareSymbols => 
            ResUtil.Get<List<string>>("cryptocompare-symbols.json", typeof(TradeResDummy).Assembly);
    }
}
