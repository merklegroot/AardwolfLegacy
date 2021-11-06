using System;
using System.Collections.Generic;
using web_util;

namespace oex_lib.Client
{
    public interface IOexClient
    {
        string GetOrderBookRaw(int tradingPairId);
        string GetTradeMarketSource();
    }

    public class OexClient : IOexClient
    {
        private readonly IWebUtil _webUtil;

        public OexClient(IWebUtil webUtil)
        {
            _webUtil = webUtil;
        }

        public string GetOrderBookRaw(int tradingPairId)
        {
            // https://oex.top/real/market.html?symbol=146&buysellcount=5&successcount=3&_t=75

            var url = $" https://oex.top/real/market.html?symbol={tradingPairId}&buysellcount=5&successcount=3&_t=75";
            var contents = _webUtil.Get(url);

            return contents;
        }

        public string GetTradeMarketSource()
        {
            const string Url = "https://www.oex.com/trademarket.html";
            return _webUtil.Get(Url);
        }

        private static Dictionary<string, int> TradingPairDictionary = new Dictionary<string, int>(StringComparer.InvariantCultureIgnoreCase)
        {
            { "PGT_BTC", 146 }
        };

        private int? GetTradingPairId(string symbol, string baseSymbol)
        {
            var key = $"{symbol.ToUpper()}_{baseSymbol.ToUpper()}";

            return TradingPairDictionary.ContainsKey(key)
                ? TradingPairDictionary[key]
                : (int?)null;
        }
    }
}
