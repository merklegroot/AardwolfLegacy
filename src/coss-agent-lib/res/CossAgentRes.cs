using coss_agent_lib.res;
using res_util_lib;
using System;
using System.Collections.Generic;
using System.Linq;

namespace coss_agent_lib
{
    public static class CossAgentRes
    {
        public static string WithdrawScript => ResUtil.Get("coss-withdraw.js");
        public static string SetButtonScript => ResUtil.Get("set-button.js");
        public static List<string> ArbitrageSymbols => ResUtil.Get<List<string>>("arbitrage-symbols.json", typeof(CossAgentResDummy).Assembly);

        private static List<string> _simpleBinanceSymbols = null;
        public static List<string> SimpleBinanceSymbols
        {
            get
            {
                var retriever = new Func<List<string>>(() => ResUtil.Get("simple-binance-symbols.txt", typeof(CossAgentResDummy).Assembly)
                    .Replace("\r\n", "\r").Replace("\n", "\r").Split('\r')
                    .Where(item => !string.IsNullOrWhiteSpace(item))
                    .Select(item => item.Trim())
                    .ToList());

                return _simpleBinanceSymbols ?? (_simpleBinanceSymbols = retriever());
            }
        }   
    }
}
