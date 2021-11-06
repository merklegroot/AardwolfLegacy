//using log_lib;
//using parse_lib;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading.Tasks;
//using trade_constants;
//using trade_lib;
//using trade_model;
//using trade_res;

//namespace integration_workflow_lib
//{
//    public class CommodityDetailsWorkflow
//    {
//        private readonly ILogRepo _log;

//        public CommodityDetailsWorkflow(ILogRepo log)
//        {
//            _log = log;
//        }

//        public dynamic GetCommodityDetails(GetCommodityDetailsServiceModel serviceModel)
//        {
//            if (serviceModel == null) { throw new ArgumentNullException(nameof(serviceModel)); }
//            if (string.IsNullOrWhiteSpace(serviceModel.Symbol)) { throw new ArgumentNullException(nameof(serviceModel.Symbol)); }

//            var items = new List<(ITradeIntegration exchange, Task<List<CommodityForExchange>> commoditiesTask, Task<List<TradingPair>> tradingPairsTask)>();
//            foreach (var exchange in ExchangeConstants.Exchanges)
//            {
//                var commoditiesTask = new Task<List<CommodityForExchange>>(() => exchange.GetCommodities(CachePolicy.OnlyUseCache), TaskCreationOptions.LongRunning);
//                commoditiesTask.Start();

//                var tradingPairsTask = new Task<List<TradingPair>>(() => exchange.GetTradingPairs(CachePolicy.OnlyUseCache), TaskCreationOptions.LongRunning);

//                items.Add((exchange, commoditiesTask, tradingPairsTask));
//            }

//            foreach (var item in items)
//            {
//                var currentExchange = item.exchange.Name;
//                Console.WriteLine($"Starting on {currentExchange}.");

//                try
//                {
//                    item.commoditiesTask.Wait();
//                    item.tradingPairsTask.Wait();
//                }
//                catch (Exception exception)
//                {
//                    _log.Error(exception);
//                    throw;
//                }
//            }

//            Commodity canon = null;
//            List<Commodity> canons = null;

//            var parsedId = ParseUtil.GuidTryParse(serviceModel.Symbol);
//            if (parsedId.HasValue && parsedId.Value != default(Guid))
//            {
//                canon = CommodityRes.ById(parsedId.Value);
//                if (canon == null) { throw new ApplicationException($"Failed to resolve canon by id {parsedId.Value}."); }
//                canons = new List<Commodity> { canon };
//            }
//            else
//            {
//                canons = CommodityRes.BySymbolAllowMultiple(serviceModel.Symbol);
//                canon = canons.OrderByDescending(item => item.IsDominant).FirstOrDefault();
//            }

//            var matchingExchanges = new List<(string exchangeName, List<string> baseCommodities)>();
//            foreach (var item in items)
//            {
//                try
//                {
//                    var result = item.commoditiesTask.Result;
//                    if (result != null && result.Any(queryCommodity =>
//                    {
//                        if (queryCommodity == null) { return false; }
//                        if (queryCommodity.CanonicalId.HasValue && canon != null && canon.Id != default(Guid))
//                        {
//                            return queryCommodity.CanonicalId == canon.Id;
//                        }

//                        return string.Equals(serviceModel.Symbol, queryCommodity.Symbol, StringComparison.InvariantCultureIgnoreCase);
//                    }))
//                    {
//                        var baseSymbols =
//                            (item.tradingPairsTask.Result ?? new List<TradingPair>()).Where(queryTradingPair => string.Equals(queryTradingPair.Symbol, serviceModel.Symbol, StringComparison.InvariantCultureIgnoreCase))
//                            .Select(queryTradingPair => queryTradingPair.BaseSymbol)
//                            .ToList();

//                        matchingExchanges.Add((item.exchange.Name, baseSymbols));
//                    }
//                }
//                catch (Exception exception)
//                {
//                    _log.Error(exception);
//                }
//            }


//            var recessiveCanons = canons.Where(item => item.Id != canon.Id).ToList();

//            var vm = new
//            {
//                id = canon?.Id,
//                recessives = recessiveCanons,
//                canonicalName = canon?.Name,
//                symbol = serviceModel.Symbol,
//                exchanges = matchingExchanges.Select(item => new
//                {
//                    name = item
//                }).ToList()
//            };

//            return vm;
//        }
//    }
//}
