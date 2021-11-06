using Microsoft.VisualStudio.TestTools.UnitTesting;
using web_util;
using trade_model;
using log_lib;
using Moq;
using binance_lib;
using System.Linq;
using trade_lib;
using Shouldly;
using dump_lib;
using trade_node_integration;
using System;
using parse_lib;
using System.Threading;
using cryptocompare_lib;
using System.Threading.Tasks;
using System.Collections.Generic;
using trade_res;
using cache_lib.Models;
using config_client_lib;
using math_lib;
using System.IO;
using Newtonsoft.Json;

namespace binance_lib_integration_tests
{
    [TestClass]
    public class BinanceExchangeIntegrationTests
    {
        private BinanceIntegration _binance;
        private CryptoCompareIntegration _cryptoCompare;

        [TestInitialize]
        public void Setup()
        {
            var configClient = new ConfigClient();

            var webUtil = new WebUtil();
            var log = new Mock<ILogRepo>();
            var nodeUtil = new TradeNodeUtil(configClient, webUtil, log.Object);

            _cryptoCompare = new CryptoCompareIntegration(webUtil, configClient);
            _binance = new BinanceIntegration(webUtil, configClient, nodeUtil, log.Object);            
        }

        [TestMethod]
        public void Binance__get_my_trade_history()
        {
            var results = _binance.GetUserTradeHistory(CachePolicy.ForceRefresh);            
            results.Dump();
        }

        [TestMethod]
        public void Binance__get_my_trade_history__only_use_cache_unless_empty()
        {
            var results = _binance.GetUserTradeHistory(CachePolicy.OnlyUseCacheUnlessEmpty);
            results.Dump();
        }

        [TestMethod]
        public void Binance__get_my_trade_history__only_use_cache()
        {
            var results = _binance.GetUserTradeHistory(CachePolicy.OnlyUseCache);
            results.Dump();
        }

        [TestMethod]
        public void Binance__get_withdrawal_history__only_use_cache_unless_empty()
        {
            var results = _binance.GetUserTradeHistory(CachePolicy.OnlyUseCacheUnlessEmpty);
            results.Where(item => item.TradeType == TradeTypeEnum.Withdraw).ToList()
                .Dump();
            // results.Dump();
        }

        [TestMethod]
        public void Binance__get_native_withdrawal_history__only_use_cache_unless_empty()
        {
            var results = _binance.GetNativeWithdrawalHistory(CachePolicy.OnlyUseCacheUnlessEmpty);
            results.Dump();
        }

