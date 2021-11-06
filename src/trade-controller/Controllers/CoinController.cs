using trade_lib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using trade_model;
using trade_web.ViewModels;

namespace trade_web.Controllers
{
    public class CoinController : BaseController
    {
        private Exchange _binanceExchange;
        private Exchange _hitBtcExchange;
        private Exchange _cryptopiaExchange;
        private Exchange _cossExchange;
        private Exchange _bitzExchange;
        private Exchange _tidexExchange;

        private List<Exchange> _exchanges
        {
            get
            {
                return new List<Exchange>
                {
                    _binanceExchange,
                    _hitBtcExchange,                    
                    _cossExchange,
                    _tidexExchange,
                    _cryptopiaExchange,
                    _bitzExchange
                };
            }
        }

        private static class ExchangeName
        {
            public const string Binance = "Binance";
            public const string HitBtc = "HitBtc";
            public const string Coss = "Coss";
            public const string Tidex = "Tidex";
            public const string Cryptopia = "Cryptopia";
            public const string BitZ = "Bit-Z";
        }

        public CoinController()
        {
            _binanceExchange = new Exchange { Name = ExchangeName.Binance, Integration = _binanceIntegration };
            _hitBtcExchange = new Exchange { Name = ExchangeName.HitBtc, Integration = _hitBtcIntegration };            
            _cossExchange = new Exchange { Name = ExchangeName.Coss, Integration = _cossIntegration };
            _tidexExchange = new Exchange { Name = ExchangeName.Tidex, Integration = _tidexIntegration };
            _cryptopiaExchange = new Exchange { Name = ExchangeName.Cryptopia, Integration = _cryptopiaIntegration };
            _bitzExchange = new Exchange { Name = ExchangeName.BitZ, Integration = _bitzIntegration };

            foreach(var exchange in _exchanges)
            {
                exchange.TradingPairsTask = Task.Run(() =>
                {
                    try
                    {
                        return exchange.Integration.GetTradingPairs();
                    }
                    catch(Exception exception)
                    {
                        _logRepo.Error(exception);
                        return new List<TradingPair>();
                    }
                });

                exchange.WithdrawlFeesTask = Task.Run(() =>
                {
                    try
                    {
                        return exchange.Integration.GetWithdrawlFees();
                    }
                    catch { return new Dictionary<string, decimal>(); }
                });

                exchange.CommoditiesTask = Task.Run(() =>
                {
                    try
                    {
                        return exchange.Integration.GetCommodities();
                    }
                    catch { return new List<CommodityForExchange>(); }
                });
            }            
        }

        public class TradingPairAndExchanges
        {
            public TradingPair TradingPair { get; set; }
            public Exchange MajorExchange { get; set; }
            public Exchange MinorExchange { get; set; }            
        }

        [HttpGet]
        [HttpPost]
        [Route("api/get-trading-pairs")]
        public HttpResponseMessage GetTradingPairsApi()
        {
            var vm = GetTradingPairsWithExchanges();
            return Request.CreateResponse(HttpStatusCode.OK, vm);
        }

        public class TradingPairWithExchanges
        {
            public string Symbol { get; set; }
            public string BaseSymbol { get; set; }
            public string ExchangeA { get; set; }
            public string ExchangeB { get; set; }
        }

