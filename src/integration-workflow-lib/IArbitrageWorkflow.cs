using cache_lib.Models;
using System.Collections.Generic;
using trade_model;
using trade_res;

namespace integration_workflow_lib
{
    public interface IArbitrageWorkflow
    {
        /// <summary>
        /// For debugging
        /// </summary>
        void SetSynchronousWorkflow(bool shouldEnable);

        ArbitrageResult Execute(string source, string destination, string symbol, CachePolicy cachePolicy);
        ArbitrageResult Execute(string source, string destination, Commodity commodity, CachePolicy cachePolicy);
        ArbitrageResult Execute(string source, string destination, string symbol, Dictionary<string, decimal> valuations, CachePolicy cachePolicy);
    }
}
