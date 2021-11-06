using coin_lib.ViewModel;
using System.Collections.Generic;
using trade_model;

namespace coin_lib.Containers
{
    public class TradingPairsForExchanges
    {
        public List<TradingPair> TradingPairs { get; set; }
        public ExchangeContainer ExchangeA { get; set; }
        public ExchangeContainer ExchangeB { get; set; }
    }
}
