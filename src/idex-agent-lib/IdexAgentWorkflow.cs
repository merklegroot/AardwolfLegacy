using cache_lib.Models;
using config_client_lib;
using idex_integration_lib;
using idex_integration_lib.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using trade_model;

namespace idex_agent_lib
{
    public class IdexAgentWorkflow
    {
        private readonly IConfigClient _configClient;

        private IIdexIntegration _idexIntegration;
        public IdexAgentWorkflow(
            IConfigClient configClient,
            IIdexIntegration idexIntegration)
        {
            _configClient = configClient;

            _idexIntegration = idexIntegration;
            _idexIntegration.UseRelay = true;
        }

        public void Execute()
        {
            var idexCachePolicy = CachePolicy.ForceRefresh;

            var walletAddress = _configClient.GetMewWalletAddress();

            var openOrders = _idexIntegration.GetOpenOrders(CachePolicy.ForceRefresh);
            var holdings = _idexIntegration.GetHoldings(idexCachePolicy);           

            var ordersInNeedOfSomeFixin = new List<OpenOrderForTradingPair>();
            for (var i = 0; i < openOrders.Count; i++)
            {
                var openOrder = openOrders[i];

                var tradingPair = new TradingPair(openOrder.Symbol, openOrder.BaseSymbol);
                var orderBook = _idexIntegration.GetExtendedOrderBook(tradingPair, idexCachePolicy);
                var myBids = orderBook.Bids?.Where(item => string.Equals(item.User, walletAddress, StringComparison.InvariantCultureIgnoreCase))
                    .ToList() ?? new List<IdexExtendedOrder>();

                var myAsks = orderBook.Asks?.Where(item => string.Equals(item.User, walletAddress, StringComparison.InvariantCultureIgnoreCase))
                    .ToList() ?? new List<IdexExtendedOrder>();

                var bestBid = orderBook.Bids?.OrderByDescending(queryBid => queryBid.Price).FirstOrDefault();
                var myBadBids = bestBid != null ? myBids.Where(myBid => 
                    !string.Equals(myBid.Hash, bestBid.Hash, StringComparison.InvariantCultureIgnoreCase)
                    && myBid.Price > bestBid.Price).ToList() : new List<IdexExtendedOrder>();
                if (myBadBids.Any())
                {                   
                    Console.WriteLine($"We've been outbid on {openOrder.Symbol}");
                }

                var bestAsk = orderBook.Asks?.OrderBy(queryBid => queryBid.Price).FirstOrDefault();
                var myBadAsks = bestAsk != null ? myAsks.Where(myAsk => 
                    !string.Equals(myAsk.Hash, bestAsk.Hash, StringComparison.InvariantCultureIgnoreCase)
                    && myAsk.Price < bestAsk.Price).ToList() : new List<IdexExtendedOrder>();

                if (myBadAsks.Any())
                {                    
                    Console.WriteLine($"We've been outasked on {openOrder.Symbol}");
                }

                if(myBadBids.Any() || myBadAsks.Any())
                {
                    ordersInNeedOfSomeFixin.Add(openOrder);
                }

            }

            if (ordersInNeedOfSomeFixin.Any())
            {
                var isOrAre = ordersInNeedOfSomeFixin.Count == 1 ? "is" : "are";
                var needOrNeeds = ordersInNeedOfSomeFixin.Count == 1 ? "needs" : "need";
                Console.WriteLine($"There {isOrAre} {ordersInNeedOfSomeFixin.Count} orders that {needOrNeeds} your attention.");
            }
            else
            {
                Console.WriteLine("All open orders are good.");
            }
        }
    }
}
