using System.Collections.Generic;
using console_app_lib;
using dump_lib;
using exchange_service_lib.Handlers;
using trade_contracts;
using trade_contracts.Messages.Exchange;
using trade_res;

namespace exchange_service_test_con.App
{
    public interface ITestApp
    {
        void Run();
    }

    public class TestApp : ConsoleApp, ITestApp
    {
        private const string ApplicationName = "Test App";

        private readonly IExchangeHandler _exchangeHandler;

        public TestApp(IExchangeHandler exchangeHandler)
        {
            _exchangeHandler = exchangeHandler;
        }

        protected override List<MenuItem> Menu =>
            new List<MenuItem>
            {
                new MenuItem("Get (E)xchanges", 'E', () => ShowExchanges()),
                new MenuItem("(B)inance ETH-BTC", 'B', () => ShowEthBtc(ExchangeNameRes.Binance)),
                new MenuItem("(C)oss ETH-BTC", 'C', () => ShowEthBtc(ExchangeNameRes.Coss)),
                new MenuItem("(O)pen orders for Coss", 'O', ShowCossOpenOrders)
            };

        private void ShowExchanges()
        {
            var req = new GetExchangesRequestMessage();
            var result = _exchangeHandler.Handle(req);
            result.Dump();
        }

        private void ShowCossOpenOrders()
        {
            var req = new GetOpenOrdersForTradingPairRequestMessage
            {
                Exchange = ExchangeNameRes.Coss,
                Symbol = "ETH",
                BaseSymbol = "BTC",
                CachePolicy = CachePolicyContract.ForceRefresh
            };

            var result = _exchangeHandler.Handle(req);
            result.Dump();
        }

        private void ShowEthBtc(string exchange)
        {
            var req = new GetOrderBookRequestMessage
            {
                Exchange = exchange,
                TradingPair = new TradingPairContract
                {
                    Symbol = "ETH",
                    BaseSymbol = "BTC"
                }
            };

            var result = _exchangeHandler.Handle(req);
            result.Dump();
        }

        private void Test_hitbtc()
        {
            const string Exchange = "HitBtc";
            const string Symbol = "FYN";

            var req = new GetDetailedCommodityForExchangeRequestMessage
            {
                Exchange = Exchange,
                Symbol = Symbol,
                NativeSymbol = null,
                CachePolicy = CachePolicyContract.AllowCache
            };

            var result = _exchangeHandler.Handle(req);
            result.Dump();
        }
    }
}