        [TestMethod]
        public void Binance__get_native_withdrawal_history__force_refresh()
        {
            var results = _binance.GetNativeWithdrawalHistory(CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Binance__get_ccxt_withdrawal_history__only_use_cache_unless_empty()
        {
            var results = _binance.GetCcxtWithdrawalHistory(CachePolicy.OnlyUseCacheUnlessEmpty);
            results.Dump();
        }

        [TestMethod]
        public void Binance__get_my_trade_history__bluzelle_eth()
        {
            var results = _binance.GetNativeUserTradeHistory(new TradingPair("BLZ", "ETH"), CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Binance__get_my_trade_history__ark_eth()
        {
            var results = _binance.GetNativeUserTradeHistory(new TradingPair("ARK", "ETH"), CachePolicy.ForceRefresh);
            results.OrderByDescending(item => item.Time).Take(3).ToList().Dump();
            // results.Dump();
        }

        [TestMethod]
        public void Binance__get_my_trade_history__knc_eth()
        {
            var results = _binance.GetNativeUserTradeHistory(new TradingPair("KNC", "ETH"), CachePolicy.ForceRefresh);
            results.OrderByDescending(item => item.Time).Take(3).ToList().Dump();
            results.Dump();
        }

        [TestMethod]
        public void Binance__get_my_trade_history__knc_btc()
        {
            var results = _binance.GetNativeUserTradeHistory(new TradingPair("KNC", "BTC"), CachePolicy.ForceRefresh);
            results.OrderByDescending(item => item.Time).Take(3).ToList().Dump();
            results.Dump();
        }

        [TestMethod]
        public void Binance__get_holdings__force_refresh()
        {
            var results = _binance.GetHoldings(CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Binance__get_holdings__only_use_cache_unless_empty()
        {
            var results = _binance.GetHoldings(CachePolicy.OnlyUseCacheUnlessEmpty);
            results.Dump();
        }

        [TestMethod]
        public void Binance__get_open_orders__ark_eth__force_refresh()
        {
            var result = _binance.GetOpenOrdersForTradingPairV2("ARK", "ETH", CachePolicy.ForceRefresh);
            result.Dump();
        }

        [TestMethod]
        public void Binance__get_open_orders__tusd_btc__force_refresh()
        {
            var result = _binance.GetOpenOrdersForTradingPairV2("TUSD", "BTC", CachePolicy.ForceRefresh);
            result.Dump();
        }

        [TestMethod]
        public void Binance__get_order_book__ark_eth__force_refresh()
        {
            var result = _binance.GetOrderBook(new TradingPair("ARK", "ETH"), CachePolicy.ForceRefresh);
            result.Dump();
        }

        [TestMethod]
        public void Binance__get_order_book__zen_btc__force_refresh()
        {
            var result = _binance.GetOrderBook(new TradingPair("ZEN", "BTC"), CachePolicy.ForceRefresh);
            result.Dump();
        }

        [TestMethod]
        public void Binance__get_order_book__snm_btc__force_refresh()
        {
            var result = _binance.GetOrderBook(new TradingPair("SNM", "BTC"), CachePolicy.ForceRefresh);
            result.Dump();
        }

        [TestMethod]
        public void Binance__get_order_book__snm_eth__force_refresh()
        {
            var result = _binance.GetOrderBook(new TradingPair("SNM", "ETH"), CachePolicy.ForceRefresh);
            result.Dump();
        }

        [TestMethod]
        public void Binance__get_invalid_order_book__cache_only()
        {
            var result = _binance.GetOrderBook(new TradingPair("ASDF", "ETH"), CachePolicy.OnlyUseCache);
            result.Dump();
        }

        [TestMethod]
        public void Binance__get_disabled_trading_pair__cache_only()
        {
            var result = _binance.GetOrderBook(new TradingPair("AE", "BNB"), CachePolicy.OnlyUseCache);
            result.Dump();
        }

        [TestMethod]
        public void Binance__get_order_book__enj_eth__cache_only()
        {
            var result = _binance.GetOrderBook(new TradingPair("ENJ", "ETH"), CachePolicy.OnlyUseCache);
            result.Dump();
        }

        [TestMethod]
        public void Binance__get_order_book__allow_cache()
        {
            var result = _binance.GetOrderBook(new TradingPair("ENJ", "ETH"), CachePolicy.AllowCache);
            result.Dump();
        }

        [TestMethod]
        public void Binance__update_group_snap_shot()
        {
            _binance.GetOrderBook(new TradingPair("ENJ", "ETH"), CachePolicy.ForceRefresh);
            _binance.GetOrderBook(new TradingPair("ICX", "ETH"), CachePolicy.ForceRefresh);
            _binance.GetOrderBook(new TradingPair("OMG", "BTC"), CachePolicy.ForceRefresh);
            _binance.GetOrderBook(new TradingPair("OMG", "ETH"), CachePolicy.ForceRefresh);

            _binance.UpdateGroupSnapShot();
        }

        [TestMethod]
        public void Binance__get_exchange_info__only_use_cache_unless_empty()
        {
            var results = _binance.GetExchangeInfo(CachePolicy.OnlyUseCacheUnlessEmpty);
            results.Dump();
        }

        [TestMethod]
        public void Binance__get_exchange_info__only_use_cache()
        {
            var results = _binance.GetExchangeInfo(CachePolicy.OnlyUseCache);
            results.Dump();
        }

        [TestMethod]
        public void Binance__limit_decimals()
        {
            var poeLimit = _binance.LimitDecimals(new NativeTradingPair("POE", "ETH"), 1.234567890123m);
            poeLimit.ShouldBe(1);

            var zenLimit = _binance.LimitDecimals(new NativeTradingPair("ZEN", "BTC"), 1.234567890123m);
            zenLimit.ShouldBe(1.234m);
        }

        [TestMethod]
        public void Binance__get_trading_pairs__force_refresh()
        {
            var result = _binance.GetTradingPairs(CachePolicy.ForceRefresh);
            var ve = result.Where(item => item.Symbol.ToUpper().StartsWith("VE")).ToList();

            result.Dump();
        }

        [TestMethod]
        public void Binance__get_trading_pairs__only_use_cache_unless_empty()
        {
            var result = _binance.GetTradingPairs(CachePolicy.OnlyUseCacheUnlessEmpty);
            result.Dump();
        }

        [TestMethod]
        public void Binance__get_trading_pairs__get_omg_eth_lot_size__only_use_cache_unless_empty()
        {
            const string Symbol = "OMG";
            const string BaseSymbol = "ETH";

            var result = _binance.GetTradingPairs(CachePolicy.OnlyUseCacheUnlessEmpty);
            var tp = result.Single(item =>
                string.Equals(item.Symbol, Symbol, StringComparison.InvariantCultureIgnoreCase)
                && string.Equals(item.BaseSymbol, BaseSymbol, StringComparison.InvariantCultureIgnoreCase));

            tp.Dump();
        }

        [TestMethod]
        public void Binance__get_native_deposit_history__force_refresh()
        {
            var result = _binance.GetNativeDepositHistory(CachePolicy.ForceRefresh);
            result.Dump();
        }

        [TestMethod]
        public void Binance__get_native_deposit_history__only_use_cache_unless_empty()
        {
            var result = _binance.GetNativeDepositHistory(CachePolicy.OnlyUseCacheUnlessEmpty);

            result.Dump();
        }

        [TestMethod]
        public void Binance__get_native_deposit_history__ark__only_use_cache_unless_empty()
        {
            var result = _binance.GetNativeDepositHistory(CachePolicy.OnlyUseCacheUnlessEmpty);
            var arkItems = result.Data.List.Where(item => string.Equals(item.Asset, "ARK", StringComparison.InvariantCultureIgnoreCase))
                .ToList();

            arkItems.Dump();            
        }

        [TestMethod]
        public void Binance__refresh_withdrawal_history()
        {
            var result = _binance.GetNativeWithdrawalHistory(CachePolicy.ForceRefresh);
            result.Dump();
        }

        [TestMethod]
        public void Binance__refresh_ark_history()
        {
            var arkEth = _binance.GetNativeUserTradeHistory(new TradingPair("ARK", "ETH"), CachePolicy.ForceRefresh);
            arkEth.Dump();

            var arkBtc = _binance.GetNativeUserTradeHistory(new TradingPair("ARK", "BTC"), CachePolicy.ForceRefresh);
            arkBtc.Dump();
        }

        [TestMethod]
        public void Binance__get_bitcoin_cash_trading_pairs()
        {
            var tradingPairs = _binance.GetTradingPairs(CachePolicy.ForceRefresh);
            var bitcoinCashPairs = tradingPairs.Where(item => string.Equals(item.Symbol, "BCH")).ToList();
            bitcoinCashPairs.Dump();
        }

        [TestMethod]
        public void Binance__get_commodities__bitcoin_cash()
        {
            var commodities = _binance.GetCommodities(CachePolicy.ForceRefresh);
            var bitcoinCashCommodity = commodities.Single(item => 
                string.Equals(item.NativeSymbol, "BCC") || string.Equals(item.NativeSymbol, "BCH"));
            bitcoinCashCommodity.Dump();
        }

        [TestMethod]
        public void Binance__get_commodities()
        {
            _binance.GetCommodities().Dump();
        }

        [TestMethod]
        public void Binance__get_withdrawal_fees()
        {
            _binance.GetWithdrawalFees(CachePolicy.ForceRefresh).Dump();
        }

        [TestMethod]
        public void Binance__get_deposit_addresses__force_refresh()
        {
            var addresses = _binance.GetDepositAddresses(CachePolicy.ForceRefresh);
            addresses.Dump();
        }

        [TestMethod]
        public void Binance__get_bcd_deposit_address()
        {
            var results = _binance.GetDepositAddress("BCD", CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Binance__get_bcc_deposit_address()
        {
            var results = _binance.GetDepositAddress("BCC", CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Binance__get_bch_deposit_address()
        {
            var results = _binance.GetDepositAddress("BCH", CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Binance__get_dnt_deposit_address()
        {
            var results = _binance.GetDepositAddress("DNT", CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Binance__get_eth_deposit_address()
        {
            var result = _binance.GetDepositAddress("ETH", CachePolicy.ForceRefresh);
            result.Dump();
        }

        [TestMethod]
        public void Binance__get_snm_deposit_address()
        {
            var result = _binance.GetDepositAddress("SNM", CachePolicy.ForceRefresh);
            result.Dump();
        }

        [TestMethod]
        public void Binance__purchase_dash_eth_market()
        {
            bool shouldRun = false;

            // this test will actually make a purchase with real money.
            // prevent this from accidentally being run.
            if (!shouldRun)
            {
                Assert.Inconclusive("This test must be run manually since it makes a real purchase.");
                return;
            }

            _binance.BuyMarket(new TradingPair("DASH", "ETH"), 0.23m);
        }

        [TestMethod]
        public void Binance__purchase_ark_eth_market()
        {
            bool shouldRun = false;

            // this test will actually make a purchase with real money.
            // prevent this from accidentally being run.
            if (!shouldRun)
            {
                Assert.Inconclusive("This test must be run manually since it makes a real purchase.");
                return;
            }

            _binance.BuyMarket(new TradingPair("ARK", "ETH"), 57.0m);
        }

        [TestMethod]
        public void Binance__puchase_ven_btc_limit()
        {
            bool shouldRun = false;

            // this test will actually make a purchase with real money.
            // prevent this from accidentally being run.
            if (!shouldRun)
            {
                Assert.Inconclusive("This test must be run manually since it makes a real purchase.");
                return;
            }

            _binance.BuyLimit(new TradingPair("VEN", "BTC"), new QuantityAndPrice { Quantity = 50, Price = 0.000468m });
        }

        [TestMethod]
        public void Binance__purchase_bnt_eth_market()
        {
            bool shouldRun = false;

            // this test will actually make a purchase with real money.
            // prevent this from accidentally being run.
            if (!shouldRun)
            {
                Assert.Inconclusive("This test must be run manually since it makes a real purchase.");
                return;
            }

            _binance.BuyMarket(new TradingPair("BNT", "ETH"), 110);
        }

        [TestMethod]
        public void Binance__cancel_all_open_orders_for_ark(string symbol)
        {
            CancelAllOpenOrdersForSymbol("ARK");
        }
        
        private void CancelAllOpenOrdersForSymbol(string symbol)
        {
            var binanceSymbols = _binance.GetBinanceTradingPairsForSymbol(symbol, CachePolicy.AllowCache);
            if (binanceSymbols == null || !binanceSymbols.Any()) { return; }

            foreach (var binanceSymbol in binanceSymbols)
            {
                var tradingPair = new TradingPair(binanceSymbol.BaseAsset, binanceSymbol.QuoteAsset);
                _binance.CancelAllOpenOrdersForTradingPair(tradingPair);
                Thread.Sleep(TimeSpan.FromSeconds(2.5));
            }
        }

        [TestMethod]
        public void Binance__cancel_open_order__tusd_btc()
        {
            var openOrdersWithAsOf = _binance.GetOpenOrdersForTradingPairV2("TUSD", "BTC", CachePolicy.ForceRefresh);
            if (openOrdersWithAsOf?.OpenOrders == null || !openOrdersWithAsOf.OpenOrders.Any())
            {
                Assert.Inconclusive("There are no open orders to cancel.");
            }

            foreach (var openOrder in openOrdersWithAsOf.OpenOrders)
            {
                _binance.CancelOrder(openOrder.OrderId);
            }
        }

        [TestMethod]
        public void Binance__sell_all_ark_optimized()
        {
            Sell_all_optimized("ARK");
        }

        [TestMethod]
        public void Binance__sell_all_ark_for_eth_slowly()
        {
            Sell_all_slowly(new TradingPair("ARK", "ETH"));
        }

        [TestMethod]
        public void Binance__sell_all_bnt_for_eth_slowly()
        {
            Sell_all_slowly(new TradingPair("BNT", "ETH"));
        }

        [TestMethod]
        public void Binance__sell_all_link__slowly__mixed()
        {
            Sell_all_slowly("LINK");
        }

        [TestMethod]
        public void Binance__sell_all_wtc_optimized()
        {
            Sell_all_optimized("WTC");
        }

        [TestMethod]
        public void Binance__sell_all_ven_slowly()
        {
            Sell_all_optimized("VEN");
        }

        [TestMethod]
        public void Binance__sell_all_wtc_mixed_slowly()
        {
            Sell_all_slowly("WTC");
        }

        [TestMethod]
        public void Binance__sell_all_sub_optimized()
        {
            Sell_all_optimized("SUB");
        }

        [TestMethod]
        public void Binance__sell_all_req_for_eth_slowly()
        {
            Sell_all_slowly(new TradingPair("REQ", "ETH"));
        }

        [TestMethod]
        public void Binance__sell_all_zen_for_eth_and_btc_slowly()
        {
            Sell_all_slowly("ZEN", new List<string> { "ETH", "BTC" });
        }

        [TestMethod]
        public void Binance__sell_all_sub_for_eth_slowly()
        {
            Sell_all_slowly(new TradingPair("SUB", "ETH"));
        }

        [TestMethod]
        public void Binance__sell_all_dlt_for_eth_slowly()
        {
            Sell_all_slowly(new TradingPair("DLT", "ETH"));
        }

        [TestMethod]
        public void Binance__sell_all_dnt_for_eth_slowly()
        {
            Sell_all_slowly(new TradingPair("DNT", "ETH"));
        }

        [TestMethod]
        public void Binance__sell_all_bch_for_btc_slowly()
        {
            Sell_all_slowly(new TradingPair("BCH", "BTC"));
        }

        [TestMethod]
        public void Binance__buy_all_bch_for_btc_slowly()
        {
            const decimal StepSize = 0.000001m;
            var tradingPair = new TradingPair("BCH", "BTC");

            var holdings = _binance.GetHoldings(CachePolicy.ForceRefresh);
            var btcAvailalbe = holdings.GetAvailableForSymbol("BTC");
            var orderBook = _binance.GetOrderBook(tradingPair, CachePolicy.ForceRefresh);
            var bestBidPrice = orderBook.BestBid().Price;
            var ourPrice = bestBidPrice + StepSize;
            var quantity = 0.9m * btcAvailalbe / bestBidPrice;

            _binance.SellLimit(tradingPair, new QuantityAndPrice
            {
                Quantity = quantity,
                Price = ourPrice
            });
        }

        [TestMethod]
        public void Buy_poe_slowly()
        {
            const decimal DesiredQuantity = 100;

            var tradingPairs = _binance.GetTradingPairs(CachePolicy.ForceRefresh);
            var poeEthTradingPair = tradingPairs.Single(item =>
                string.Equals(item.Symbol, "POE", StringComparison.InvariantCultureIgnoreCase)
                && string.Equals(item.Symbol, "ETH", StringComparison.InvariantCultureIgnoreCase));
            

            var poeEthOrderBook = _binance.GetOrderBook(new TradingPair("POE", "ETH"), CachePolicy.ForceRefresh);
        }

        [TestMethod]
        public void Binance__sell_all_bch_for_eth_slowly()
        {
            Sell_all_slowly(new TradingPair("BCH", "ETH"));
        }

        [TestMethod]
        public void Binance__sell_all_cvc_for_eth_slowly()
        {
            Sell_all_slowly(new TradingPair("CVC", "ETH"));
        }

        [TestMethod]
        public void Binance__sell_all_cvc_for_btc_slowly()
        {
            Sell_all_slowly(new TradingPair("CVC", "BTC"));
        }

        [TestMethod]
        public void Binance__sell_all_cvc_mixed_slowly()
        {
            Sell_all_slowly(CommodityRes.Civic);
        }

        [TestMethod]
        public void Binance__sell_all_xem_mixed_slowly()
        {
            Sell_all_slowly(CommodityRes.NewEconomyMovement, new List<string> { "ETH", "BTC" });
        }

        [TestMethod]
        public void Binance__sell_all_omg_mixed_slowly()
        {
            Sell_all_slowly("OMG");
        }

        [TestMethod]
        public void Binance__sell_all_omg_for_eth_slowly()
        {
            Sell_all_slowly(new TradingPair("OMG", "ETH"));
        }

        [TestMethod]
        public void Binance__sell_all_snm_for_BTC_slowly()
        {
            Sell_all_slowly(new TradingPair("SNM", "BTC"));
        }

        [TestMethod]
        public void Binance__sell_all_snm_for_ETH_and_BTC_slowly()
        {
            Sell_all_slowly("SNM", new List<string> { "ETH", "BTC" });
        }

        [TestMethod]
        public void Binance__sell_all_blz_for_BTC_slowly()
        {
            Sell_all_slowly(new TradingPair("BLZ", "BTC"));
        }

        [TestMethod]
        public void Binance__sell_all_blz__mixed__slowly()
        {
            Sell_all_slowly(CommodityRes.Bluzelle.Symbol);
        }

        [TestMethod]
        public void Binance__sell_all_dash___eth_btc__slowly()
        {
            Sell_all_slowly(CommodityRes.Dash, new List<string> { "ETH", "BTC" });
        }

        [TestMethod]
        public void Binance__sell_all_ncash_optimized()
        {
            Sell_all_optimized("NCASH");
        }

        [TestMethod]
        public void Binance__get_cached_order_books()
        {
            var results = _binance.GetCachedOrderBooks();
            results.Dump();
        }

        [TestMethod]
        public void Binance__get_cached_order_books__snm_eth()
        {
            var results = _binance.GetCachedOrderBooks();
            var match = results.Where(item => 
                string.Equals(item.Symbol, "SNM", StringComparison.InvariantCultureIgnoreCase)
                && string.Equals(item.BaseSymbol, "ETH", StringComparison.InvariantCultureIgnoreCase))
                .SingleOrDefault();

            match.Dump();
        }

        [TestMethod]
        public void Binance__get_trade_history()
        {
            var results = _binance.GetUserTradeHistory(CachePolicy.AllowCache);
            results.Dump();
        }

        private void Sell_all_slowly(Commodity commodity)
        {
            Sell_all_slowly(commodity.Symbol);
        }

        private void Sell_all_slowly(string symbol)
        {
            Sell_all_slowly(symbol, new List<string> { "ETH", "BTC" });
        }

        private void Sell_all_slowly(TradingPair tradingPair)
        {
            Sell_all_slowly(tradingPair.Symbol, new List<string> { tradingPair.BaseSymbol });
        }

        private void Sell_all_slowly(Commodity commodity, List<string> baseSymbols)
        {
            Sell_all_slowly(commodity.Symbol, baseSymbols);
        }

        private void Sell_all_slowly(string symbol, List<string> baseSymbols)
        {
            // 0.06903
            // 0.12345
            // 0.00001

            // const decimal PriceTick = 0.00000001m;

            var usdValueTask = Task.Run(() => _cryptoCompare.GetUsdValue(symbol, CachePolicy.ForceRefresh));

            var tradingPairsWithBinanceSymbols = baseSymbols.Select(queryBaseSymbol =>
            {
                var tradingPair = new TradingPair(symbol, queryBaseSymbol);
                return new
                {
                    TradingPair = tradingPair,
                    BinanceSymbol = _binance.GetBinanceTradingPairFromCanonicalTradingPair(tradingPair, CachePolicy.ForceRefresh)
                };
            }).ToList();
            var usdValue = usdValueTask.Result.Value;

            while (true)
            {
                foreach (var combo in tradingPairsWithBinanceSymbols)
                {
                    var openOrders = _binance.GetOpenOrdersForTradingPairV2(symbol, combo.TradingPair.BaseSymbol, CachePolicy.ForceRefresh);
                    foreach (var openOrder in openOrders?.OpenOrders ?? new List<OpenOrder>())
                    {
                        _binance.CancelOrder(openOrder.OrderId);
                    }

                    var tradingPair = combo.TradingPair;
                    var binanceSymbol = combo.BinanceSymbol;

                    var filterNames = binanceSymbol?.Filters?.Select(item => item.filterType).Distinct().ToList();
                    var lotSizeFilter = binanceSymbol?.Filters?.FirstOrDefault(queryFilter => string.Equals(queryFilter?.filterType, "LOT_SIZE", StringComparison.InvariantCultureIgnoreCase));
                    var priceFilter = binanceSymbol?.Filters?.FirstOrDefault(queryFilter => string.Equals(queryFilter?.filterType, "PRICE_FILTER", StringComparison.InvariantCultureIgnoreCase));
                    var minNotionalFilter = binanceSymbol?.Filters?.FirstOrDefault(queryFilter => string.Equals(queryFilter?.filterType, "MIN_NOTIONAL", StringComparison.InvariantCultureIgnoreCase));
                    var minNotionalText = minNotionalFilter?.minNotional;
                    decimal? minNotional = ParseUtil.DecimalTryParse(minNotionalText);

                    var priceTickSizeText = priceFilter?.tickSize;
                    double priceTickSize = ParseUtil.DoubleTryParse(priceTickSizeText).Value;

                    var lotStepSizeText = lotSizeFilter?.stepSize;
                    double? lotStepSize = ParseUtil.DoubleTryParse(lotStepSizeText);

                    if (!lotStepSize.HasValue)
                    {
                        Console.WriteLine($"Couldn't retrieve the lot size for {tradingPair.Symbol}.");
                        return;
                    }

                    var lotPrecision = -((int)Math.Log10(lotStepSize.Value));
                    var pricePrecision = -((int)Math.Log10(priceTickSize));

                    var lotFactor = (decimal)Math.Pow(10.0d, lotPrecision);
                    var priceFactor = (decimal)Math.Pow(10.0d, pricePrecision);

                    var holding = _binance.GetHolding(tradingPair.Symbol, CachePolicy.ForceRefresh);

                    var available = holding.Available;
                    if (available < (decimal)lotStepSize.Value)
                    {

                        if (available < (decimal)lotStepSize.Value)
                        {
                            Console.WriteLine($"We own {available} {tradingPair.Symbol}. The step size is {lotStepSize.Value}. Since we own less than the step size, we cannot sell any.");
                            return;
                        }
                        else if (available <= 0)
                        {
                            Console.WriteLine($"We don't have any available {tradingPair.Symbol}.");
                            return;
                        }
                    }

                    Console.WriteLine($"We have {available} {tradingPair.Symbol} available.");

                    var desiredQuantityToSell = MathUtil.ConstrainToMultipleOf(available / tradingPairsWithBinanceSymbols.Count, (decimal)lotStepSize.Value);

                    if (desiredQuantityToSell > available) { desiredQuantityToSell = (decimal)lotStepSize.Value; }
                    var quantityToSell = desiredQuantityToSell <= available
                        ? desiredQuantityToSell
                        : MathUtil.ConstrainToMultipleOf(available, (decimal)lotStepSize.Value);

                    if (quantityToSell <= 0)
                    {
                        Console.WriteLine("We have nothing left to sell.");
                        return;
                    }

                    var orderBook = _binance.GetOrderBook(tradingPair, true);
                    var bestBid = orderBook.BestBid();
                    var bestBidPrice = bestBid.Price;
                    var bestAsk = orderBook.BestAsk();
                    var bestAskPrice = bestAsk.Price;

                    // var priceToAsk = enforcePricePrecision((bestBidPrice + 9.0m * bestAskPrice) / 10.0m);
                    var priceToAsk = bestAskPrice - (decimal)priceTickSize;

                    if (minNotional.HasValue)
                    {
                        if (priceToAsk * quantityToSell < minNotional.Value)
                        {
                            quantityToSell = MathUtil.ConstrainToMultipleOf(available, minNotional.Value);
                            if (priceToAsk * quantityToSell < minNotional.Value)
                            {
                                quantityToSell = MathUtil.ConstrainToMultipleOf(quantityToSell + (decimal)lotStepSize.Value, (decimal)lotStepSize.Value);
                            }
                        }
                    }

                    var orderResult = _binance.SellLimit(tradingPair, new QuantityAndPrice { Quantity = quantityToSell, Price = priceToAsk });
                    Thread.Sleep(TimeSpan.FromSeconds(5));
                }
            }
        }

        private void Sell_all_optimized(string symbol)
        {
            var usdValueTask = Task.Run(() => _cryptoCompare.GetUsdValue(symbol, CachePolicy.ForceRefresh));

            var binanceSymbols = _binance.GetBinanceTradingPairsForSymbol(symbol, CachePolicy.ForceRefresh);
            if (binanceSymbols == null || !binanceSymbols.Any()) { return; }

            var usdValue = usdValueTask.Result.Value;

            var cancelAllOpenOrders = new Action(() =>
            {
                foreach (var binanceSymbol in binanceSymbols)
                {
                    var tradingPair = new TradingPair(binanceSymbol.BaseAsset, binanceSymbol.QuoteAsset);
                    _binance.CancelAllOpenOrdersForTradingPair(tradingPair);
                    Thread.Sleep(TimeSpan.FromSeconds(2.5));
                }
            });

            while (true)
            {
                foreach (var binanceSymbol in binanceSymbols)
                {
                    var tradingPair = new TradingPair(binanceSymbol.BaseAsset, binanceSymbol.QuoteAsset);

                    var filterNames = binanceSymbol?.Filters?.Select(item => item.filterType).Distinct().ToList();
                    var lotSizeFilter = binanceSymbol?.Filters?.FirstOrDefault(queryFilter => string.Equals(queryFilter?.filterType, "LOT_SIZE", StringComparison.InvariantCultureIgnoreCase));
                    var priceFilter = binanceSymbol?.Filters?.FirstOrDefault(queryFilter => string.Equals(queryFilter?.filterType, "PRICE_FILTER", StringComparison.InvariantCultureIgnoreCase));
                    var minNotionalFilter = binanceSymbol?.Filters?.FirstOrDefault(queryFilter => string.Equals(queryFilter?.filterType, "MIN_NOTIONAL", StringComparison.InvariantCultureIgnoreCase));
                    var minNotionalText = minNotionalFilter?.minNotional;
                    decimal? minNotional = ParseUtil.DecimalTryParse(minNotionalText);

                    var priceTickSizeText = priceFilter?.tickSize;
                    double? priceTickSize = ParseUtil.DoubleTryParse(priceTickSizeText);

                    var lotStepSizeText = lotSizeFilter?.stepSize;
                    double? lotStepSize = ParseUtil.DoubleTryParse(lotStepSizeText);

                    var openOrders = _binance.GetCcxtOpenOrders(tradingPair.Symbol, tradingPair.BaseSymbol, CachePolicy.ForceRefresh).Data;

                    try
                    {                       
                        if (openOrders != null && openOrders.Any())
                        {
                            _binance.CancelAllOpenOrdersForTradingPair(tradingPair);
                            Thread.Sleep(TimeSpan.FromSeconds(2.5));
                            openOrders = _binance.GetCcxtOpenOrders(tradingPair.Symbol, tradingPair.BaseSymbol, CachePolicy.ForceRefresh).Data;
                            if (openOrders != null && openOrders.Any())
                            {
                                _binance.CancelAllOpenOrdersForTradingPair(tradingPair);
                            }
                        }
                    }
                    catch(Exception exception)
                    {
                        Console.WriteLine(exception);
                    }

                    if (!lotStepSize.HasValue)
                    {
                        Console.WriteLine($"Couldn't retrieve the lot size for {symbol}.");
                        return;
                    }

                    var lotPrecision = -((int)Math.Log10(lotStepSize.Value));
                    var pricePrecision = -((int)Math.Log10(priceTickSize.Value));

                    var lotFactor = (decimal)Math.Pow(10.0d, lotPrecision);
                    var priceFactor = (decimal)Math.Pow(10.0d, pricePrecision);

                    var holding = _binance.GetHolding(symbol, CachePolicy.ForceRefresh);

                    var available = holding.Available;
                    if (available < (decimal)lotStepSize.Value)
                    {
                        if (holding.InOrders > 0)
                        {
                            cancelAllOpenOrders();
                            Thread.Sleep(TimeSpan.FromSeconds(2.5));
                            holding = _binance.GetHolding(symbol, CachePolicy.ForceRefresh);
                            available = holding.Available;
                        }

                        if (available < (decimal)lotStepSize.Value)
                        {
                            Console.WriteLine($"We own {available} {symbol}. The step size is {lotStepSize.Value}. Since we own less than the step size, we cannot sell any.");
                            return;
                        }
                        else if (available <= 0)
                        {
                            Console.WriteLine($"We don't have any available {symbol}.");
                            return;
                        }
                    }

                    Console.WriteLine($"We have {available} {symbol} available.");                  

                    var enforceLotSize = new Func<decimal, decimal>(x => Math.Truncate(x * lotFactor) / lotFactor);
                    var enforcePricePrecision = new Func<decimal, decimal>(x => Math.Truncate(x * priceFactor) / priceFactor);

                    const decimal DesiredUsdValueToSell = 10.0m;
                    var desiredQuantityToSell = enforceLotSize(DesiredUsdValueToSell / usdValue);

                    if (desiredQuantityToSell > available) { desiredQuantityToSell = (decimal)lotStepSize.Value; }
                    var quantityToSell = desiredQuantityToSell <= available
                        ? desiredQuantityToSell
                        : enforceLotSize(available);

                    if (quantityToSell <= 0)
                    {
                        Console.WriteLine("We have nothing left to sell.");
                        return;
                    }

                    var orderBook = _binance.GetOrderBook(tradingPair, true);
                    var bestBid = orderBook.BestBid();
                    var bestBidPrice = bestBid.Price;
                    var bestAsk = orderBook.BestAsk();
                    var bestAskPrice = bestAsk.Price;

                    var priceToAsk = enforcePricePrecision((bestBidPrice + 9.0m * bestAskPrice) / 10.0m);

                    if (minNotional.HasValue)
                    {
                        if (priceToAsk * quantityToSell < minNotional.Value)
                        {
                            quantityToSell = enforceLotSize(minNotional.Value / priceToAsk);
                            if (priceToAsk * quantityToSell < minNotional.Value)
                            {
                                quantityToSell = enforceLotSize(quantityToSell + (decimal)lotStepSize.Value);
                            }
                        }
                    }

                    _binance.SellLimit(tradingPair, new QuantityAndPrice { Quantity = quantityToSell, Price = priceToAsk });

                    // _binance.SellMarket(tradingPair, quantityToSell);

                    Thread.Sleep(TimeSpan.FromSeconds(5));

                    //available = _binance.GetHolding(symbol, CachePolicy.ForceRefresh).Available;

                    //if (available <= 0 || enforceLotSize(available) <= 0)
                    //{
                    //    Console.WriteLine("We have nothing left to sell.");
                    //    return;
                    //}
                }
            }
        }

    }
}
