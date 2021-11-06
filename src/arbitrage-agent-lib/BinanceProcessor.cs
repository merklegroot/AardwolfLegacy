using binance_lib;

namespace arbitrage_agent_lib
{
    public class BinanceProcessor
    {
        private readonly IBinanceIntegration _binance;

        public BinanceProcessor(IBinanceIntegration binance)
        {
            _binance = binance;
        }

        public void Execute()
        {
        }
    }
}
