using System.Collections.Generic;
using trade_lib;

namespace worker_lib
{
    public class WorkerApp
    {
        private List<ITradeIntegration> _integrations;

        public WorkerApp(List<ITradeIntegration> integrations)
        {
            _integrations = integrations;
        }

        public void Run()
        {
            
        }
    }
}
