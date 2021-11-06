using balance_lib;
using dump_lib;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StructureMap;
using trade_ioc;

namespace balance_lib_integration_tests
{
    [TestClass]
    public class BalanceAggregatorTests
    {
        private IBalanceAggregator _balanceAggregator;

        [TestInitialize]
        public void Setup()
        {
            var container = Container.For<DefaultRegistry>();
            _balanceAggregator = container.GetInstance<IBalanceAggregator>();
        }

        [TestMethod]
        public void Balance_aggregator__idex()
        {
            var serviceModel = new GetHoldingsForExchangeServiceModel
            {
                Name = "idex",
                ForceRefresh = false
            };

            var results = _balanceAggregator.GetHoldingsForExchange(serviceModel);
            results.Dump();
        }

        [TestMethod]
        public void Balance_aggregator__mew()
        {
            var serviceModel = new GetHoldingsForExchangeServiceModel
            {
                Name = "mew",
                ForceRefresh = false
            };

            var results = _balanceAggregator.GetHoldingsForExchange(serviceModel);
            results.Dump();
        }

        [TestMethod]
        public void Balance_aggregator__cryptopia()
        {
            var serviceModel = new GetHoldingsForExchangeServiceModel
            {
                Name = "cryptopia",
                ForceRefresh = false
            };

            var results = _balanceAggregator.GetHoldingsForExchange(serviceModel);
            results.Dump();
        }
    }
}
