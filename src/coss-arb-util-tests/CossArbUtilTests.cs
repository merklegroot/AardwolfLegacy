using System;
using cache_lib.Models;
using config_client_lib;
using coss_arb_lib;
using log_lib;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using workflow_client_lib;
using exchange_client_lib;
using dump_lib;
using trade_res;
using System.Linq;
using log_lib.Models;
using trade_constants;
using static coss_arb_lib.CossArbUtil;

namespace coss_arb_util_tests
{
    [TestClass]
    public class CossArbUtilTests
    {
        private IExchangeClient _exchangeClient;
        private CossArbUtil _cossArbUtil;        

        [TestInitialize]
        public void Setup()
        {
            var log = GenerateLog();

            var configClient = new ConfigClient();
            _exchangeClient = new ExchangeClient();
            var workflowClient = new WorkflowClient();

            _cossArbUtil = new CossArbUtil(configClient, _exchangeClient, workflowClient, log);
        }

        [TestMethod]
        public void Coss_arb_util__auto_sell__force_refresh()
        {
            bool shouldRun = false;
            if (!shouldRun)
            {
                throw new ApplicationException("This test works with real funds and must be run manually.");
            }

            _cossArbUtil.AutoSell();
        }

        [TestMethod]
        public void Coss_arb_util__auto_buy__force_refresh()
        {
            bool shouldRun = false;
            if (!shouldRun)
            {
                throw new ApplicationException("This test works with real funds and must be run manually.");
            }

            _cossArbUtil.AutoBuy(CachePolicy.ForceRefresh);
        }

        [TestMethod]
        public void Coss_arb_util__auto_buy__req_eth__force_refresh()
        {
            bool shouldRun = false;
            if (!shouldRun)
            {
                throw new ApplicationException("This test works with real funds and must be run manually.");
            }

            _cossArbUtil.AutoBuy("REQ", "ETH", CachePolicy.ForceRefresh);
        }

        [TestMethod]
        public void Coss_arb_util__open_bid()
        {
            bool shouldActuallyRun = false;
            if (!shouldActuallyRun)
            {
                throw new ApplicationException("This test works with real funds and must be run manually.");
            }

            _cossArbUtil.OpenBid();
        }

        [TestMethod]
        public void Coss_arb_util__auto_eth_btc_v2()
        {
            _cossArbUtil.AutoEthBtcV2();
        }

        [TestMethod]
        public void Coss_arb_util__auto_eth_gusd()
        {
            _cossArbUtil.AutoEthGusd();
        }

        [TestMethod]
        public void Coss_arb_util__auto_eth_usdc()
        {
            _cossArbUtil.AutoEthUsdc();
        }

        [TestMethod]
        public void Coss_arb_util__auto_btc_gusd()
        {
            _cossArbUtil.AutoBtcGusd();
        }

        [TestMethod]
        public void Coss_arb_util__auto_btc_usdc()
        {
            _cossArbUtil.AutoBtcUsdc();
        }

        [TestMethod]
        public void Coss_arb_util__auto_fyn()
        {
            bool shouldRun = false;
            if (!shouldRun)
            {
                throw new ApplicationException("This test works with real funds and must be run manually.");
            }

            const string Symbol = "FYN";
            const string CompExchange = IntegrationNameRes.HitBtc;
            _cossArbUtil.AutoSymbol(Symbol, CompExchange);
        }

        [TestMethod]
        public void Coss_arb_util__auto_pay()
        {
            const string Symbol = "PAY";
            const string CompExchange = IntegrationNameRes.HitBtc;
            _cossArbUtil.AutoSymbol(Symbol, CompExchange);
        }

        [TestMethod]
        public void Coss_arb_util__auto_la()
        {
            bool shouldRun = false;
            if (!shouldRun)
            {
                throw new ApplicationException("This test works with real funds and must be run manually.");
            }

            const string Symbol = "LA";
            const string CompExchange = IntegrationNameRes.HitBtc;
            _cossArbUtil.AutoSymbol(Symbol, CompExchange);
        }

        [TestMethod]
        public void Coss_arb_util__auto_hgt()
        {
            const string Symbol = "HGT";
            const string CompExchange = IntegrationNameRes.HitBtc;
            _cossArbUtil.AutoSymbol(Symbol, CompExchange);
        }

