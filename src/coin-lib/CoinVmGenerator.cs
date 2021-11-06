using coin_lib.Containers;
using coin_lib.ServiceModel;
using coin_lib.ViewModel;
using log_lib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using cache_lib.Models;
using trade_res;
using trade_model;
using exchange_client_lib;

namespace coin_lib
{
    public class CoinVmGenerator : ICoinVmGenerator
    {
        private const decimal ProfitPercentageThreshold = 0.75m;
        private const int MaxRows = 10;

        private readonly IExchangeClient _exchangeClient;
        private readonly ILogRepo _log;

        private static List<string> ExchangesToUse => new List<string>
        {
            ExchangeNameRes.Binance,
            ExchangeNameRes.Coss,
            ExchangeNameRes.HitBtc,
            ExchangeNameRes.Bitz,
            //ExchangeNameRes.Kraken,
            ExchangeNameRes.Livecoin,
            ExchangeNameRes.KuCoin,
            //ExchangeNameRes.Cryptopia,
            ExchangeNameRes.Qryptos,

            // ExchangeNameRes.Idex,
            // ExchangeNameRes.Tidex,
            ExchangeNameRes.Yobit
        };

        public List<ExchangeContainer> _exchanges = ExchangesToUse
            .Select(queryExchange => new ExchangeContainer { Exchange = queryExchange })
            .ToList();

        public CoinVmGenerator(
            IExchangeClient exchangeClient,
            ILogRepo log)
        {
            _exchangeClient = exchangeClient;
            _log = log;

            FillExchangeDetails();
        }

        private void FillExchangeDetails()
        {
            var stopWatchAggregate = new Stopwatch();
            stopWatchAggregate.Start();
            var tasks = new List<Task>();

            foreach (var exchange in _exchanges)
            {
                var task = new Task(() =>
                {
                    var stopWatch = new Stopwatch();
                    stopWatch.Start();

                    exchange.TradingPairs = _exchangeClient.GetTradingPairs(exchange.Exchange, CachePolicy.OnlyUseCache);
                    if (string.Equals(exchange.Exchange, "qryptos", StringComparison.InvariantCultureIgnoreCase))
                    {
                        var ve = exchange.TradingPairs.Where(item => item.Symbol.ToUpper().StartsWith("VE")).ToList();
                    }

                    exchange.WithdrawalFees = _exchangeClient.GetWithdrawalFees(exchange.Exchange, CachePolicy.OnlyUseCache);
                    exchange.Commodities = _exchangeClient.GetCommoditiesForExchange(exchange.Exchange, CachePolicy.OnlyUseCache);

                    stopWatch.Stop();

                    //Console.WriteLine($"It took {stopWatch.ElapsedMilliseconds} ms to fill the details for {exchange.Exchange}.");
                }, TaskCreationOptions.LongRunning);

                task.Start();
                tasks.Add(task);                
            }

            tasks.ForEach(task => task.Wait());

            stopWatchAggregate.Stop();

            var elapsedMilliseconds = stopWatchAggregate.ElapsedMilliseconds;

            Console.WriteLine($"It took {elapsedMilliseconds} ms to fill the details for all exchanges.");
        }

