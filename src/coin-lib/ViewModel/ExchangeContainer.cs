using System.Collections.Generic;
using System.Threading.Tasks;
using trade_contracts;
using trade_model;

namespace coin_lib.ViewModel
{
    public class ExchangeContainer
    {
        public string Exchange { get; set; }

        //public Task<Dictionary<string, decimal>> WithdrawalFeesTask { get; set; }
        public Dictionary<string, decimal> WithdrawalFees { get; set; }

        //public Task<List<ExchangeCommodityContract>> CommoditiesTask { get; set; }
        public List<CommodityForExchange> Commodities { get; set; }

        //public Task<List<TradingPairContract>> TradingPairsTask { get; set; }
        public List<TradingPair> TradingPairs { get; set; }

        public override string ToString()
        {
            return Exchange;
        }
    }
}
