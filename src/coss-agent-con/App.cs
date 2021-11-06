using System;
using trade_browser_lib;
using trade_ioc;
using StructureMap;
using coss_agent_lib;
using rabbit_lib;
using integration_workflow_lib;
using coss_agent_lib.Strategy;
using coss_data_lib;

namespace coss_agent_con
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
            For<IRabbitConnectionFactory>().Use<RabbitConnectionFactory>();
            For<ICossWebDriverFactory>().Use<CossWebDriverFactory>();
            For<ICossDriver>().Use<CossDriver>();
            For<IOrderManager>().Use<OrderManager>();
            For<IArbitrageWorkflow>().Use<ArbitrageWorkflow>();
            For<ICossAutoBuy>().Use<CossAutoBuy>();
            For<ICossAutoOpenBid>().Use<CossAutoOpenBid>();
            For<ICossXhrOpenOrderRepo>().Use<CossXhrOpenOrderRepo>();
            For<ICossAgent>().Use<CossAgent>();       
        }
    }
}
