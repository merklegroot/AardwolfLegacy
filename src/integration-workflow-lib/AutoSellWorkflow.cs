using exchange_client_lib;
using trade_browser_lib;

namespace integration_workflow_lib
{
    public class AutoSellWorkflow
    {
        private readonly IExchangeClient _exchangeClient;

        public AutoSellWorkflow(IExchangeClient exchangeClient)
        {
            _exchangeClient = exchangeClient;
        }

        public void Execute(string symbol)
        {
            new AutoSellStrategy();
        }
    }
}