        public CoinViewModel GenerateVm(
            string symbol,
            string baseSymbol,
            List<ExchangeContainer> exchanges,
            CachePolicy cachePolicy)
        {
            if (string.IsNullOrWhiteSpace(symbol)) { throw new ArgumentNullException(nameof(symbol)); }
            if (string.IsNullOrWhiteSpace(baseSymbol)) { throw new ArgumentNullException(nameof(baseSymbol)); }
            if (exchanges == null) { throw new ArgumentNullException(nameof(exchanges)); }
            if (exchanges.Count != 2) { throw new ArgumentOutOfRangeException(nameof(exchanges)); }
            if (exchanges[0] == null) { throw new ArgumentException($"{nameof(exchanges)}[0] must not be null."); }
            if (exchanges[1] == null) { throw new ArgumentException($"{nameof(exchanges)}[1] must not be null."); }

            var viewModel = new CoinViewModel
            {
                Symbol = symbol,
                BaseSymbol = baseSymbol,
                Exchanges = new List<CoinExchangeViewModel> { new CoinExchangeViewModel(), new CoinExchangeViewModel() }
            };

            decimal? majorExchangeWithdrawalFee = null;
            if (exchanges[1].WithdrawalFees != null && exchanges[1].WithdrawalFees.ContainsKey(symbol))
            {
                majorExchangeWithdrawalFee = exchanges[1].WithdrawalFees[symbol];
                viewModel.Exchanges[1].WithdrawalFee = $"{exchanges[1].WithdrawalFees[symbol].ToString()} {symbol}";
            }

            decimal? minorExchangeWithdrawalFee = null;
            if (exchanges[0].WithdrawalFees != null && exchanges[0].WithdrawalFees.ContainsKey(symbol))
            {
                minorExchangeWithdrawalFee = exchanges[0].WithdrawalFees[symbol];
                viewModel.Exchanges[0].WithdrawalFee = $"{exchanges[0].WithdrawalFees[symbol].ToString()} {symbol}";
            }

            var majorBookTask = Task.Run(() =>
            {
                try
                {
                    var stopWatch = new Stopwatch();
                    stopWatch.Start();

                    var result = _exchangeClient.GetOrderBook(exchanges[1].Exchange, symbol, baseSymbol, cachePolicy);

                    stopWatch.Stop();
                    _log.Info($"CoinController - It took {stopWatch.ElapsedMilliseconds} ms to get the {symbol}-{baseSymbol} order book for \"{exchanges[1].Exchange}\".");

                    return result;
                }
                catch (Exception exception)
                {
                    _log.Error(new StringBuilder()
                        .AppendLine($"Failed to get orderbook for exchange \"{exchanges[1].Exchange}\" with trading pair \"{symbol}-{baseSymbol}\".")
                        .AppendLine(exception.Message)
                        .AppendLine(exception.StackTrace).ToString());
                    throw;
                }
            });

            var minorBookTask = Task.Run(() =>
            {
                try
                {
                    var stopWatch = new Stopwatch();
                    stopWatch.Start();

                    var result = _exchangeClient.GetOrderBook(exchanges[0].Exchange, symbol, baseSymbol, cachePolicy);

                    stopWatch.Stop();
                    _log.Info($"CoinController - It took {stopWatch.ElapsedMilliseconds} ms to get the {symbol}-{baseSymbol} order book for \"{exchanges[0].Exchange}\".");

                    return result;
                }
                catch (Exception exception)
                {
                    _log.Error(new StringBuilder()
                        .AppendLine($"Failed to get orderbook for exchange \"{exchanges[0].Exchange}\" with trading pair \"{symbol}\"-{baseSymbol}\".")
                        .AppendLine(exception.Message)
                        .AppendLine(exception.StackTrace).ToString());
                    throw;
                }
            });

            var majorBook = majorBookTask.Result;
            var minorBook = minorBookTask.Result;

            var majorBestBid = majorBook != null ? majorBook.Bids.OrderByDescending(item => item.Price).FirstOrDefault() : null;
            var majorBestAsk = majorBook != null ? majorBook.Asks.OrderBy(item => item.Price).FirstOrDefault() : null;

            viewModel.Exchanges[1].OrderBookAsOf = majorBook?.AsOf;
            viewModel.Exchanges[1].Asks = majorBook != null && majorBook.Asks != null ? majorBook.Asks.OrderBy(item => item.Price).Take(MaxRows).Select(item => OrderViewModel.FromModel(item)).ToList() : null;
            viewModel.Exchanges[1].Bids = majorBook != null && majorBook.Bids != null ? majorBook.Bids.OrderByDescending(item => item.Price).Take(MaxRows).Select(item => OrderViewModel.FromModel(item)).ToList() : null;

            viewModel.Exchanges[1].BidPrice = majorBestBid != null ? majorBestBid.Price.ToString() : "--";
            viewModel.Exchanges[1].BidQuantity = majorBestBid != null ? majorBestBid.Quantity.ToString() : "--";
            viewModel.Exchanges[1].AskPrice = majorBestAsk != null ? majorBestAsk.Price.ToString() : "--";
            viewModel.Exchanges[1].AskQuantity = majorBestAsk != null ? majorBestAsk.Quantity.ToString() : "--";

            var minorBestBid = minorBook != null && minorBook.Bids != null ? minorBook.Bids.OrderByDescending(item => item.Price).FirstOrDefault() : null;
            viewModel.Exchanges[0].OrderBookAsOf = minorBook?.AsOf;
            viewModel.Exchanges[0].Bids = minorBook != null && minorBook.Bids != null ? minorBook.Bids.OrderByDescending(item => item.Price).Take(MaxRows).Select(item => OrderViewModel.FromModel(item)).ToList() : null;
            viewModel.Exchanges[0].Asks = minorBook != null && minorBook.Asks != null ? minorBook.Asks.OrderBy(item => item.Price).Take(MaxRows).Select(item => OrderViewModel.FromModel(item)).ToList() : null;

            if (viewModel.Exchanges[0].Bids != null)
            {
                viewModel.Exchanges[0].Bids.ForEach(bid =>
                {
                    if (bid.Price.HasValue && majorBestAsk != null && bid.Price > majorBestAsk.Price)
                    {
                        bid.IsGoodOrder = true;
                    }
                });
            }

            var minorBestAsk = minorBook != null && minorBook.Asks != null ? minorBook.Asks.OrderBy(item => item.Price).FirstOrDefault() : null;

            if (viewModel.Exchanges[1].Bids != null)
            {
                viewModel.Exchanges[1].Bids.ForEach(bid =>
                {
                    if (bid.Price.HasValue && minorBestAsk != null && bid.Price > minorBestAsk.Price)
                    {
                        bid.IsGoodOrder = true;
                    }
                });
            }

            viewModel.Exchanges[0].BidPrice = minorBestBid != null ? minorBestBid.Price.ToString() : "--";
            viewModel.Exchanges[0].BidQuantity = minorBestBid != null ? minorBestBid.Quantity.ToString() : "--";

            viewModel.Exchanges[0].AskPrice = minorBestAsk != null ? minorBestAsk.Price.ToString() : "--";
            viewModel.Exchanges[0].AskQuantity = minorBestAsk != null ? minorBestAsk.Quantity.ToString() : "--";

            viewModel.Exchanges[0].Profit = majorBestBid != null && minorBestAsk != null ? (majorBestBid.Price - minorBestAsk.Price).ToString() : "---";
            viewModel.Exchanges[0].ProfitPercentage =
                majorBestBid != null && minorBestAsk != null
                ? (100.0m * (majorBestBid.Price - minorBestAsk.Price) / minorBestAsk.Price)
                : (decimal?)null;

            viewModel.Exchanges[0].ProfitPercentageDisplayText =
                viewModel.Exchanges[0].ProfitPercentage.HasValue
                ? viewModel.Exchanges[0].ProfitPercentage.Value.ToString("N4") + "%"
                : "---";

            viewModel.Exchanges[1].Profit = minorBestBid != null && majorBestAsk != null ? (minorBestBid.Price - majorBestAsk.Price).ToString() : "---";
            viewModel.Exchanges[1].ProfitPercentage =
                minorBestBid != null && majorBestAsk != null
                ? (100.0m * (minorBestBid.Price - majorBestAsk.Price) / majorBestAsk.Price)
                : (decimal?)null;

            viewModel.Exchanges[1].ProfitPercentageDisplayText =
                viewModel.Exchanges[1].ProfitPercentage.HasValue
                ? viewModel.Exchanges[1].ProfitPercentage.Value.ToString("N4") + "%"
                : "---";

            viewModel.Exchanges[1].BreakEvenQuantity =
                minorBestBid != null && majorBestAsk != null && majorExchangeWithdrawalFee.HasValue
                && majorExchangeWithdrawalFee.Value > 0 && (minorBestBid.Price - majorBestAsk.Price) > 0
                ?
                    ((majorExchangeWithdrawalFee.Value * majorBestAsk.Price)
                    /
                    (minorBestBid.Price - majorBestAsk.Price)).ToString()
                : null;

            viewModel.Exchanges[0].BreakEvenQuantity =
                majorBestBid != null && minorBestAsk != null && minorExchangeWithdrawalFee.HasValue
                && minorExchangeWithdrawalFee.Value > 0
                && (majorBestBid.Price - minorBestAsk.Price) > 0
                ?
                    ((minorExchangeWithdrawalFee.Value * minorBestAsk.Price)
                    /
                    (majorBestBid.Price - minorBestAsk.Price)).ToString()
                : null;

            viewModel.Exchanges[1].Name = exchanges[1].Exchange;
            viewModel.Exchanges[0].Name = exchanges[0].Exchange;

            if (exchanges[0].Commodities.Count(item => string.Equals(symbol, item.Symbol)) >= 2)
            {
                throw new ApplicationException($"Exchange {exchanges[0].Exchange} has multiple commodities with the symbol {symbol}.");
            }

            if (exchanges[1].Commodities.Count(item => string.Equals(symbol, item.Symbol)) >= 2)
            {
                throw new ApplicationException($"Exchange {exchanges[1].Exchange} has multiple commodities with the symbol {symbol}.");
            }

            var majorExchangeCommodity = exchanges[1].Commodities.SingleOrDefault(item => string.Equals(symbol, item.Symbol));
            var minorExchangeCommodity = exchanges[0].Commodities.SingleOrDefault(item => string.Equals(symbol, item.Symbol));

            viewModel.Exchanges[1].NativeSymbol = majorExchangeCommodity?.NativeSymbol;
            viewModel.Exchanges[1].CommodityName = majorExchangeCommodity?.Name ?? "Unspecified";
            viewModel.Exchanges[1].CommodityCanonicalId = majorExchangeCommodity?.CanonicalId;
            viewModel.Exchanges[0].NativeSymbol = minorExchangeCommodity?.NativeSymbol;
            viewModel.Exchanges[0].CommodityName = minorExchangeCommodity?.Name ?? "Unspecified";
            viewModel.Exchanges[0].CommodityCanonicalId = minorExchangeCommodity?.CanonicalId;
            viewModel.Exchanges[1].CanDeposit = majorExchangeCommodity?.CanDeposit;
            viewModel.Exchanges[1].CanWithdraw = majorExchangeCommodity?.CanWithdraw;
            viewModel.Exchanges[0].CanDeposit = minorExchangeCommodity?.CanDeposit;
            viewModel.Exchanges[0].CanWithdraw = minorExchangeCommodity?.CanWithdraw;

            if (minorExchangeCommodity?.CustomValues != null)
            {
                viewModel.Exchanges[0].CustomValues =
                    minorExchangeCommodity.CustomValues.Keys.Select(key =>
                    {
                        return new KeyValuePair<string, string>(key, minorExchangeCommodity.CustomValues[key]);
                    }).ToList();
            }

            if (majorExchangeCommodity?.CustomValues != null)
            {
                viewModel.Exchanges[1].CustomValues =
                    majorExchangeCommodity.CustomValues.Keys.Select(key =>
                    {
                        return new KeyValuePair<string, string>(key, majorExchangeCommodity.CustomValues[key]);
                    }).ToList();
            }

            return viewModel;
        }

