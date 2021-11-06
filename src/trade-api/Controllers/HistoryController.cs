using cache_lib.Models;
using log_lib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Web.Http;
using trade_api.Utils;
using trade_api.ViewModels;
using trade_lib;
using exchange_client_lib;

namespace trade_api.Controllers
{
    public class HistoryController : ApiController
    {
        private readonly IExchangeClient _exchangeClient;

        private readonly ILogRepo _log;

        public HistoryController(
            IExchangeClient exchangeClient,
            ILogRepo log)
        {
            _exchangeClient = exchangeClient;

            _log = log;
        }

        public class RefreshHistoryServiceModel
        {
            public string Exchange { get; set; }
        }

        private static object SlimLocker = new object();
        private static Dictionary<string, ManualResetEventSlim> _slims = new Dictionary<string, ManualResetEventSlim>(StringComparer.InvariantCultureIgnoreCase);

        [HttpPost]
        [Route("api/get-history-for-exchange")]
        public HttpResponseMessage GetHistoryForExchange(GetHistoryForExchangeServiceModel serviceModel)
        {   
            if (serviceModel == null) { throw new ArgumentNullException(nameof(serviceModel)); }

            var cachePolicy = CachePolicyParser.ParseCachePolicy(serviceModel.CachePolicy, serviceModel.ForceRefresh, CachePolicy.OnlyUseCache);

            if (string.Equals(serviceModel.Exchange, "aggregate", StringComparison.InvariantCultureIgnoreCase))
            {
                var aggregateResults = _exchangeClient.GetAggregateHistory(serviceModel.Limit, cachePolicy);

                DateTime? asOfUtc = null;
                if (aggregateResults?.AsOfUtcByExchange?.Keys != null)
                {
                    foreach (var key in aggregateResults.AsOfUtcByExchange.Keys)
                    {
                        var individualAsOfUtc = aggregateResults.AsOfUtcByExchange[key];
                        if (!individualAsOfUtc.HasValue)
                        {
                            asOfUtc = null;
                            continue;
                        }

                        if (!asOfUtc.HasValue) { asOfUtc = individualAsOfUtc.Value; }
                        if (individualAsOfUtc.Value < asOfUtc.Value)
                        {
                            asOfUtc = individualAsOfUtc.Value;
                        }
                    }
                }

                var aggregateViewModels = new List<HistoricalTradeViewModel>();
                foreach(var aggregateHistoryItem in aggregateResults?.History)
                {
                    var vm = HistoricalTradeViewModel.FromModel(aggregateHistoryItem, serviceModel.Exchange);
                    vm.Exchange = aggregateHistoryItem.Exchange;
                    aggregateViewModels.Add(vm);
                }
                var aggregateContainer = new
                {
                    AsOfUtc = asOfUtc,
                    AsOfUtcByExchange = aggregateResults.AsOfUtcByExchange,
                    HistoryItems = aggregateViewModels
                };

                return Request.CreateResponse(aggregateContainer);
            }

            const int HistoryLimit = 250;
            var results = _exchangeClient.GetExchangeHistory(serviceModel.Exchange, HistoryLimit, cachePolicy);

            var historyViewModels = results?.History != null
                ? results.History.Select(item => 
                    HistoricalTradeViewModel.FromModel(item, serviceModel.Exchange))
                    .ToList()
                : null;

            var container = new
            {
                AsOfUtc = results?.AsOfUtc,
                HistoryItems = historyViewModels
            };

            return Request.CreateResponse(container);
        }