        [TestMethod]
        public void Coss_arb_util__acquire_xem_coss_v5()
        {
            const string Symbol = "XEM";
            _cossArbUtil.AcquireAgainstBinanceSymbolV5(Symbol);
        }

        [TestMethod]
        public void Coss_arb_util__auto_poe()
        {
            const string Symbol = "POE";
            _cossArbUtil.AcquireAgainstBinanceSymbolV5(Symbol);
        }

        [TestMethod]
        public void Coss_arb_util__auto_pix()
        {
            const string Symbol = "PIX";
            const string CompExchange = IntegrationNameRes.HitBtc;
            _cossArbUtil.AutoSymbol(Symbol, CompExchange);
        }

        [TestMethod]
        public void Coss_arb_util__auto_dat()
        {
            const string Symbol = "DAT";
            const string CompExchange = IntegrationNameRes.KuCoin;
            _cossArbUtil.AutoSymbol(Symbol, CompExchange);
        }

        [TestMethod]
        public void Coss_arb_util__auto_gat()
        {
            const string Symbol = "GAT";
            const string CompExchange = IntegrationNameRes.KuCoin;
            _cossArbUtil.AutoSymbol(Symbol, CompExchange);
        }

        [TestMethod]
        public void Coss_arb_util__auto_wish()
        {
            bool shouldRun = false;
            if (!shouldRun)
            {
                throw new ApplicationException("This test works with real funds and must be run manually.");
            }

            const string Symbol = "WISH";
            const string CompExchange = IntegrationNameRes.Cryptopia;
            _cossArbUtil.AutoSymbol(Symbol, CompExchange);
        }


        [TestMethod]
        public void Coss_arb_util__auto_link()
        {
            bool shouldRun = false;
            if (!shouldRun)
            {
                throw new ApplicationException("This test works with real funds and must be run manually.");
            }

            const string Symbol = "LINK";
            const string CompExchange = IntegrationNameRes.Binance;
            _cossArbUtil.AutoSymbol(Symbol, CompExchange);
        }

        [TestMethod]
        public void Coss_arb_util__auto_nox()
        {
            const string Symbol = "NOX";
            const string CompExchange = IntegrationNameRes.Livecoin;
            _cossArbUtil.AutoSymbol(Symbol, CompExchange);
        }

        [TestMethod]
        public void Coss_arb_util__auto_omg()
        {
            const string Symbol = "OMG";
            const string CompExchange = IntegrationNameRes.Binance;
            _cossArbUtil.AutoSymbol(Symbol, CompExchange);
        }

        [TestMethod]
        public void Coss_arb_util__auto_enj()
        {
            bool shouldRun = false;

            if (!shouldRun)
            {
                throw new ApplicationException("This test works with real funds and must be run manually.");
            }

            const string Symbol = "ENJ";
            const string CompExchange = IntegrationNameRes.Binance;
            _cossArbUtil.AutoSymbol(Symbol, CompExchange);
        }

        [TestMethod]
        public void Coss_arb_util__auto_mco()
        {
            const string Symbol = "MCO";
            const string CompExchange = IntegrationNameRes.Binance;
            _cossArbUtil.AutoSymbol(Symbol, CompExchange);
        }

        [TestMethod]
        public void Coss_arb_util__auto_mco_eth()
        {
            const string Symbol = "MCO";
            const string QuoteSymbol = "ETH";
            _cossArbUtil.AutoCossBinance(Symbol, QuoteSymbol);
        }

        [TestMethod]
        public void Coss_arb_util__auto_mco_btc()
        {
            const string Symbol = "MCO";
            const string QuoteSymbol = "BTC";
            _cossArbUtil.AutoCossBinance(Symbol, QuoteSymbol);
        }

        [TestMethod]
        public void Coss_arb_util__auto_lala()
        {
            const string Symbol = "LALA";
            const string CompExchange = IntegrationNameRes.KuCoin;
            _cossArbUtil.AutoSymbol(Symbol, CompExchange);
        }

        [TestMethod]
        public void Coss_arb_util__auto_cs()
        {
            const string Symbol = "CS";
            const string CompExchange = IntegrationNameRes.KuCoin;
            _cossArbUtil.AutoSymbol(Symbol, CompExchange);
        }