        public static bool DoTradingPairsMatch(TradingPair pairA, TradingPair pairB)
        {
            if (pairA == null && pairB == null) { return true; }
            if (pairA == null || pairB == null) { return false; }
            
            if (pairA.CanonicalCommodityId.HasValue 
                && pairA.CanonicalCommodityId.Value != default(Guid)
                && pairB.CanonicalCommodityId.HasValue
                && pairB.CanonicalCommodityId.Value != default(Guid))
            {
                if (pairA.CanonicalCommodityId.Value != pairB.CanonicalCommodityId.Value)
                { return false; }
            }
            else
            {
                if (!string.Equals(pairA.Symbol, pairB.Symbol, StringComparison.InvariantCultureIgnoreCase))
                { return false; }
            }

            if (pairA.CanonicalBaseCommodityId.HasValue
                && pairA.CanonicalBaseCommodityId.Value != default(Guid)
                && pairB.CanonicalBaseCommodityId.HasValue
                && pairB.CanonicalBaseCommodityId.Value != default(Guid))
            {
                if (pairA.CanonicalBaseCommodityId.Value != pairB.CanonicalBaseCommodityId.Value)
                { return false; }
            }
            else
            {
                if (!string.Equals(pairA.BaseSymbol, pairB.BaseSymbol, StringComparison.InvariantCultureIgnoreCase))
                {
                    return false;
                }
            }

            return true;
        }

