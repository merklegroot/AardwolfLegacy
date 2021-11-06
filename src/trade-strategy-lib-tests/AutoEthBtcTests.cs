using Microsoft.VisualStudio.TestTools.UnitTesting;
using trade_model;
using Shouldly;
using System.Collections.Generic;
using trade_strategy_lib;
using dump_lib;

namespace trade_strategy_lib_tests
{
    [TestClass]
    public class AutoEthBtcTests
    {
        // bit-z
        // min ETH/BTC: 0.050 ETH
        // min BTC/DKT: 0.001 BTC (assuming this also means that's the general minimum BTC trade?)
        private const decimal MinimumEth = 0.050m;
        private const decimal ProfitPercentageThreshold = 1.0m;

        private AutoEthBtc _autoEthBtc;

        [TestInitialize]
        public void Setup()
        {
            _autoEthBtc = new AutoEthBtc();
        }

        [TestMethod]
        public void Strategy__Auto_eth__no_orders()
        {
            var cossOrderBook = new OrderBook();
            var binanceOrderBook = new OrderBook();

            var strategyAction = _autoEthBtc.Execute(cossOrderBook, binanceOrderBook, ProfitPercentageThreshold, MinimumEth);
            strategyAction.ActionType.ShouldBe(StrategyActionEnum.DoNothing);
        }

        [TestMethod]
        public void Strategy__Auto_eth__no_winners()
        {
            var cossOrderBook = new OrderBook
            {
                Bids = new List<Order>
                {
                    new Order { Price = 2, Quantity = 1 }
                },
                Asks = new List<Order>
                {
                    new Order { Price = 5, Quantity = 1 }
                }
            };

            var binanceOrderBook = new OrderBook
            {
                Bids = new List<Order>
                {
                    new Order { Price = 1, Quantity = 1 }
                },
                Asks = new List<Order>
                {
                    new Order { Price = 6, Quantity = 1 }
                }
            };

            var result = _autoEthBtc.Execute(cossOrderBook, binanceOrderBook, ProfitPercentageThreshold, MinimumEth);
            result.Dump();

            result.ActionType.ShouldBe(StrategyActionEnum.DoNothing);
        }

        [TestMethod]
        public void Strategy__Auto_eth__simple_buy()
        {
            var cossOrderBook = new OrderBook
            {
                Bids = new List<Order>
                {
                    new Order { Price = 2, Quantity = 1 }
                },
                Asks = new List<Order>
                {
                    new Order { Price = 3, Quantity = 1 }
                }
            };

            var binanceOrderBook = new OrderBook
            {
                Bids = new List<Order>
                {
                    new Order { Price = 4, Quantity = 1 }
                },
                Asks = new List<Order>
                {
                    new Order { Price = 6, Quantity = 1 }
                }
            };

            var result = _autoEthBtc.Execute(cossOrderBook, binanceOrderBook, ProfitPercentageThreshold, MinimumEth);
            result.Dump();

            result.ActionType.ShouldBe(StrategyActionEnum.PlaceBid);
            result.Price.ShouldBe(3);
            result.Quantity.ShouldBe(1.00001m);
        }

        [TestMethod]
        public void Strategy__Auto_eth__simple_sell()
        {
            var cossOrderBook = new OrderBook
            {
                Bids = new List<Order>
                {
                    new Order { Price = 4, Quantity = 1 }
                },
                Asks = new List<Order>
                {
                    new Order { Price = 5, Quantity = 1 }
                }
            };

            var binanceOrderBook = new OrderBook
            {
                Bids = new List<Order>
                {
                    new Order { Price = 2, Quantity = 1 }
                },
                Asks = new List<Order>
                {
                    new Order { Price = 3, Quantity = 1 }
                }
            };

            var result = _autoEthBtc.Execute(cossOrderBook, binanceOrderBook, ProfitPercentageThreshold, MinimumEth);
            result.Dump();

            result.ActionType.ShouldBe(StrategyActionEnum.PlaceAsk);
            result.Price.ShouldBe(4);            
            result.Quantity.ShouldBe(1.00001m);
        }

        [TestMethod]
        public void Strategy__Auto_eth__sell_multiple()
        {
            var cossOrderBook = new OrderBook
            {
                Bids = new List<Order>
                {
                    new Order { Price = 4, Quantity = 1 },
                    new Order { Price = 4.1m, Quantity = 1 }
                },
                Asks = new List<Order>
                {
                    new Order { Price = 5, Quantity = 1 }
                }
            };

            var binanceOrderBook = new OrderBook
            {
                Bids = new List<Order>
                {
                    new Order { Price = 2, Quantity = 1 }
                },
                Asks = new List<Order>
                {
                    new Order { Price = 3, Quantity = 1 }
                }
            };

            var result = _autoEthBtc.Execute(cossOrderBook, binanceOrderBook, ProfitPercentageThreshold, MinimumEth);
            result.Dump();

            result.ActionType.ShouldBe(StrategyActionEnum.PlaceAsk);
            result.Price.ShouldBe(4);
            result.Quantity.ShouldBe(2.00001m);
        }

        [TestMethod]
        public void Strategy__Auto_eth__buy_multiple()
        {
            var cossOrderBook = new OrderBook
            {
                Bids = new List<Order>
                {
                    new Order { Price = 2, Quantity = 1 },
                },
                Asks = new List<Order>
                {
                    new Order { Price = 3, Quantity = 1 },
                    new Order { Price = 3.1m, Quantity = 1 }
                }
            };

            var binanceOrderBook = new OrderBook
            {
                Bids = new List<Order>
                {
                    new Order { Price = 4, Quantity = 1 }
                },
                Asks = new List<Order>
                {
                    new Order { Price = 5, Quantity = 1 }
                }
            };

            var result = _autoEthBtc.Execute(cossOrderBook, binanceOrderBook, ProfitPercentageThreshold, MinimumEth);
            result.Dump();

            result.ActionType.ShouldBe(StrategyActionEnum.PlaceBid);
            result.Price.ShouldBe(3.1m);
            result.Quantity.ShouldBe(2.00001m);
        }
    }
}
