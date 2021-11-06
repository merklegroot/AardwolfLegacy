using cache_lib.Models;
using exchange_client_lib;
using System;
using System.Collections.Generic;
using System.Linq;
using trade_res;
using trade_strategy_lib;
using workflow_client_lib;

namespace coss_agent_lib.Strategy
{
    public class CossAutoOpenAsk
    {
        private readonly IExchangeClient _exchangeClient;
        private readonly IWorkflowClient _workflowClient;

        public CossAutoOpenAsk(
            IExchangeClient exchangeClient,
            IWorkflowClient workflowClient)
        {
            _exchangeClient = exchangeClient;
            _workflowClient = workflowClient;
        }

        public void ExecuteLisk()
        {
            var filteredOutBaseSymbols = new List<string>
            {
                "USD",
                // TODO: Add COSS back in as a base symbol after doing a bit more math...
                "COSS"
            };

            var symbol = "LSK";

            var cossTradingPairs = _exchangeClient.GetTradingPairs(ExchangeNameRes.Coss, CachePolicy.AllowCache);
            var binanceTradingPairs = _exchangeClient.GetTradingPairs(ExchangeNameRes.Binance, CachePolicy.AllowCache);

            var cossLiskPairs = cossTradingPairs.Where(item => 
                string.Equals(item.Symbol, symbol, StringComparison.InvariantCultureIgnoreCase)
                && !string.Equals(item.BaseSymbol, "USD", StringComparison.InvariantCultureIgnoreCase)).ToList();
            var binanceLiskPairs = binanceTradingPairs.Where(item => string.Equals(item.Symbol, symbol, StringComparison.InvariantCultureIgnoreCase)
                && !string.Equals(item.BaseSymbol, "USD", StringComparison.InvariantCultureIgnoreCase)).ToList();
            //_exchangeClient.GetOrderBook(ExchangeNameRes.Coss, "LSK", )

            var autoOpenAsk = new AutoOpenAsk();
            
            

            foreach (var cossPair in cossLiskPairs)
            {
                var binancePair = binanceLiskPairs.Where(item => string.Equals(item.BaseSymbol, cossPair.BaseSymbol, StringComparison.InvariantCultureIgnoreCase))
                    .SingleOrDefault();

                if (binancePair == null) { continue; }
                // autoOpenAsk.ExecuteAgainstHighVolumeExchange()
            }
        }
    }
}
