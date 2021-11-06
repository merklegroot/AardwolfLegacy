using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using trade_strategy_lib;

namespace trade_strategy_lib_tests
{
    [TestClass]
    public class AutoArbTests
    {
        private AutoArb _autoArb;

        [TestInitialize]
        public void Setup()
        {
            _autoArb = new AutoArb();
        }

        [TestMethod]
        public void Auto_arb__simple_scenario()
        {

        }
    }
}