        private List<TradingPairWithExchanges> GetTradingPairsWithExchanges()
        {
            var commoditiesToIgnore =
                (_binanceExchange.Commodities.Union(_hitBtcExchange.Commodities)).Where(item =>
                    (item.CanDeposit.HasValue && item.CanDeposit.Value == false) || (item.CanWithdraw.HasValue && item.CanWithdraw.Value == false))
                .ToList();

            var getIntersections = new Func<Exchange, Exchange, List<TradingPair>>((Exchange exchangeA, Exchange exchangeB) =>
                exchangeA.TradingPairs.Intersect(exchangeB.TradingPairs)
                .Where(tp => !commoditiesToIgnore.Any(com =>
                    string.Equals(com.Symbol, tp.Symbol, StringComparison.InvariantCultureIgnoreCase)
                    || string.Equals(com.Symbol, tp.BaseSymbol, StringComparison.InvariantCultureIgnoreCase)
                ))
                .ToList()
            );

            var tradingPairsWithExchanges = new List<TradingPairWithExchanges>();
            for (var i = 0; i < _exchanges.Count; i++)
            for (var j = i + i; j < _exchanges.Count; j++)
            {                
                var exchangeA = _exchanges[i];
                var exchangeB = _exchanges[j];

                tradingPairsWithExchanges.AddRange(getIntersections(exchangeA, exchangeB)
                    .Select(tp => new TradingPairWithExchanges
                    {
                        Symbol = tp.Symbol,
                        BaseSymbol = tp.BaseSymbol,
                        ExchangeA = exchangeA.Name,
                        ExchangeB = exchangeB.Name
                    }));
            }          

            return tradingPairsWithExchanges;
        }

        public class GetOrdersServiceModel
        {
            public string Symbol { get; set; }
            public string BaseSymbol { get; set; }
            public string ExchangeA { get; set; }
            public string ExchangeB { get; set; }
        }

        private Exchange FindExchange(string name)
        {
            return _exchanges
                .SingleOrDefault(item => string.Equals(item.Name, name, StringComparison.InvariantCultureIgnoreCase));
        }

        [HttpPost]
        [Route("api/get-orders")]
        public async Task<HttpResponseMessage> GetOrdersApi(GetOrdersServiceModel serviceModel)
        {
            var viewModel = await Task.Run(() => GetOrders(serviceModel));
            return Request.CreateResponse(HttpStatusCode.OK, viewModel);
        }

        private CoinViewModel GetOrders(GetOrdersServiceModel serviceModel)
        { 
            var tradingPair = new TradingPair(serviceModel.Symbol, serviceModel.BaseSymbol);
            var exchangeA = FindExchange(serviceModel.ExchangeA);
            var exchangeB = FindExchange(serviceModel.ExchangeB);

            var coinVm = GenerateVm(tradingPair, exchangeA, exchangeB, true);

            return coinVm;
        }

        private class TradingPairsForExchanges
        {
            public List<TradingPair> TradingPairs { get; set; }
            public Exchange ExchangeA { get; set; }
            public Exchange ExchangeB { get; set; }
        }

        [HttpPost]
        [Route("api/get-all-orders")]
        public async Task<HttpResponseMessage> GetAllOrdersEntryPoint()
        {
            var result = Task.Run(() => GetAllOrders());

            return Request.CreateResponse(HttpStatusCode.OK, await result);
        } 
        
        public CoinViewModelsContainer GetAllOrders()
        {
            var hitBtcCommoditiesToIgnore =
               (_hitBtcExchange.Commodities ?? new List<CommodityForExchange>()).Where(item =>
                    (item.CanDeposit.HasValue && item.CanDeposit.Value == false) || (item.CanWithdraw.HasValue && item.CanWithdraw.Value == false))
                .ToList();

            var exchangeCombos = new List<Tuple<Exchange, Exchange>>
            {
                Tuple.Create(_binanceExchange, _cossExchange),
                Tuple.Create(_hitBtcExchange, _cossExchange),                
                Tuple.Create(_tidexExchange, _cossExchange),
                Tuple.Create(_binanceExchange, _hitBtcExchange),
                Tuple.Create(_binanceExchange, _tidexExchange),
                Tuple.Create(_hitBtcExchange, _tidexExchange),
            };

            var intersections = new List<TradingPairsForExchanges>();

            foreach (var combo in exchangeCombos) {

                var exA = combo.Item1;
                var exB = combo.Item2;
                var pairs = exA.TradingPairs
                        .Intersect(exB.TradingPairs)
                        .ToList();

                var intersection = new TradingPairsForExchanges
                {
                    TradingPairs = pairs,
                    ExchangeA = exA,
                    ExchangeB = exB
                };

                intersections.Add(intersection);
            }   

            var container = new CoinViewModelsContainer();
            foreach (var intersection in intersections)
            {
                foreach (var pair in intersection.TradingPairs)
                {
                    var coinVm = GenerateVm(pair, intersection.ExchangeA, intersection.ExchangeB);
                    container.Coins.Add(coinVm);
                }
            };

            container.Coins = container.Coins.OrderByDescending(coin =>
                Math.Max(coin.BinanceToCossProfitPercentage ?? 0, coin.CossToBinanceProfitPercentage ?? 0)
            ).ToList();

            return container;
        }       
        