        public static List<TradingPair> GetIntersections(ExchangeContainer exchangeA, ExchangeContainer exchangeB)
        {
            var matches = new List<TradingPair>();
            for (var i = 0; i < exchangeA.TradingPairs.Count; i++)
            {
                var pairA = exchangeA.TradingPairs[i];
                for (var j = 0; j < exchangeB.TradingPairs.Count; j++)
                {
                    var pairB = exchangeB.TradingPairs[j];

                    if (DoTradingPairsMatch(pairA, pairB))
                    {
                        matches.Add(pairA);
                        break;
                    }
                }
            }

            return matches;
        }

        public List<TradingPairWithExchanges> GetTradingPairsWithExchanges()
        {
            var tradingPairsWithExchanges = new List<TradingPairWithExchanges>();
            for (var i = 0; i < _exchanges.Count; i++)
                for (var j = i + i; j < _exchanges.Count; j++)
                {
                    var exchangeA = _exchanges[i];
                    var exchangeB = _exchanges[j];

                    var intersections = GetIntersections(exchangeA, exchangeB);
                    tradingPairsWithExchanges.AddRange(intersections
                        .Select(tp => new TradingPairWithExchanges
                        {
                            Symbol = tp.Symbol,
                            BaseSymbol = tp.BaseSymbol,
                            ExchangeA = exchangeA.Exchange,
                            ExchangeB = exchangeB.Exchange
                        }));
                }

            return tradingPairsWithExchanges;
        }

