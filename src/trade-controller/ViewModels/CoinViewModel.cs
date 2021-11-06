using System;
using System.Collections.Generic;

namespace trade_web.ViewModels
{
    public class CoinViewModel
    {
        public string Symbol { get; set; }
        public string BaseSymbol { get; set; }

        public ExchangeViewModel MajorExchange { get; set; } = new ExchangeViewModel();
        public ExchangeViewModel MinorExchange { get; set; } = new ExchangeViewModel();

        public string MajorExchangeWithdrawlFee { get; set; }

        [Obsolete]
        public string BinanceWithdrawlFee { get { return MajorExchangeWithdrawlFee; } }

        public string MinorExchangeWithdrawlFee { get; set; }

        [Obsolete]
        public string CossWithdrawlFee { get { return MinorExchangeWithdrawlFee; } }

        public string BinanceBidPrice { get; set; }
        public string BinanceBidQuantity { get; set; }
        public string BinanceAskPrice { get; set; }
        public string BinanceAskQuantity { get; set; }

        public string CossBidPrice { get; set; }
        public string CossBidQuantity { get; set; }
        public string CossAskPrice { get; set; }
        public string CossAskQuantity { get; set; }

        public List<OrderViewModel> CossBids { get; set; }
        public List<OrderViewModel> BinanceBids { get; set; }
        public List<OrderViewModel> CossAsks { get; set; }
        public List<OrderViewModel> BinanceAsks { get; set; }

        public string CossToBinanceProfit { get; set; }
        public decimal? CossToBinanceProfitPercentage { get; set; }
        public string CossToBinanceProfitPercentageDisplayText { get; set; }
        public string CossToBinanceBreakEvenQuantity { get; set; }

        public string BinanceToCossProfit { get; set; }
        public decimal? BinanceToCossProfitPercentage { get; set; }
        public string BinanceToCossProfitPercentageDisplayText { get; set; }
        public string BinanceToCossBreakEvenQuantity { get; set; }
    }
}