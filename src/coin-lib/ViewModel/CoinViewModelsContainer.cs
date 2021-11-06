using System;
using System.Collections.Generic;

namespace coin_lib.ViewModel
{
    public class CoinViewModelsContainer
    {
        public DateTime TimeStampUtc { get; set; } = DateTime.UtcNow;
        public List<CoinViewModel> Coins { get; set; } = new List<CoinViewModel>();
    }
}
