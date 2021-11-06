using trade_lib;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using trade_model;

namespace trade_web.Controllers
{
    public class ArbController : BaseController
    {
        public class Exchange
        {
            public ITradeIntegration Integration { get; set; }
            public string Name { get; set; }
            public List<TradingPair> TradingPairs { get; set; }
        }

        private List<Exchange> _exchanges;

        public ArbController()
        {
            _exchanges = new List<Exchange>
            {
                new Exchange { Name = "Binance", Integration = _binanceIntegration },
                new Exchange { Name = "HitBTC", Integration = _hitBtcIntegration },
                new Exchange { Name = "Cryptopia", Integration = _cryptopiaIntegration },
                new Exchange { Name = "Coss", Integration = _cossIntegration },
                new Exchange { Name = "Bit-Z", Integration = _bitzIntegration },
            };
        }

        private class VmItem
        {
            public string Name { get; set; }
            public List<Order> Bids { get; set; }
            public List<Order> Asks { get; set; }
        }

        public class Intersection
        {
            public TradingPair TradingPair { get; set; }
            public List<string> Exchanges { get; set; }
        }

        [HttpPost]
        [HttpGet]
        [Route("api/get-intersections")]
        public HttpResponseMessage GetIntersections()
        {
            var tradingPairsTasks = _exchanges.Select(exchange => Task.Run(() => exchange.Integration.GetTradingPairs())).ToList();
            for (var i = 0; i < _exchanges.Count; i++)
            {
                _exchanges[i].TradingPairs = tradingPairsTasks[i].Result;
            }

            var intersections = new List<Intersection>();
            var allTradingPairs = _exchanges.SelectMany(item => item.TradingPairs).Distinct().ToList();
            foreach (var tradingPair in allTradingPairs)
            {
                var matches = _exchanges.Where(exchange => exchange.TradingPairs.Any(tp => tp.Equals(tradingPair))).ToList();
                if (matches.Count > 1)
                {
                    var intersection = new Intersection { TradingPair = tradingPair, Exchanges = matches.Select(item => item.Name).ToList() };
                    intersections.Add(intersection);
                }
            }

            return Request.CreateResponse(HttpStatusCode.OK, intersections.OrderByDescending(item => item.Exchanges.Count).ToList());
        }

        [HttpPost]
        [HttpGet]
        [Route("api/arb")]
        public HttpResponseMessage GetArb()
        {
            var tradingPairsTasks = _exchanges.Select(exchange => Task.Run(() => exchange.Integration.GetTradingPairs())).ToList();
            for(var i = 0; i < _exchanges.Count; i++)
            {
                _exchanges[i].TradingPairs = tradingPairsTasks[i].Result;
            }

            var intersections = new List<dynamic>();
            var allTradingPairs = _exchanges.SelectMany(item => item.TradingPairs).Distinct().ToList();
            foreach(var tradingPair in allTradingPairs)
            {
                
                var matches = _exchanges.Where(exchange => exchange.TradingPairs.Any(tp => tp == tradingPair)).ToList();
                if (matches.Count > 1)
                {
                    var intersection = new { TradingPair = tradingPair, Exchanges = matches.Select(item => item.Name).ToList() };
                    intersections.Add(intersection);
                }                
            }

            var tradingPairs = new List<TradingPair>
            {
                new TradingPair("LTC", "BTC"),
                new TradingPair("OMG", "BTC"),
                new TradingPair("DASH", "BTC"),
                new TradingPair("HSR", "BTC"),
                new TradingPair("EOS", "BTC"),
                new TradingPair("ETC", "BTC"),
                new TradingPair("ZEC", "BTC"),
                new TradingPair("BTG", "BTC"),
                new TradingPair("TRX", "BTC"),
                new TradingPair("ENJ", "BTC"),
                new TradingPair("LSK", "BTC"),
                new TradingPair("BCH", "BTC"),
                new TradingPair("PAY", "BTC"),
                new TradingPair("PAY", "ETH"),
                new TradingPair("WISH", "BTC"),
                new TradingPair("WISH", "ETH"),
                new TradingPair("NEO", "BTC"),
                new TradingPair("VZT", "BTC"),

                new TradingPair("BPL", "BTC"),
                new TradingPair("BPL", "ETH"),

                new TradingPair("ARK", "BTC"),
                new TradingPair("ARK", "ETH"),
                new TradingPair("WAVES", "BTC"),
                new TradingPair("WAVES", "ETH"),

                new TradingPair("LINK", "BTC"),
                new TradingPair("LINK", "ETH")

            };

            var vms = new List<dynamic>();
            foreach (var tradingPair in tradingPairs)
            {
                var vmItems = new List<VmItem>();
                var exchangesWithTasks = _exchanges
                    .Where(queryExchange => queryExchange.TradingPairs.Any(item => item.Equals(tradingPair)))
                    .Select(exchange => new
                {
                    Exchange = exchange,
                    OrderBookTask = Task.Run(() => exchange.Integration.GetOrderBook(tradingPair))
                });

                foreach (var exchangeWithTask in exchangesWithTasks)
                {
                    var exchange = exchangeWithTask.Exchange;
                    var orderBook = exchangeWithTask.OrderBookTask.Result;

                    var vmItem = new VmItem
                    {
                        Name = exchange.Name,
                        Bids = orderBook.Bids.OrderByDescending(item => item.Price).Take(5).ToList(),
                        Asks = orderBook.Asks.OrderBy(item => item.Price).Take(5).ToList()
                    };

                    vmItems.Add(vmItem);
                }

                var bestAsk =
                    vmItems
                        .OrderBy(vmItem => vmItem.Asks.First().Price)
                        .Take(1)
                        .Select(vmItem => new { Name = vmItem.Name, Price = vmItem.Asks.First().Price, Quantity = vmItem.Asks.First().Quantity })
                        .First();

                var bestBid =
                    vmItems
                        .OrderBy(vmItem => vmItem.Bids.First().Price)
                        .Take(1)
                        .Select(vmItem => new { Name = vmItem.Name, Price = vmItem.Bids.First().Price, Quantity = vmItem.Bids.First().Quantity })
                        .First();

                var vm = new { TradingPair = tradingPair, Exchanges = vmItems, BestAsk = bestAsk, BestBid = bestBid };
                vms.Add(vm);
            }
            return Request.CreateResponse(HttpStatusCode.OK, vms);
        }
    }
}