        [HttpPost]
        [Route("api/refresh-history")]
        public HttpResponseMessage RefreshHistory(RefreshHistoryServiceModel serviceModel)
        {
            throw new NotImplementedException();

            //try
            //{
            //    if (serviceModel == null) { throw new ArgumentNullException(nameof(serviceModel)); }
            //    var exchange = GetExchangeFromName(serviceModel.Exchange);

            //    var alreadyRunningText = $"The refresh history process for {exchange.Name} is already running. Please be patient.";
            //    if (_slims.ContainsKey(exchange.Name) && !_slims[exchange.Name].IsSet)
            //    {
            //        return Request.CreateResponse(HttpStatusCode.OK, alreadyRunningText);
            //    }

            //    bool gotSlim;
            //    ManualResetEventSlim slim;
            //    lock (SlimLocker)
            //    {
            //        if (_slims.ContainsKey(exchange.Name))
            //        {
            //            slim = _slims[exchange.Name];
            //        }
            //        else
            //        {
            //            slim = _slims[exchange.Name] = new ManualResetEventSlim(true);
            //        }

            //        if (!slim.IsSet)
            //        {
            //            gotSlim = false;
            //        }
            //        else
            //        {
            //            slim.Wait();
            //            slim.Reset();
            //            gotSlim = true;
            //        }
            //    }

            //    if (!gotSlim)
            //    {
            //        return Request.CreateResponse(HttpStatusCode.OK, alreadyRunningText);
            //    }

            //    Task.Run(() =>
            //    {
            //        try
            //        {
            //            exchange.GetUserTradeHistory(CachePolicy.ForceRefresh);
            //        }
            //        catch (Exception exception)
            //        {
            //            _log.Error(exception);
            //        }
            //        finally
            //        {
            //            slim.Set();
            //        }
            //    });

            //    return Request.CreateResponse(HttpStatusCode.OK, $"Starting the refresh history process for {exchange.Name}.");
            //}
            //catch (Exception exception)
            //{
            //    _log.Error(exception);
            //    throw;
            //}
        }

        [HttpPost]
        [Route("api/get-history")]
        public HttpResponseMessage GetHistory()
        {
            throw new NotImplementedException();

            //try
            //{
            //    return GetHistoryUnwrapped();
            //}
            //catch (Exception exception)
            //{
            //    _log.Error(exception);

            //    throw;
            //}
        }

        public class GetHistoryForExchangeServiceModel
        {
            public string Exchange { get; set; }
            public bool ForceRefresh { get; set; }
            public string CachePolicy { get; set; }
            public int? Limit { get; set; }
        }

        private HttpResponseMessage GetHistoryUnwrapped()
        {
            throw new NotImplementedException();

            //var exchanges = new List<ITradeHistoryIntegration>
            //{
            //    _binanceIntegration,
            //    _livecoinIntegration,
            //    _krakenIntegration,
            //    _coinbaseIntegration,
            //    _bitzIntegration,
            //    _cossIntegration,
            //    _mewIntegration,
            //    _idexIntegration
            //};

            //var tasks = exchanges.Select(item => (item.Name, Task.Run(() =>
            //{
            //    try
            //    {
            //        return item.GetUserTradeHistory(CachePolicy.OnlyUseCache);
            //    }
            //    catch(Exception exception)
            //    {
            //        _log.Error(exception);
            //        return new List<HistoricalTrade>();
            //    }
            //}))).ToList();

            //tasks.ForEach(task => task.Item2.Wait());

            //var allVms = new List<HistoricalTradeViewModel>();
            //foreach (var task in tasks)
            //{
            //    var exchangeName = task.Item1;
            //    var items = task.Item2.Result;

            //    foreach (var item in items)
            //    {
            //        var vmItem = HistoricalTradeViewModel.FromModel(item, exchangeName);
            //        allVms.Add(vmItem);
            //    }
            //}

            //var orderedVms = allVms
            //    .OrderByDescending(item => item.TimeStampUtc)
            //    .ToList();

            //return Request.CreateResponse(HttpStatusCode.OK, orderedVms);
        }

        private ITradeHistoryIntegration GetExchangeFromName(string name)
        {
            throw new NotImplementedException();

            //if (string.IsNullOrWhiteSpace(name)) { throw new ArgumentNullException(nameof(name)); }

            //var simplify = new Func<string, string>(x =>
            //{
            //    return Regex.Replace((x ?? string.Empty).ToUpper().Replace("-", string.Empty), @"\s+", string.Empty);
            //});

            //return _exchanges.SingleOrDefault(item =>
            //    string.Equals(simplify(item.Name), simplify(name), StringComparison.InvariantCultureIgnoreCase)
            //);
        }
    }
}
