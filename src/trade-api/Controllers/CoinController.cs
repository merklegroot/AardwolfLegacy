using System.Net;
using System.Net.Http;
using System.Web.Http;
using coin_lib;
using coin_lib.ServiceModel;
using cache_lib.Models;
using System;
using System.Collections.Generic;
using trade_api.Utils;
using exchange_client_lib;
using trade_res;
using System.Linq;
using task_lib;
using log_lib;
using System.Diagnostics;
using linq_lib;

namespace trade_api.Controllers
{
    public class CoinController : ApiController
    {
        private readonly IExchangeClient _exchangeClient;
        private readonly ICoinVmGenerator _coinVmGenerator;
        private readonly ILogRepo _log;

        public CoinController(
            IExchangeClient exchangeClient,
            ICoinVmGenerator coinVmGenerator,
            ILogRepo log)
        {
            _exchangeClient = exchangeClient;
            _coinVmGenerator = coinVmGenerator;
            _log = log;
        }     

        [HttpGet]
        [HttpPost]
        [Route("api/get-trading-pairs")]
        public HttpResponseMessage GetTradingPairsApi()
        {
            var vm = _coinVmGenerator.GetTradingPairsWithExchanges();
            return Request.CreateResponse(HttpStatusCode.OK, vm);
        }

        [HttpPost]
        [Route("api/get-orders")]
        public HttpResponseMessage GetOrders(GetOrdersServiceModel serviceModel)
        {
            try
            {
                var cachePolicy = CachePolicyParser.ParseCachePolicy(serviceModel.CachePolicy, CachePolicy.ForceRefresh);
                var exchanges = new List<string> { serviceModel.ExchangeA, serviceModel.ExchangeB };
                var viewModel = _coinVmGenerator.GetOrdersInternal(serviceModel.Symbol, serviceModel.BaseSymbol, exchanges, cachePolicy);
                return Request.CreateResponse(HttpStatusCode.OK, viewModel);
            }
            catch (Exception exception)
            {
                try
                {
                    _log.Error($"Failed to get comp for exchanges {serviceModel.ExchangeA}, {serviceModel.ExchangeB} tradingPair {serviceModel.Symbol}-{serviceModel.BaseSymbol} with cachePolicy {serviceModel.CachePolicy}");
                } catch { }

                _log.Error(exception);

                throw;
            }
        }

        [HttpPost]
        [Route("api/get-all-orders")]
        public HttpResponseMessage GetAllOrdersEntryPoint(GetAllOrdersServiceModel serviceModel)
        {
            var cachePolicy = serviceModel.ForceRefresh ? CachePolicy.ForceRefresh : CachePolicy.OnlyUseCache;
            var vm = _coinVmGenerator.GetAllOrders(serviceModel.FilteredOutExchanges, serviceModel.ExchangesToInclude, cachePolicy);

            return Request.CreateResponse(HttpStatusCode.OK, vm);
        }

        internal class Comp
        {
            public List<string> Exchanges { get; set; }
            public string Symbol { get; set; }
            public string BaseSymbol { get; set; }
        }

        [HttpPost]
        [Route("api/coin/get-comps")]
        public HttpResponseMessage GetComps()
        {
            var exchanges = new List<string> {
                ExchangeNameRes.Qryptos,
                ExchangeNameRes.Coss,
                ExchangeNameRes.Binance,
                ExchangeNameRes.KuCoin,                
                ExchangeNameRes.HitBtc,
                ExchangeNameRes.Bitz,
                ExchangeNameRes.Livecoin
            };
            
            var cachePolicy = CachePolicy.OnlyUseCacheUnlessEmpty;
            var tradingPairTasks = exchanges.Select(queryExchange => LongRunningTask.Run(() =>
                Time(() => _exchangeClient.GetTradingPairs(queryExchange, CachePolicy.OnlyUseCacheUnlessEmpty),
                $"Get {queryExchange} trading pairs {cachePolicy}"))               
                )
                .ToList();

            foreach (var task in tradingPairTasks)
            {
                task.Wait();
            }

            var allComps = new List<Comp>();

            for (var i = 0; i < exchanges.Count; i++)
            {
                for (var j = i + 1; j < exchanges.Count; j++)
                {
                    var exchangeGroup = new List<string> { exchanges[i], exchanges[j] };

                    // Don't compare Binance to KuCoin
                    if (exchangeGroup.Any(queryExchange => string.Equals( queryExchange, ExchangeNameRes.Binance, StringComparison.InvariantCultureIgnoreCase))
                        && exchangeGroup.Any(queryExchange => string.Equals(queryExchange, ExchangeNameRes.KuCoin, StringComparison.InvariantCultureIgnoreCase)))
                    {
                        continue;
                    }

                    // Don't compare Binance to HitBTC
                    if (exchangeGroup.Any(queryExchange => string.Equals(queryExchange, ExchangeNameRes.Binance, StringComparison.InvariantCultureIgnoreCase))
                        && exchangeGroup.Any(queryExchange => string.Equals(queryExchange, ExchangeNameRes.HitBtc, StringComparison.InvariantCultureIgnoreCase)))
                    {
                        continue;
                    }

                    var comps = tradingPairTasks[i].Result
                        .Where(queryTpA => tradingPairTasks[j].Result.Any(queryTpB =>
                        {
                            return CoinVmGenerator.DoTradingPairsMatch(queryTpA, queryTpB);
                        }))
                        .Select(item =>
                        {
                            return new Comp
                            {
                                Exchanges = exchangeGroup,
                                Symbol = item.Symbol,
                                BaseSymbol = item.BaseSymbol
                            };
                        })
                        // .Take(10)
                        .ToList();

                    allComps.AddRange(comps);
                }
            }

            //var qryptosComps = allComps.Where(queryComp => 
            //    queryComp.Exchanges.Any(queryExchange => string.Equals(queryExchange, ExchangeNameRes.Qryptos, StringComparison.InvariantCultureIgnoreCase)))
            //    .ToList();

            //return Request.CreateResponse(qryptosComps);

            var shuffledComps = allComps.Shuffle().ToList();

            // return Request.CreateResponse(allComps);
            return Request.CreateResponse(shuffledComps);
        }

        private T Time<T>(Func<T> method, string desc)
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            try
            {
                return method();
            }
            finally
            {
                stopWatch.Stop();
                _log.Info($"{desc} took {stopWatch.ElapsedMilliseconds} ms");
            }
        }
    }
}
