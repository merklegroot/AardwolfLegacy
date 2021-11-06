using System;
using trade_browser_lib;
using trade_ioc;
using StructureMap;

namespace trade_browser_con
{
    public class App : IDisposable
    {
        private readonly ICossAgent _agent;

        public App()
        {
            var container = Container.For<AgentRegistry>();
            _agent = container.GetInstance<ICossAgent>();            
        }       

        public void Run()
        {
            _agent.Start();
        }

        public void Dispose()
        {
            if (_agent != null) { _agent.Dispose(); }
        }
    }

    public class AgentRegistry : DefaultRegistry
    {
        public AgentRegistry()
        {
            For<ICossAgent>().Use<CossAgent>();
            For<IOrderManager>().Use<OrderManager>();
        }
    }
}
