using System;
using System.Collections.Generic;

namespace coin_lib.ViewModel
{
    public class CoinExchangeViewModel
    {
        public string Name { get; set; }
        public Guid? CommodityCanonicalId { get; set; }
        public string NativeSymbol { get; set; }
        public string CommodityName { get; set; }
        public bool? CanDeposit { get; set; }
        public bool? CanWithdraw { get; set; }
        public List<KeyValuePair<string, string>> CustomValues { get; set; }

        public string WithdrawalFee { get; set; }

        public string BidPrice { get; set; }
        public string BidQuantity { get; set; }

        public string AskPrice { get; set; }
        public string AskQuantity { get; set; }

        public DateTime? OrderBookAsOf { get; set; }
        public List<OrderViewModel> Bids { get; set; }
        public List<OrderViewModel> Asks { get; set; }

        public string Profit { get; set; }
        public decimal? ProfitPercentage { get; set; }
        public string ProfitPercentageDisplayText { get; set; }
        public string BreakEvenQuantity { get; set; }
    }
}
