using cache_lib.Models;
using System;
using System.Collections.Generic;
using trade_contracts;
using trade_lib;
using trade_model;

namespace exchange_service_lib.Workflows
{
    public interface IExchangeWorkflow
    {
        List<ExchangeContract> GetExchanges();

        (List<HistoricalTrade> History, DateTime? AsOfUtc) GetExchangeHistory(
            ITradeIntegration exchange,
            CachePolicy cachePolicy,
            int? limit);
    }
}