        [TestMethod]
        public void Coss_arb_util__auto_can()
        {
            const string Symbol = "CAN";
            const string CompExchange = IntegrationNameRes.KuCoin;
            _cossArbUtil.AutoSymbol(Symbol, CompExchange);
        }

        [TestMethod]
        public void Coss_arb_util__auto_prl()
        {
            bool shouldRun = false;
            if (!shouldRun)
            {
                throw new ApplicationException("This test works with real funds and must be run manually.");
            }

            const string Symbol = "PRL";
            const string CompExchange = IntegrationNameRes.KuCoin;
            _cossArbUtil.AutoSymbol(Symbol, CompExchange);
        }

        [TestMethod]
        public void Coss_arb_util__auto_fxt()
        {
            const string Symbol = "FXT";
            const string CompExchange = IntegrationNameRes.HitBtc;
            _cossArbUtil.AutoSymbol(Symbol, CompExchange);
        }

        [TestMethod]
        public void Coss_arb_util__coss__xdce_against_idex()
        {

            const string Symbol = "XDCE";
            const string CompExchange = IntegrationNameRes.Idex;
            _cossArbUtil.AutoSymbol(Symbol, CompExchange);
        }

        [TestMethod]
        public void Coss_arb_util__auto_ind()
        {
            bool shouldRun = false;
            if (!shouldRun)
            {
                throw new ApplicationException("This test works with real funds and must be run manually.");
            }

            const string Symbol = "IND";
            const string CompExchange = IntegrationNameRes.HitBtc;
            _cossArbUtil.AutoSymbol(Symbol, CompExchange);
        }

        [TestMethod]
        public void Coss_arb_util__auto_ark()
        {
            bool shouldRun = false;
            if (!shouldRun)
            {
                throw new ApplicationException("This test works with real funds and must be run manually.");
            }

            const string Symbol = "ARK";
            const string CompExchange = IntegrationNameRes.Binance;
            _cossArbUtil.AutoSymbol(Symbol, CompExchange);
        }

        [TestMethod]
        public void Coss_arb_util__auto_req()
        {
            const string Symbol = "REQ";
            const string CompExchange = IntegrationNameRes.Binance;
            _cossArbUtil.AutoSymbol(Symbol, CompExchange);
        }

        [TestMethod]
        public void Coss_arb_util__auto_sell_blz()
        {
            var results = _cossArbUtil.AutoSellSymbol("BLZ");
            results.Dump();
        }

        [TestMethod]
        public void Coss_arb_util__hitbtc_intersections_test()
        {
            var cossTradingPairs = _exchangeClient.GetTradingPairs(IntegrationNameRes.Coss, CachePolicy.OnlyUseCacheUnlessEmpty);
            var hitBtcTradingPairs = _exchangeClient.GetTradingPairs(IntegrationNameRes.HitBtc, CachePolicy.OnlyUseCacheUnlessEmpty);
            var binanceTradingPairs = _exchangeClient.GetTradingPairs(IntegrationNameRes.Binance, CachePolicy.OnlyUseCacheUnlessEmpty);

            var hitBtcSymbols = hitBtcTradingPairs.Select(item => item.Symbol.ToUpper()).Distinct().ToList();
            var binanceSymbols = binanceTradingPairs.Select(item => item.Symbol.ToUpper()).Distinct().ToList();
            var cossSymbols = cossTradingPairs.Select(item => item.Symbol.ToUpper()).Distinct().ToList();
            var matches = hitBtcSymbols.Where(hitBtcSymbol =>
                cossSymbols.Contains(hitBtcSymbol)
                && !binanceSymbols.Contains(hitBtcSymbol))
                .ToList();

            matches.Dump();
        }

