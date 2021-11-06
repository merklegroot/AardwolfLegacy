using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using System;
using trade_model;

namespace trade_model_tests
{
    [TestClass]
    public class TradingPairTests
    {
        [TestMethod]
        public void Empty_trading_pairs_should_be_equal()
        {
            var a = new TradingPair();
            var b = new TradingPair();

            a.Equals(b).ShouldBe(true);
        }

        [TestMethod]
        public void Identical_symbols_should_be_equal()
        {
            var a = new TradingPair("SUB", "ETH");
            var b = new TradingPair("SUB", "ETH");

            a.Equals(b).ShouldBe(true);
        }

        [TestMethod]
        public void Different_symbols_should_NOT_be_equal()
        {
            var a = new TradingPair("SUB", "ETH");
            var b = new TradingPair("DUB", "ETH");

            a.Equals(b).ShouldBe(false);
        }

        [TestMethod]
        public void Identical_symbols_with_the_same_ids_SHOULD_be_equal()
        {
            var a = new TradingPair("HAV", "BTC");
            a.CanonicalCommodityId = new Guid("71cab82b-15d8-447a-8b0d-8a6a50665f14");
            var b = new TradingPair("HAV", "BTC");
            b.CanonicalCommodityId = new Guid("71cab82b-15d8-447a-8b0d-8a6a50665f14");

            a.Equals(b).ShouldBe(true);
        }

        [TestMethod]
        public void Different_symbols_with_the_same_ids_SHOULD_be_equal()
        {
            var a = new TradingPair("HAV", "BTC");
            a.CanonicalCommodityId = new Guid("71cab82b-15d8-447a-8b0d-8a6a50665f14");
            var b = new TradingPair("HARV", "BTC");
            b.CanonicalCommodityId = new Guid("71cab82b-15d8-447a-8b0d-8a6a50665f14");

            a.Equals(b).ShouldBe(true);
        }

        [TestMethod]
        public void Identical_symbols_with_the_different_ids_should_NOT_be_equal()
        {
            var a = new TradingPair("HAV", "BTC");
            a.CanonicalCommodityId = new Guid("71cab82b-15d8-447a-8b0d-8a6a50665f14");
            var b = new TradingPair("HAV", "BTC");
            b.CanonicalCommodityId = new Guid("dcc8b804-ea71-46de-9266-5446555215a1");

            a.Equals(b).ShouldBe(false);
        }
    }
}
