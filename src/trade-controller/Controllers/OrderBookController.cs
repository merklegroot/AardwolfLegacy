using trade_lib;
using System;
using System.Net.Http;
using System.Web.Http;
using System.Collections.Generic;
using trade_model;
using System.Net;
using System.Threading.Tasks;
using System.Linq;
using Newtonsoft.Json;

namespace trade_web.Controllers
{
    public class OrderBookController : BaseController
    {
        public class GetOrderBookServiceModel
        {
            public string Symbol { get; set; }
            public string BaseSymbol { get; set; }
            public string ExchangeA { get; set; }
            public string ExchangeB { get; set; }
        }

        [HttpPost]
        [Route("api/get-order-book")]
        public HttpResponseMessage GetOrderBook(GetOrderBookServiceModel serviceModel)
        {
            try
            {
                if (serviceModel == null) { throw new ArgumentNullException(nameof(serviceModel)); }
                if (string.IsNullOrWhiteSpace(serviceModel.ExchangeA)) { throw new ArgumentNullException(nameof(serviceModel.ExchangeA)); }
                if (string.IsNullOrWhiteSpace(serviceModel.ExchangeB)) { throw new ArgumentNullException(nameof(serviceModel.ExchangeB)); }

                var tradingPair = new TradingPair(serviceModel.Symbol, serviceModel.BaseSymbol);

                var integrationA = GetIntegration(serviceModel.ExchangeA.Trim());
                if (integrationA == null) { throw new ApplicationException($"Unable to retrieve integration for \"{serviceModel.ExchangeA}\"."); }

                var integrationB = GetIntegration(serviceModel.ExchangeB.Trim());
                if (integrationB == null) { throw new ApplicationException($"Unable to retrieve integration for \"{serviceModel.ExchangeB}\"."); }

                var orderBookATask = Task.Run(() => integrationA.GetOrderBook(tradingPair));
                var orderBookBTask = Task.Run(() => integrationB.GetOrderBook(tradingPair));
                var integrationAWithdrawlFee = integrationA.GetWithdrawlFee(tradingPair.Symbol);
                var integrationBWithdrawlFee = integrationB.GetWithdrawlFee(tradingPair.Symbol);

                var orderBookA = orderBookATask.Result;
                var orderBookB = orderBookBTask.Result;

                orderBookA.Asks = orderBookA.Asks.OrderBy(item => item.Price).Take(5).ToList();
                orderBookA.Bids = orderBookA.Bids.OrderByDescending(item => item.Price).Take(5).ToList();

                orderBookB.Asks = orderBookB.Asks.OrderBy(item => item.Price).Take(5).ToList();
                orderBookB.Bids = orderBookB.Bids.OrderByDescending(item => item.Price).Take(5).ToList();

                var vm = new
                {
                    withdrawalFeeA = integrationA.GetWithdrawlFee(tradingPair.Symbol),
                    withdrawalFeeB = integrationB.GetWithdrawlFee(tradingPair.Symbol),
                    orderBookA,
                    orderBookB
                };

                return Request.CreateResponse(HttpStatusCode.OK, vm);
            }
            catch (Exception exception)
            {
                _logRepo.Error(exception);
                var json = JsonConvert.SerializeObject(serviceModel, Formatting.Indented);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, $"Failed to get order book for{Environment.NewLine}{json}");
            }
        }

        private ITradeIntegration GetIntegration(string name)
        {
            var integrationDictionary = new Dictionary<string, ITradeIntegration>(StringComparer.InvariantCultureIgnoreCase)
            {
                { "Coss", _cossIntegration },
                { "Binance", _binanceIntegration },
                { "HitBtc", _hitBtcIntegration },
                { "Cryptopia", _cryptopiaIntegration },
                { "BitZ", _bitzIntegration },
                { "Tidex", _tidexIntegration }
            };

            return integrationDictionary.ContainsKey(name) ? integrationDictionary[name] : null;
        }
    }
}