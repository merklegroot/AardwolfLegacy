using System.Collections.Generic;

namespace coin_lib.ViewModel
{
    public class CoinViewModel
    {
        public string Symbol { get; set; }
        public string BaseSymbol { get; set; }

        public List<CoinExchangeViewModel> Exchanges { get; set; }
    }
}