        [TestMethod]
        public void Coss_arb_util__kucoin_intersections_test()
        {
            var cossTradingPairs = _exchangeClient.GetTradingPairs(IntegrationNameRes.Coss, CachePolicy.OnlyUseCacheUnlessEmpty);
            var kucoinTradingPairs = _exchangeClient.GetTradingPairs(IntegrationNameRes.KuCoin, CachePolicy.OnlyUseCacheUnlessEmpty);
            var binanceTradingPairs = _exchangeClient.GetTradingPairs(IntegrationNameRes.Binance, CachePolicy.OnlyUseCacheUnlessEmpty);

            var kucoinBtcSymbols = kucoinTradingPairs.Select(item => item.Symbol.ToUpper()).Distinct().ToList();
            var binanceSymbols = binanceTradingPairs.Select(item => item.Symbol.ToUpper()).Distinct().ToList();
            var cossSymbols = cossTradingPairs.Select(item => item.Symbol.ToUpper()).Distinct().ToList();
            var matches = kucoinBtcSymbols.Where(kucoinSymbol =>
                cossSymbols.Contains(kucoinSymbol)
                && !binanceSymbols.Contains(kucoinSymbol))
                .ToList();

            matches.Dump();
        }

        [TestMethod]
        public void Coss_arb_util__auto_eth_tusd()
        {
            _cossArbUtil.AutoTusdWithReverseBinanceSymbol("ETH");
        }

        [TestMethod]
        public void Coss_arb_util__auto_eth_usdt()
        {
            _cossArbUtil.AutoEthUsdt();
        }

        [TestMethod]
        public void Coss_arb_util__auto_btc_tusd()
        {
            _cossArbUtil.AutoTusdWithReverseBinanceSymbol("BTC");
        }        
        
        [TestMethod]
        public void Coss_arb_util__acquire_coss_v4()
        {
            _cossArbUtil.AcquireCossV4();
        }

        [TestMethod]
        public void Coss_arb_util__acquire_ark_coss_v5()
        {
            const string Symbol = "ARK";
            _cossArbUtil.AcquireAgainstBinanceSymbolV5(Symbol);
        }

        [TestMethod]
        public void Coss_arb_util__acquire_lsk_coss()
        {
            const string Symbol = "LSK";
            _cossArbUtil.AcquireAgainstBinanceSymbolV5(Symbol);
        }

        [TestMethod]
        public void Coss_arb_util__acquire_trx()
        {
            const string Symbol = "TRX";
            _cossArbUtil.AcquireAgainstBinanceSymbolV5(Symbol);
        }

        [TestMethod]
        public void Coss_arb_util__acquire_dash_coss()
        {
            const string Symbol = "DASH";
            _cossArbUtil.AcquireAgainstBinanceSymbolV5(Symbol);
        }

        [TestMethod]
        public void Coss_arb_util__acquire_dash_coss_v5()
        {
            const string Symbol = "DASH";
            _cossArbUtil.AcquireAgainstBinanceSymbolV5(Symbol);
        }

        [TestMethod]
        public void Coss_arb_util__acquire_bnt_coss_v5()
        {
            const string Symbol = "BNT";
            _cossArbUtil.AcquireAgainstBinanceSymbolV5(Symbol);
        }

        [TestMethod]
        public void Coss_arb_util__acquire_bchabc_coss_v3()
        {
            const string Symbol = "BCHABC";
            _cossArbUtil.AcquireAgainstBinanceSymbolV5(Symbol);
        }

        [TestMethod]
        public void Coss_arb_util__acquire_ltc_coss_v5()
        {
            const string Symbol = "LTC";
            _cossArbUtil.AcquireAgainstBinanceSymbolV5(Symbol);
        }

        [TestMethod]
        public void Coss_arb_util__acquire_enj_coss_v5()
        {
            const string Symbol = "ENJ";
            _cossArbUtil.AcquireAgainstBinanceSymbolV5(Symbol);
        }

        [TestMethod]
        public void Coss_arb_util__acquire_neo_coss_v5()
        {
            const string Symbol = "NEO";
            _cossArbUtil.AcquireAgainstBinanceSymbolV5(Symbol);
        }

        [TestMethod]
        public void Coss_arb_util__acquire_zen_coss_v5()
        {
            const string Symbol = "ZEN";
            _cossArbUtil.AcquireAgainstBinanceSymbolV5(Symbol);
        }

        [TestMethod]
        public void Coss_arb_util__acquire_snm_coss_v5()
        {
            const string Symbol = "SNM";
            _cossArbUtil.AcquireAgainstBinanceSymbolV5(Symbol);
        }

        [TestMethod]
        public void Coss_arb_util__acquire_sub_coss_v5()
        {
            const string Symbol = "SUB";
            _cossArbUtil.AcquireAgainstBinanceSymbolV5(Symbol);
        }

