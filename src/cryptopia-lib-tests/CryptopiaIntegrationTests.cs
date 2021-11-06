using cryptopia_lib;
using cryptopia_lib.Models;
using dump_lib;
using log_lib;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using test_shared;
using cache_lib.Models;
using trade_model;
using trade_node_integration;
using trade_res;
using web_util;
using config_client_lib;

namespace cryptopia_lib_tests
{
    [TestClass]
    public class CryptopiaIntegrationTests
    {
        private CryptopiaIntegration _cryptopia;

        [TestInitialize]
        public void Setup()
        {
            var webUtil = new WebUtil();
            var configClient = new ConfigClient();
            var log = new Mock<ILogRepo>();
            var node = new TradeNodeUtil(configClient, webUtil, log.Object);

            _cryptopia = new CryptopiaIntegration(configClient, node, webUtil, log.Object);
        }

        [TestMethod]
        public void Cryptopia__get_withdrawal_fees()
        {
            _cryptopia.GetWithdrawalFees(CachePolicy.ForceRefresh).Dump();
        }

        [TestMethod]
        public void Cryptopia__get_trading_pairs__force_refresh()
        {
            _cryptopia.GetTradingPairs(CachePolicy.ForceRefresh).Dump();
        }

        [TestMethod]
        public void Cryptopia__get_trading_pairs__allow_cache()
        {
            _cryptopia.GetTradingPairs(CachePolicy.AllowCache).Dump();
        }

        [TestMethod]
        public void Cryptopia__get_trading_pairs__only_use_cache_unless_empty()
        {
            _cryptopia.GetTradingPairs(CachePolicy.OnlyUseCacheUnlessEmpty).Dump();
        }

        [TestMethod]
        public void Cryptopia__get_order_book__force_refresh()
        {
            _cryptopia.GetOrderBook(new TradingPair("BPL", "BTC"), CachePolicy.ForceRefresh).Dump();
        }
        
        [TestMethod]
        public void Cryptopia__get_btb_btc_order_book__force_refresh()
        {
            _cryptopia.GetOrderBook(new TradingPair("BTB", "BTC"), CachePolicy.ForceRefresh).Dump();
        }

        [TestMethod]
        public void Cryptopia__get_btb_btc_order_book__only_use_cache_unless_empty()
        {
            _cryptopia.GetOrderBook(new TradingPair("BTB", "BTC"), CachePolicy.OnlyUseCacheUnlessEmpty).Dump();
        }

        [TestMethod]
        public void Cryptopia__get_zen_btc_order_book__force_refresh()
        {
            _cryptopia.GetOrderBook(new TradingPair("ZEN", "BTC"), CachePolicy.ForceRefresh).Dump();
        }

        [TestMethod]
        public void Cryptopia__get_vit_btc_order_book__force_refresh()
        {
            _cryptopia.GetOrderBook(new TradingPair("VIT", "BTC"), CachePolicy.ForceRefresh).Dump();
        }

        [TestMethod]
        public void Cryptopia__get_commodities__force_refresh()
        {
            _cryptopia.GetCommodities(CachePolicy.ForceRefresh).Dump();
        }

        [TestMethod]
        public void Cryptopia__get_native_currencies()
        {
            var result = _cryptopia.GetNativeCurrencies(CachePolicy.OnlyUseCacheUnlessEmpty);
            result.Dump();
        }

        [TestMethod]
        public void Cryptopia__get_holdings__only_use_cache_unless_empty()
        {
            _cryptopia.GetHoldings(CachePolicy.OnlyUseCacheUnlessEmpty).Dump();
        }

        [TestMethod]
        public void Cryptopia__get_blz_commodity()
        {
            var commodities = _cryptopia.GetCommodities(CachePolicy.AllowCache);
            var blz = commodities.SingleOrDefault(item => string.Equals("BLZ", item.NativeSymbol, StringComparison.InvariantCultureIgnoreCase));
            blz.Dump();
        }

        [TestMethod]
        public void Cryptopia__get_coin_status()
        {
            var results = _cryptopia.GetNativeCoinStatuses(CachePolicy.AllowCache);
            "Listing Statuses:".Dump();
            results.aaData.Select(item => item.ListingStatusText).Distinct().Dump();

            "Wallet Statuses:".Dump();
            results.aaData.Select(item => item.WalletStatusText).Distinct().Dump();
            results.Dump();
        }

        [TestMethod]
        public void Cryptopia__generate_map()
        {
            var native = _cryptopia.GetNativeCurrencies(CachePolicy.OnlyUseCacheUnlessEmpty);
            var getNative = new Func<string, CryptopiaCurrenciesPayloadItem>(nativeSymbol => native.Data.SingleOrDefault(item => string.Equals(item.Symbol, nativeSymbol)));

            MapCommodity(getNative("ETH"), CommodityRes.Eth);
            MapCommodity(getNative("BTC"), CommodityRes.Bitcoin);
            MapCommodity(getNative("ADA"), CommodityRes.Cardano);
            MapCommodity(getNative("ARK"), CommodityRes.Ark);
            MapCommodity(getNative("BLZ"), CommodityRes.BlazeCoin);
        }

        private void MapCommodity(CryptopiaCurrenciesPayloadItem native, Commodity canon)
        {
            if (native == null) { throw new ArgumentNullException(nameof(native)); }
            if (native.Id == default(long)) { throw new ArgumentNullException(nameof(native.Id)); }
            if (string.IsNullOrWhiteSpace(native.Symbol)) { throw new ArgumentNullException(nameof(native.Symbol)); }
            if (string.IsNullOrWhiteSpace(native.Name)) { throw new ArgumentNullException(nameof(native.Name)); }

            if (canon == null) { throw new ArgumentNullException(nameof(canon)); }
            if (canon.Id == default(Guid)) { throw new ArgumentNullException(nameof(canon.Id)); }
            if (string.IsNullOrWhiteSpace(canon.Symbol)) { throw new ArgumentNullException(nameof(canon.Symbol)); }
            if (string.IsNullOrWhiteSpace(canon.Name)) { throw new ArgumentNullException(nameof(canon.Name)); }

            const string FileName = @"C:\repo\trade-ex\cryptopia-lib\res\cryptopia-map.json";
            var contents = File.Exists(FileName) ? File.ReadAllText(FileName) : null;
            var map = !string.IsNullOrWhiteSpace(contents)
                ? (JsonConvert.DeserializeObject<List<CryptopiaCommodityMapItem>>(contents) ?? new List<CryptopiaCommodityMapItem>())
                : new List<CryptopiaCommodityMapItem>();

            var existingCanonMatch = map.SingleOrDefault(item => item.CanonicalId == canon.Id);
            if (existingCanonMatch != null) { return; }

            var existingNativeMatch = map.SingleOrDefault(item => item.NativeId == native.Id);
            if (existingNativeMatch != null) { return; }

            var mapItem = new CryptopiaCommodityMapItem
            {
                CanonicalId = canon.Id,
                CanonicalSymbol = canon.Symbol,
                CanonicalName = canon.Name,
                NativeId = native.Id,
                NativeSymbol = native.Symbol,
                NativeName = native.Name
            };

            map.Add(mapItem);

            var updatedContents = JsonConvert.SerializeObject(map, Formatting.Indented);
            File.WriteAllText(FileName, updatedContents);
        }
    }
}
