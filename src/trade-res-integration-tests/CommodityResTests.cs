using System;
using System.Diagnostics;
using dump_lib;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using trade_res;

namespace trade_res_integration_tests
{
    [TestClass]
    public class CommodityResTests
    {
        [TestMethod]
        public void Commodity_res__all()
        {
            var all = CommodityRes.All;
            var allAgain = CommodityRes.All;

            all.Dump();
        }

        [TestMethod]
        public void Commodity_res__ambrosous()
        {
            var stopWatchA = new Stopwatch();
            stopWatchA.Start();
            CommodityRes.Ambrosous.Dump();
            stopWatchA.Stop();
            stopWatchA.ElapsedMilliseconds.Dump();

            var stopWatchB = new Stopwatch();
            stopWatchB.Start();
            CommodityRes.Ambrosous.Dump();
            stopWatchB.Stop();
            stopWatchB.ElapsedMilliseconds.Dump();
        }
    }
}
