using binance_lib;
using bit_z_lib;
using blocktrade_lib;
using cache_lib.Models;
using coinbase_lib;
using coss_lib;
using cryptopia_lib;
using gemini_lib;
using hitbtc_lib;
using idex_integration_lib;
using kraken_integration_lib;
using kucoin_lib;
using livecoin_lib;
using mew_integration_lib;
using oex_lib;
using qryptos_lib;
using System;
using System.Collections.Generic;
using System.Linq;
using trade_contracts;
using trade_lib;
using trade_model;
using yobit_lib;

namespace exchange_service_lib.Workflows
{
    public class ExchangeWorkflow : IExchangeWorkflow
    {
        private readonly List<ITradeIntegration> _exchanges;

        public ExchangeWorkflow(
            ICossIntegration cossIntegration,
            IBinanceIntegration binanceIntegration,
            IBitzIntegration bitzIntegration,
            IKucoinIntegration kucoinIntegration,
            IHitBtcIntegration hitBtcIntegration,
            ILivecoinIntegration livecoinIntegration,
            IKrakenIntegration krakenIntegration,
            IMewIntegration mewIntegration,
            IIdexIntegration idexIntegration,
            ICryptopiaIntegration cryptopiaIntegration,
            IYobitIntegration yobitIntegration,
            IQryptosIntegration qryptosIntegration,
            ICoinbaseIntegration coinbaseIntegration,
            IOexExchange oexExchange,
            IGeminiExchange geminiExchange,
            IBlockTradeExchange blockTradeExchange)
        {
            _exchanges = new List<ITradeIntegration>
            {
                cossIntegration,
                binanceIntegration,
                bitzIntegration,
                kucoinIntegration,
                hitBtcIntegration,
                livecoinIntegration,
                krakenIntegration,
                mewIntegration,
                idexIntegration,
                cryptopiaIntegration,
                yobitIntegration,
                qryptosIntegration,
                coinbaseIntegration,
                oexExchange,
                geminiExchange,
                blockTradeExchange
            };
        }

        public List<ExchangeContract> GetExchanges()
        {
            return _exchanges.Select(item => 
            new ExchangeContract
            {
                Id = item.Id.ToString(),
                Name = item.Name,
                HasOrderBooks = !(item is IMewIntegration),
                IsRefreshable = item is IRefreshable,
                IsWithdrawable = item is IWithdrawableTradeIntegration,
                CanBuyMarket = item is IBuyAndSellIntegration,
                CanSellMarket = item is IBuyAndSellIntegration                
            }).ToList();
        }

        // TODO: This should move over to its own root-aggregate service.
        public object GetAggregateHistory(CachePolicy cachePolicy)
        {
            throw new NotImplementedException();
        }

        public (List<HistoricalTrade> History, DateTime? AsOfUtc) GetExchangeHistory(
            ITradeIntegration exchange, 
            CachePolicy cachePolicy,
            int? limit)
        {
            if (exchange == null) { throw new ArgumentNullException(nameof(exchange)); }

            if (exchange is ITradeHistoryIntegrationV2 v2)
            {
                var historyContainer = v2.GetUserTradeHistoryV2(cachePolicy);
                var orderedHistory = historyContainer.History
                    .OrderByDescending(item => item.TimeStampUtc);

                var orderedHistoryList = (limit.HasValue && limit.Value > 0
                    ? orderedHistory.Take(limit.Value)
                    : orderedHistory).ToList();

                return (orderedHistoryList, historyContainer?.AsOfUtc);
            }

            if (!(exchange is ITradeHistoryIntegration historyExchange))
            {
                throw new ApplicationException($"Exchange integration {exchange.Name} does not provide history.");
            }

            var history = historyExchange.GetUserTradeHistory(cachePolicy);

            if (history != null)
            {
                var orderedHistory = history.OrderByDescending(item => item.TimeStampUtc);

                var orderedHistoricalTradeList = (limit.HasValue && limit.Value > 0
                    ? orderedHistory.Take(limit.Value)
                    : orderedHistory).ToList();

                return (orderedHistoricalTradeList, (DateTime?)null);
            }

            return ((List<HistoricalTrade>)null, (DateTime?)null);
        }
    }
}