        private List<Tuple<ExchangeContainer, ExchangeContainer>> GetExchangeCombos(
            List<string> filteredOutExchanges,
            List<string> fxchangesToInclude)
        {
            var exchangeCombos = new List<Tuple<ExchangeContainer, ExchangeContainer>>();

            for (var i = 0; i < _exchanges.Count; i++)
            {
                for (var j = i + 1; j < _exchanges.Count; j++)
                {
                    if (filteredOutExchanges != null
                        && filteredOutExchanges
                        .Any(queryExchange =>
                            string.Equals(queryExchange.Replace("-", string.Empty), _exchanges[i].Exchange.Replace("-", string.Empty), StringComparison.InvariantCultureIgnoreCase)
                            || string.Equals(queryExchange.Replace("-", string.Empty), _exchanges[j].Exchange.Replace("-", string.Empty), StringComparison.InvariantCultureIgnoreCase)))
                    {
                        continue;
                    }

                    if (fxchangesToInclude != null
                        && fxchangesToInclude.Any())
                    {
                        if (!fxchangesToInclude.Any(queryExchange => string.Equals(queryExchange.Replace("-", string.Empty), _exchanges[i].Exchange.Replace("-", string.Empty), StringComparison.InvariantCultureIgnoreCase))
                            || !fxchangesToInclude.Any(queryExchange => string.Equals(queryExchange.Replace("-", string.Empty), _exchanges[j].Exchange.Replace("-", string.Empty), StringComparison.InvariantCultureIgnoreCase)))
                        {
                            continue;
                        }
                    }

                    exchangeCombos.Add(Tuple.Create(_exchanges[i], _exchanges[j]));
                }
            }

            return exchangeCombos;
        }

        private List<TradingPairsForExchanges> GetIntersections(List<Tuple<ExchangeContainer, ExchangeContainer>> exchangeCombos)
        {
            var intersections = new List<TradingPairsForExchanges>();

            foreach (var combo in exchangeCombos)
            {
                var exA = combo.Item1;
                var exB = combo.Item2;

                var pairs = new List<TradingPair>();
                for (var i = 0; i < exA.TradingPairs.Count; i++)
                {
                    var pairA = exA.TradingPairs[i];
                    for (var j = 0; j < exB.TradingPairs.Count; j++)
                    {
                        var pairB = exB.TradingPairs[j];

                        if (pairA.Symbol.ToUpper().StartsWith("VE")
                            && pairB.Symbol.ToUpper().StartsWith("VE"))
                        {
                            var x = 1;
                        }

                        if (DoTradingPairsMatch(pairA, pairB))
                        {
                            pairs.Add(pairA);
                            break;
                        }
                    }
                }

                //var pairs = exA.TradingPairs
                //    .Where(itemA => exB.TradingPairs.Any(itemB => DoTradingPairsMatch(itemA, itemB)))
                //    .ToList();

                var intersection = new TradingPairsForExchanges
                {
                    TradingPairs = pairs,
                    ExchangeA = exA,
                    ExchangeB = exB
                };

                intersections.Add(intersection);
            }

            return intersections;
        }