        public class Exchange
        {
            public string Name { get; set; }
            public ITradeIntegration Integration { get; set; }

            public Task<Dictionary<string, decimal>> WithdrawlFeesTask { get; set; }
            public Dictionary<string, decimal> WithdrawlFees { get { return WithdrawlFeesTask.Result; } }

            public Task<List<CommodityForExchange>> CommoditiesTask { get; set; }
            public List<CommodityForExchange> Commodities { get { return CommoditiesTask.Result; } }

            public Task<List<TradingPair>> TradingPairsTask { get; set; }
            public List<TradingPair> TradingPairs { get { return TradingPairsTask.Result; } }
        }

        private CoinViewModel GenerateVm(
            TradingPair tradingPair,
            Exchange majorExchange,
            Exchange minorExchange,
            bool forceRefresh = false)
        {
            var symbol = tradingPair.Symbol;
            var baseSymbol = tradingPair.BaseSymbol;
            
            var viewModel = new CoinViewModel
            {
                Symbol = symbol,
                BaseSymbol = baseSymbol
            };

            decimal? majorExchangeWithdrawlFee = null;
            if (majorExchange.WithdrawlFees.ContainsKey(symbol))
            {
                majorExchangeWithdrawlFee = majorExchange.WithdrawlFees[symbol];
                viewModel.MajorExchangeWithdrawlFee = $"{majorExchange.WithdrawlFees[symbol].ToString()} {symbol}";
            }

            decimal? cossWithdrawlFee = null;
            if (minorExchange.WithdrawlFees.ContainsKey(symbol))
            {
                cossWithdrawlFee = minorExchange.WithdrawlFees[symbol];
                viewModel.MinorExchangeWithdrawlFee = $"{minorExchange.WithdrawlFees[symbol].ToString()} {symbol}";
            }

            var majorBookTask = Task.Run(() => majorExchange.Integration.GetOrderBook(new TradingPair(symbol, baseSymbol), forceRefresh));
            var minorBookTask = Task.Run(() => minorExchange.Integration.GetOrderBook(new TradingPair(symbol, baseSymbol), forceRefresh));

            var majorBook = majorBookTask.Result;
            var minorBook = minorBookTask.Result;

            var majorBestBid = majorBook != null ? majorBook.Bids.OrderByDescending(item => item.Price).FirstOrDefault() : null;
            var majorBestAsk = majorBook != null ? majorBook.Asks.OrderBy(item => item.Price).FirstOrDefault() : null;

            const int MaxRows = 5;
            viewModel.BinanceAsks = majorBook != null && majorBook.Asks != null ? majorBook.Asks.OrderBy(item => item.Price).Take(MaxRows).Select(item => OrderViewModel.FromModel(item)).ToList() : null;
            viewModel.BinanceBids = majorBook != null && majorBook.Bids != null ? majorBook.Bids.OrderByDescending(item => item.Price).Take(MaxRows).Select(item => OrderViewModel.FromModel(item)).ToList() : null;

            viewModel.BinanceBidPrice = majorBestBid != null ? majorBestBid.Price.ToString() : "--";
            viewModel.BinanceBidQuantity = majorBestBid != null ? majorBestBid.Quantity.ToString() : "--";
            viewModel.BinanceAskPrice = majorBestAsk != null ? majorBestAsk.Price.ToString() : "--";
            viewModel.BinanceAskQuantity = majorBestAsk != null ? majorBestAsk.Quantity.ToString() : "--";
            
            var minorBestBid = minorBook != null && minorBook.Bids != null ? minorBook.Bids.OrderByDescending(item => item.Price).FirstOrDefault() : null;
            viewModel.CossBids = minorBook != null && minorBook.Bids != null ? minorBook.Bids.OrderByDescending(item => item.Price).Take(MaxRows).Select(item => OrderViewModel.FromModel(item)).ToList() : null;
            viewModel.CossAsks = minorBook != null && minorBook.Asks != null ? minorBook.Asks.OrderBy(item => item.Price).Take(MaxRows).Select(item => OrderViewModel.FromModel(item)).ToList() : null;

            viewModel.CossBids.ForEach(bid =>
            {
                if (bid.Price.HasValue && majorBestAsk != null && bid.Price > majorBestAsk.Price)
                {
                    bid.IsGoodOrder = true;
                }
            });

            var minorBestAsk = minorBook != null && minorBook.Asks != null ? minorBook.Asks.OrderBy(item => item.Price).FirstOrDefault() : null;

            viewModel.BinanceBids.ForEach(bid =>
            {
                if (bid.Price.HasValue && minorBestAsk != null && bid.Price > minorBestAsk.Price)
                {
                    bid.IsGoodOrder = true;
                }
            });

            viewModel.CossBidPrice = minorBestBid != null ? minorBestBid.Price.ToString() : "--";
            viewModel.CossBidQuantity = minorBestBid != null ? minorBestBid.Quantity.ToString() : "--";

            viewModel.CossAskPrice = minorBestAsk != null ? minorBestAsk.Price.ToString() : "--";
            viewModel.CossAskQuantity = minorBestAsk != null ? minorBestAsk.Quantity.ToString() : "--";

            viewModel.CossToBinanceProfit = majorBestBid != null && minorBestAsk != null ? (majorBestBid.Price - minorBestAsk.Price).ToString() : "---";
            viewModel.CossToBinanceProfitPercentage =
                majorBestBid != null && minorBestAsk != null
                ? (100.0m * (majorBestBid.Price - minorBestAsk.Price) / minorBestAsk.Price)
                : (decimal?)null;

            viewModel.CossToBinanceProfitPercentageDisplayText =
                viewModel.CossToBinanceProfitPercentage.HasValue
                ? viewModel.CossToBinanceProfitPercentage.Value.ToString("N4") + "%"
                : "---";

            viewModel.BinanceToCossProfit = minorBestBid != null && majorBestAsk != null ? (minorBestBid.Price - majorBestAsk.Price).ToString() : "---";
            viewModel.BinanceToCossProfitPercentage =
                minorBestBid != null && majorBestAsk != null
                ? (100.0m * (minorBestBid.Price - majorBestAsk.Price) / majorBestAsk.Price)
                : (decimal?)null;

            viewModel.BinanceToCossProfitPercentageDisplayText =
                viewModel.BinanceToCossProfitPercentage.HasValue
                ? viewModel.BinanceToCossProfitPercentage.Value.ToString("N4") + "%"
                : "---";

            viewModel.BinanceToCossBreakEvenQuantity =
                minorBestBid != null && majorBestAsk != null && majorExchangeWithdrawlFee.HasValue
                && majorExchangeWithdrawlFee.Value > 0 && (minorBestBid.Price - majorBestAsk.Price) > 0
                ?
                    ((majorExchangeWithdrawlFee.Value * majorBestAsk.Price)
                    /
                    (minorBestBid.Price - majorBestAsk.Price)).ToString()
                : null;

            viewModel.CossToBinanceBreakEvenQuantity =
                majorBestBid != null && minorBestAsk != null && cossWithdrawlFee.HasValue
                && cossWithdrawlFee.Value > 0
                && (majorBestBid.Price - minorBestAsk.Price) > 0
                ?
                    ((cossWithdrawlFee.Value * minorBestAsk.Price)
                    /
                    (majorBestBid.Price - minorBestAsk.Price)).ToString()
                : null;

            viewModel.MajorExchange.Name = majorExchange.Name;
            viewModel.MinorExchange.Name = minorExchange.Name;

            return viewModel;
        }
    }
}
