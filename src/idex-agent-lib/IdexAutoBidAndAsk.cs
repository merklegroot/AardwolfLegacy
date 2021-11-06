using idex_integration_lib.Models;
using System;
using System.Linq;
using trade_model;

namespace idex_agent_lib
{
    public class IdexAutoBidAndAsk
    {
        public class AutoBidAndAskResult
        {
            public QuantityAndPrice DesiredBid { get; set; }
            public QuantityAndPrice DesiredAsk { get; set; }
        }

        public AutoBidAndAskResult Execute(
            decimal totalTokenOwned,
            decimal totalEthAvailable,
            string myEthAddress,
            IdexExtendedOrderBook idexOrderBook,
            decimal binanceBestBid, decimal binanceBestAsk)
        {
            if (totalTokenOwned <= 0 && totalEthAvailable <= 0
                || idexOrderBook == null
                || idexOrderBook.Asks == null || !idexOrderBook.Asks.Any()
                || idexOrderBook.Bids == null || !idexOrderBook.Bids.Any()
                || binanceBestBid <= 0 || binanceBestAsk <= 0
                || binanceBestBid > binanceBestAsk)
            { return new AutoBidAndAskResult(); }

            throw new NotImplementedException();
        }
    }
}
