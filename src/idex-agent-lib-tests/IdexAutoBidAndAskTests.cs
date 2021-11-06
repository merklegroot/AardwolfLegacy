using dump_lib;
using idex_agent_lib;
using idex_integration_lib.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace idex_agent_lib_tests
{
    [TestClass]
    public class IdexAutoBidAndAskTests
    {
        private IdexAutoBidAndAsk _auto;

        [TestInitialize]
        public void Setup()
        {
            _auto = new IdexAutoBidAndAsk();
        }

        [TestMethod]
        public void Idex_auto_bid_and_ask__pass_invalid_data()
        {
            var result = _auto.Execute(0, 0, null, null, 0, 0);
            result.Dump();
        }

        [TestMethod]
        public void Idex_auto_bid_and_ask__simple_scenario()
        {
            var tokenOwned = 10;
            var ethOwned = 1;
            const string UserEthAddress = "0xabcd";
            var binanceBestBid = 0.01m;
            var binanceBestAsk = 0.02m;
            var idexOrderBook = new IdexExtendedOrderBook
            {
                Asks = new List<IdexExtendedOrder>
                {
                    new IdexExtendedOrder { Price = 0.025m, Quantity = 1 }
                },
                Bids = new List<IdexExtendedOrder>
                {
                    new IdexExtendedOrder { Price = 0.02m, Quantity = 1 }
                }
            };

            var result = _auto.Execute(tokenOwned, ethOwned, UserEthAddress, idexOrderBook, binanceBestBid, binanceBestAsk);
            result.Dump();
        }
    }
}
