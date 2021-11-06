using coin_lib.Containers;
using coin_lib.ServiceModel;
using coin_lib.ViewModel;
using System.Collections.Generic;
using cache_lib.Models;

namespace coin_lib
{
    public interface ICoinVmGenerator
    {
        CoinViewModel GenerateVm(
            string symbol,
            string baseSymbol,
            List<ExchangeContainer> exchanges,
            CachePolicy cachePolicy);

        List<TradingPairWithExchanges> GetTradingPairsWithExchanges();

        CoinViewModelsContainer GetAllOrders(List<string> filteredOutExchanges, List<string> exchangesToInclude, CachePolicy cachePolicy);

        CoinViewModel GetOrdersInternal(
            string symbol,
            string baseSymbol,
            List<string> exchanges,
            CachePolicy cachePolicy);
    }
}