        public CoinViewModelsContainer GetAllOrders(
            List<string> filteredOutExchanges,
            List<string> exchangesToInclude,
            CachePolicy cachePolicy)
        {
            var exchangeCombos = GetExchangeCombos(filteredOutExchanges, exchangesToInclude);
            var intersections = GetIntersections(exchangeCombos);

            var container = new CoinViewModelsContainer();
            var vmTasks = new List<Task<List<CoinViewModel>>>();

            var vmsGroups = new List<List<CoinViewModel>>();

            foreach (var intersection in intersections)
            {
                var vms = new List<CoinViewModel>();
                foreach (var pair in intersection.TradingPairs)
                {
                    try
                    {
                        if (intersection == null) { throw new ApplicationException($"{nameof(intersection)} must not be null."); }
                        if (intersection.ExchangeA == null) { throw new ApplicationException($"{nameof(intersection.ExchangeA)} must not be null."); }
                        if (intersection.ExchangeB == null) { throw new ApplicationException($"{nameof(intersection.ExchangeB)} must not be null."); }

                        var intersectionExchanges = new List<string> { intersection.ExchangeA.Exchange, intersection.ExchangeB.Exchange };
                        // The Binance-HitBtc route is already saturated.
                        // Deals are consumed too quickly to be worth the risk.
                        if (intersectionExchanges.Any(queryExchange => string.Equals(queryExchange, "binance", StringComparison.InvariantCultureIgnoreCase))
                            && intersectionExchanges.Any(queryExchange => string.Equals(queryExchange, "hitbtc", StringComparison.InvariantCultureIgnoreCase)))
                        {
                            continue;
                        }

                        var coinVm = GenerateVm(pair.Symbol, pair.BaseSymbol, new List<ExchangeContainer> { intersection.ExchangeA, intersection.ExchangeB }, cachePolicy);
                        vms.Add(coinVm);
                    }
                    catch (Exception exception)
                    {
                        _log.Error($"Failed for trading pair {pair} between {intersection?.ExchangeA} and {intersection?.ExchangeB}", exception);
                    }
                }


                vmsGroups.Add(vms);
            }

            foreach (var vms in vmsGroups)
            {
                try
                {
                    var isProfitableEnough = new Func<CoinViewModel, bool>(vm =>
                        (vm.Exchanges[1].ProfitPercentage.HasValue && vm.Exchanges[1].ProfitPercentage.Value > ProfitPercentageThreshold)
                        || (vm.Exchanges[0].ProfitPercentage.HasValue && vm.Exchanges[0].ProfitPercentage.Value > ProfitPercentageThreshold));

                    var canTransferFunds = new Func<CoinViewModel, bool>(vm =>
                    {
                        if (vm.Exchanges[1].ProfitPercentage.HasValue && vm.Exchanges[1].ProfitPercentage.Value > ProfitPercentageThreshold)
                        {
                            if ((!vm.Exchanges[1].CanWithdraw.HasValue || vm.Exchanges[1].CanWithdraw.Value)
                                && (!vm.Exchanges[0].CanDeposit.HasValue || vm.Exchanges[0].CanDeposit.Value))
                            {
                                return true;
                            }
                        }

                        if (vm.Exchanges[0].ProfitPercentage.HasValue && vm.Exchanges[0].ProfitPercentage.Value > ProfitPercentageThreshold)
                        {
                            if ((!vm.Exchanges[0].CanWithdraw.HasValue || vm.Exchanges[0].CanWithdraw.Value)
                                && (!vm.Exchanges[1].CanDeposit.HasValue || vm.Exchanges[1].CanDeposit.Value))
                            {
                                return true;
                            }
                        }

                        return false;
                    });

                    var profitables = vms.Where(vm =>
                        isProfitableEnough(vm)
                        && canTransferFunds(vm)
                    ).ToList();

                    if (profitables.Any()) { container.Coins.AddRange(profitables); }
                }
                catch (Exception exception)
                {
                    _log.Error(exception);
                }
            }

            container.Coins = container.Coins.OrderByDescending(coin =>
                Math.Max(coin.Exchanges[0].ProfitPercentage ?? 0, coin.Exchanges[1].ProfitPercentage ?? 0)
            ).ToList();

            return container;
        }

        public CoinViewModel GetOrdersInternal(
            string symbol,
            string baseSymbol,
            List<string> exchanges,
            CachePolicy cachePolicy)
        {
            var exchangeContainers =
                exchanges.Select(FindExchange)
                .ToList();

            var coinVm = GenerateVm(symbol, baseSymbol, exchangeContainers, cachePolicy);

            return coinVm;
        }

        private ExchangeContainer FindExchange(string name)
        {
            return _exchanges
                .SingleOrDefault(item => string.Equals(item.Exchange, name, StringComparison.InvariantCultureIgnoreCase));
        }
    }
}
