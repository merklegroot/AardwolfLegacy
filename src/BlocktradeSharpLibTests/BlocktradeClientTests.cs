using System;
using BlocktradeExchangeLib;
using dump_lib;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BlocktradeSharpLibTests
{
    [TestClass]
    public class BlocktradeClientTests
    {
        private BlocktradeClient _blocktradeClient;

        [TestInitialize]
        public void Setup()
        {
            _blocktradeClient = new BlocktradeClient();
        }

        [TestMethod]
        public void Blocktrade_client__get_trading_assets_raw()
        {
            var results = _blocktradeClient.GetTradingAssetsRaw();
            results.Dump();
        }

        [TestMethod]
        public void Blocktrade_client__get_trading_pairs_raw()
        {
            var results = _blocktradeClient.GetTradingPairsRaw();
            results.Dump();
        }
    }
}