        [TestMethod]
        public void Coss_arb_util__acquire_omg_coss_v5()
        {
            const string Symbol = "OMG";
            _cossArbUtil.AcquireAgainstBinanceSymbolV5(Symbol);
        }

        [TestMethod]
        public void Coss_arb_util__acquire_link_coss_v5()
        {
            const string Symbol = "LINK";
            _cossArbUtil.AcquireAgainstBinanceSymbolV5(Symbol);
        }

        [TestMethod]
        public void Coss_arb_util__acquire_waves_coss_v5()
        {
            const string Symbol = "WAVES";
            _cossArbUtil.AcquireAgainstBinanceSymbolV5(Symbol);
        }

        [TestMethod]
        public void Coss_arb_util__acquire_knc_coss_v5()
        {
            const string Symbol = "KNC";
            _cossArbUtil.AcquireAgainstBinanceSymbolV5(Symbol);
        }

        [TestMethod]
        public void Coss_arb_util__acquire_xdce_tusd()
        {
            _cossArbUtil.AcquireXdceTusd();
        }

        [TestMethod]
        public void Coss_arb_util__acquire_xdce()
        {
            _cossArbUtil.AcquireXdce();
        }

        [TestMethod]
        public void Coss_arb_util__acquire_bwt()
        {
            _cossArbUtil.AcquireBwt();
        }

        [TestMethod]
        public void Coss_arb_util__acquire_eos()
        {
            _cossArbUtil.AcquireAgainstBinanceSymbolV5("EOS");
        }

        [TestMethod]
        public void Coss_arb_util__acquire_eth_ltc()
        {
            _cossArbUtil.AcquireEthLtc();
        }

        [TestMethod]
        public void Coss_arb_util__acquire_bwt_gusd()
        {
            _cossArbUtil.AcquireBwtGusd();
        }

        [TestMethod]
        public void Coss_arb_util__acquire_bwt_tusd()
        {
            _cossArbUtil.AcquireBwtTusd();
        }

        [TestMethod]
        public void Coss_arb_util__determine_coss_eth_value()
        {
            var valuationData = new CossValuationData
            {
                BinanceEthBtcOrderBook = _exchangeClient.GetOrderBook(IntegrationNameRes.Binance, "ETH", "BTC", CachePolicy.OnlyUseCacheUnlessEmpty),
                CossCossEthOrderBook = _exchangeClient.GetOrderBook(IntegrationNameRes.Coss, "COSS", "ETH", CachePolicy.OnlyUseCacheUnlessEmpty),
                CossCossBtcOrderBook = _exchangeClient.GetOrderBook(IntegrationNameRes.Coss, "COSS", "BTC", CachePolicy.OnlyUseCacheUnlessEmpty),
            };

            var result = _cossArbUtil.DetermineCossBtcValue(valuationData);
            result.Dump();
        }

        private ILogRepo GenerateLog()
        {
            return new LogRepo();

            //var log = new Mock<ILogRepo>();
            //log.Setup(mock => mock.Error(It.IsAny<Exception>(), It.IsAny<EventType>(), It.IsAny<Guid?>()))
            //    .Callback<Exception, EventType, Guid?>((ex, et, id) =>
            //    {
            //        Console.WriteLine($"Exception: {ex}");
            //    });

            //log.Setup(mock => mock.Error(It.IsAny<string>(), It.IsAny<Exception>(), It.IsAny<EventType>(), It.IsAny<Guid?>()))
            //    .Callback<string, Exception, EventType, Guid?>((message, ex, et, id) =>
            //    {
            //        Console.WriteLine($"Message: {message}; Exception: {ex}");
            //    });

            //log.Setup(mock => mock.Error(It.IsAny<string>(), It.IsAny<EventType>(), It.IsAny<Guid?>()))
            //    .Callback<string, EventType, Guid?>((message, et, id) =>
            //    {
            //        Console.WriteLine($"Message: {message}");
            //    });

            //log.Setup(mock => mock.Info(It.IsAny<string>(), It.IsAny<EventType>(), It.IsAny<Guid?>()))
            //    .Callback<string, EventType, Guid?>((message, et, id) =>
            //    {
            //        Console.WriteLine($"Message: {message}");
            //    });

            //return log.Object;
        }
    }
}
