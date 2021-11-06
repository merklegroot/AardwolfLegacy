using System;
using System.Linq;
using System.Threading;
using arb_workflow_lib;
using cache_lib.Models;
using config_client_lib;
using exchange_client_lib;
using log_lib;
using log_lib.Models;
using math_lib;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using trade_constants;
using trade_model;
using trade_res;
using workflow_client_lib;

namespace arb_workflow_lib_tests
{
    [TestClass]
    public class ArbWorkflowUtilTests
    {
        private IExchangeClient _exchangeClient;
        private ArbWorkflowUtil _arb;

        [TestInitialize]
        public void Setup()
        {
            var configClient = new ConfigClient();
            _exchangeClient = new ExchangeClient();
            var workflowClient = new WorkflowClient();

            var log = GenerateLog();

            _arb = new ArbWorkflowUtil(configClient, _exchangeClient, workflowClient, log);
        }

        [TestMethod]
        public void Arb_workflow_util__acquire_quantity__knc_binance()
        {
            const string Symbol = "KNC";
            const string BaseSymbol = "ETH";
            const string Exchange = IntegrationNameRes.Binance;
            const decimal TargetQuantity = 1443.0m;

            var tradingPairs = _exchangeClient.GetTradingPairs(Exchange, CachePolicy.AllowCache);
            var tradingPair = tradingPairs.Single(item => string.Equals(item.Symbol, Symbol, StringComparison.InvariantCultureIgnoreCase)
                && string.Equals(item.BaseSymbol, BaseSymbol, StringComparison.InvariantCultureIgnoreCase));

            var lotSize = tradingPair.LotSize.Value;
            if (lotSize <= 0) { throw new ApplicationException("LotSize must be > 0."); }

            var priceTick = tradingPair.PriceTick.Value;
            if (priceTick <= 0) { throw new ApplicationException("PriceTick must be > 0."); }

            while (true)
            {
                var openOrdersResult = _exchangeClient.GetOpenOrdersForTradingPairV2(Exchange, Symbol, BaseSymbol, CachePolicy.ForceRefresh);
                if (openOrdersResult.OpenOrders.Any())
                {
                    foreach (var openOrder in openOrdersResult.OpenOrders)
                    {
                        _exchangeClient.CancelOrder(Exchange, openOrder);
                    }
                }

                var symbolHolding = _exchangeClient.GetBalance(Exchange, Symbol, CachePolicy.ForceRefresh);
                var symbolTotal = symbolHolding?.Total ?? 0;
                var remainingQuantityToAcquire = TargetQuantity - symbolTotal;

                if (remainingQuantityToAcquire <= TargetQuantity * 0.005m) { return; }

                var orderBook = _exchangeClient.GetOrderBook(Exchange, Symbol, BaseSymbol, CachePolicy.ForceRefresh);
                var bestBid = orderBook.BestBid();
                var bestBidPrice = bestBid.Price;
                if (bestBidPrice <= 0) { throw new ApplicationException($"{Exchange}'s best bid price for {Symbol}-{BaseSymbol} must be > 0."); }

                var tickUpBidPrice = bestBidPrice + priceTick;
                var quantityToBid = MathUtil.ConstrainToMultipleOf(remainingQuantityToAcquire, lotSize);
                if (quantityToBid <= 0) { return; }

                var orderResult = _exchangeClient.BuyLimitV2(Exchange, Symbol, BaseSymbol, new QuantityAndPrice
                {
                    Quantity = quantityToBid,
                    Price = tickUpBidPrice
                });

                Thread.Sleep(TimeSpan.FromSeconds(5));
            }
        }
        
        [TestMethod]
        public void Arb_workflow_util__qryptos_auto_mitx()
        {
            const string Symbol = "MITX";
            //const string AltBaseSymbol = "QASH";
            const string AltBaseSymbol = null;
            const string CompExchange = IntegrationNameRes.HitBtc;

            _arb.AutoSymbol(Symbol, IntegrationNameRes.Qryptos, CompExchange, AltBaseSymbol);
        }

        [TestMethod]
        public void Arb_workflow_util__qryptos_auto_mith()
        {
            const string Symbol = "MITH";
            const string CompExchange = IntegrationNameRes.Binance;

            _arb.AutoSymbol(Symbol, IntegrationNameRes.Qryptos, CompExchange, null, true);
        }

        [TestMethod]
        public void Arb_workflow_util__qryptos_auto_can()
        {
            bool shouldRun = false;
            if (!shouldRun)
            {
                throw new ApplicationException("This test works with real funds and must be executed manually.");
            }

            const string Symbol = "CAN";
            const string AltBaseSymbol = "QASH";
            const string CompExchange = IntegrationNameRes.KuCoin;
            const bool WaiveArbDepositAndWithdrawalCheck = true;

            _arb.AutoSymbol(Symbol, IntegrationNameRes.Qryptos, CompExchange, AltBaseSymbol, WaiveArbDepositAndWithdrawalCheck);
        }

        [TestMethod]
        public void Arb_workflow_util__qryptos_auto_ftx()
        {
            bool shouldRun = false;
            if (!shouldRun)
            {
                throw new ApplicationException("This test works with real funds and must be executed manually.");
            }

            const string Symbol = "FTX";
            const string AltBaseSymbol = "QASH";
            const string CompExchange = IntegrationNameRes.HitBtc;

            _arb.AutoSymbol(Symbol, IntegrationNameRes.Qryptos, CompExchange, AltBaseSymbol);
        }

        [TestMethod]
        public void Arb_workflow_util__qryptos_auto_vet()
        {
            bool shouldRun = false;
            if (!shouldRun)
            {
                throw new ApplicationException("This test works with real funds and must be executed manually.");
            }

            const string Symbol = "VET";
            const string AltBaseSymbol = null;
            const string CompExchange = IntegrationNameRes.Binance;

            _arb.AutoSymbol(Symbol, IntegrationNameRes.Qryptos, CompExchange, AltBaseSymbol);
        }

        [TestMethod]
        public void Arb_workflow_util__blocktrade_auto_kaya()
        {
            _arb.AutoSymbol("KAYA", IntegrationNameRes.Blocktrade, IntegrationNameRes.Coss, null, true, false, 100, 35, new System.Collections.Generic.Dictionary<string, decimal> { { "ETH", 0.15m } });
        }

        [TestMethod]
        public void Arb_workflow_util__coss_auto_gat()
        {
            const string Symbol = "GAT";
            const string Exchange = IntegrationNameRes.Coss;
            const string AltBaseSymbol = null;
            const string CompExchange = IntegrationNameRes.KuCoin;

            _arb.AutoSymbol(Symbol, Exchange, CompExchange, AltBaseSymbol);
        }

        [TestMethod]
        public void Arb_workflow_util__coss_auto_xem()
        {
            bool shouldRun = false;
            if (!shouldRun)
            {
                throw new ApplicationException("This test works with real funds and must be executed manually.");
            }

            const string Symbol = "XEM";
            const string Exchange = IntegrationNameRes.Coss;
            const string AltBaseSymbol = null;
            const string CompExchange = IntegrationNameRes.Binance;

            _arb.AutoSymbol(Symbol, Exchange, CompExchange, AltBaseSymbol);
        }

        [TestMethod]
        public void Arb_workflow_util__kucoin__auto_bchabc()
        {
            bool shouldRun = false;
            if (!shouldRun)
            {
                throw new ApplicationException("This test works with real funds and must be executed manually.");
            }

            const string Symbol = "BCHABC";
            const string Exchange = IntegrationNameRes.KuCoin;
            const string AltBaseSymbol = null;
            const string CompExchange = IntegrationNameRes.Binance;

            _arb.AutoSymbol(Symbol, Exchange, CompExchange, AltBaseSymbol, true, true);
        }

        [TestMethod]
        public void Arb_workflow_util__kucoin__acquire_usdc_eth()
        {
            bool shouldRun = false;
            if (!shouldRun)
            {
                throw new ApplicationException("This test works with real funds and must be executed manually.");
            }

            _arb.AcquireUsdcEth();
        }

        [TestMethod]
        public void Arb_workflow_util__coss_auto_wish()
        {
            bool shouldRun = false;
            if (!shouldRun)
            {
                throw new ApplicationException("This test works with real funds and must be executed manually.");
            }

            const string Symbol = "WISH";
            const string Exchange = IntegrationNameRes.Coss;
            const string AltBaseSymbol = null;
            const string CompExchange = IntegrationNameRes.Cryptopia;

            _arb.AutoSymbol(Symbol, Exchange, CompExchange, AltBaseSymbol);
        }

        [TestMethod]
        public void Arb_workflow_util__coss_auto_kaya()
        {
            const string Symbol = "KAYA";
            const string Exchange = IntegrationNameRes.Coss;
            const string AltBaseSymbol = null;
            const string CompExchange = IntegrationNameRes.Blocktrade;

            _arb.AutoSymbol(Symbol, Exchange, CompExchange, AltBaseSymbol, false, true, 500);
        }

        [TestMethod]
        public void Arb_workflow_util__coss_auto_lsk()
        {
            bool shouldRun = false;
            if (!shouldRun)
            {
                throw new ApplicationException("This test works with real funds and must be executed manually.");
            }

            const string Symbol = "LSK";
            const string Exchange = IntegrationNameRes.Coss;
            const string AltBaseSymbol = null;
            const string CompExchange = IntegrationNameRes.Binance;

            _arb.AutoSymbol(Symbol, Exchange, CompExchange, AltBaseSymbol);
        }

        [TestMethod]
        public void Arb_workflow_util__hitbtc_auto_steem()
        {
            bool shouldRun = false;
            if (!shouldRun)
            {
                throw new ApplicationException("This test works with real funds and must be executed manually.");
            }

            const string Symbol = "STEEM";
            const string Exchange = IntegrationNameRes.HitBtc;
            const string AltBaseSymbol = null;
            const string CompExchange = IntegrationNameRes.Binance;

            _arb.AutoSymbol(Symbol, Exchange, CompExchange, AltBaseSymbol);
        }

        [TestMethod]
        public void Arb_workflow_util__hitbtc__auto_eth_gusd__only_sell()
        {
            const string Exchange = IntegrationNameRes.HitBtc;
            const string DollarSymbol = "GUSD";     
            
            _arb.AutoEthXusd(Exchange, DollarSymbol, true, false);
        }

        [TestMethod]
        public void Arb_workflow_util__coss_auto_mrk()
        {
            bool shouldRun = false;
            if (!shouldRun)
            {
                throw new ApplicationException("This test works with real funds and must be executed manually.");
            }

            const string Symbol = "MRK";
            const string Exchange = IntegrationNameRes.Coss;
            const string AltBaseSymbol = null;
            const string CompExchange = IntegrationNameRes.HitBtc;

            _arb.AutoSymbol(Symbol, Exchange, CompExchange, AltBaseSymbol);
        }

        [TestMethod]
        public void Arb_workflow_util__coss_auto_snm()
        {
            bool shouldRun = false;
            if (!shouldRun)
            {
                throw new ApplicationException("This test works with real funds and must be executed manually.");
            }

            const string Symbol = "SNM";
            const string Exchange = IntegrationNameRes.Coss;
            const string AltBaseSymbol = null;
            const string CompExchange = IntegrationNameRes.Binance;

            _arb.AutoSymbol(Symbol, Exchange, CompExchange, AltBaseSymbol);
        }

        [TestMethod]
        public void Arb_workflow_util__coss_auto_poe()
        {
            const string Symbol = "POE";
            const string Exchange = IntegrationNameRes.Coss;
            const string AltBaseSymbol = null;
            const string CompExchange = IntegrationNameRes.Binance;

            _arb.AutoSymbol(Symbol, Exchange, CompExchange, AltBaseSymbol);
        }

        [TestMethod]
        public void Arb_workflow_util__coss_auto_cred()
        {
            const string Symbol = "CRED";
            const string Exchange = IntegrationNameRes.Coss;
            const string AltBaseSymbol = null;
            const string CompExchange = IntegrationNameRes.Idex;

            _arb.AutoSymbol(Symbol, Exchange, CompExchange, AltBaseSymbol, false, true);
        }

        [TestMethod]
        public void Arb_workflow_util__coss_auto_xdce()
        {
            const string Symbol = "XDCE";
            const string Exchange = IntegrationNameRes.Coss;
            const string AltBaseSymbol = null;
            const string CompExchange = IntegrationNameRes.Idex;

            _arb.AutoSymbol(Symbol, Exchange, CompExchange, AltBaseSymbol, false, true);
        }

        [TestMethod]
        public void Arb_workflow_util__coss_auto_prsn()
        {
            const string Symbol = "PRSN";
            const string Exchange = IntegrationNameRes.Coss;

            _arb.SingleExchangeArb(Exchange, Symbol);
        }

        [TestMethod]
        public void Arb_workflow_util__coss_auto_xnk()
        {
            const string Symbol = "XNK";
            const string Exchange = IntegrationNameRes.Coss;
            const string AltBaseSymbol = null;
            const string CompExchange = IntegrationNameRes.Qryptos;

            _arb.AutoSymbol(Symbol, Exchange, CompExchange, AltBaseSymbol, false, true);
        }

        [TestMethod]
        public void Arb_workflow_util__coss_auto_opq()
        {
            bool shouldRun = false;
            if (!shouldRun)
            {
                throw new ApplicationException("This test works with real funds and must be executed manually.");
            }

            const string Symbol = "OPQ";
            const string Exchange = IntegrationNameRes.Coss;
            const string AltBaseSymbol = null;
            const string CompExchange = IntegrationNameRes.KuCoin;

            _arb.AutoSymbol(Symbol, Exchange, CompExchange, AltBaseSymbol);
        }

        [TestMethod]
        public void Arb_workflow_util__coss_auto_pix()
        {
            bool shouldRun = false;
            if (!shouldRun)
            {
                throw new ApplicationException("This test works with real funds and must be executed manually.");
            }

            const string Symbol = "PIX";
            const string Exchange = IntegrationNameRes.Coss;
            const string AltBaseSymbol = null;
            const string CompExchange = IntegrationNameRes.HitBtc;

            _arb.AutoSymbol(Symbol, Exchange, CompExchange, AltBaseSymbol);
        }

        [TestMethod]
        public void Arb_workflow_util__coss_auto_prl()
        {
            bool shouldRun = false;
            if (!shouldRun)
            {
                throw new ApplicationException("This test works with real funds and must be executed manually.");
            }

            const string Symbol = "PRL";
            const string Exchange = IntegrationNameRes.Coss;
            const string AltBaseSymbol = null;
            const string CompExchange = IntegrationNameRes.KuCoin;

            _arb.AutoSymbol(Symbol, Exchange, CompExchange, AltBaseSymbol);
        }

        [TestMethod]
        public void Arb_workflow_util__coss_auto_fyn()
        {
            const string Symbol = "FYN";
            const string Exchange = IntegrationNameRes.Coss;
            const string AltBaseSymbol = null;
            const string CompExchange = IntegrationNameRes.HitBtc;

            _arb.AutoSymbol(Symbol, Exchange, CompExchange, AltBaseSymbol);
        }

        [TestMethod]
        public void Arb_workflow_util__coss_auto_dash()
        {
            bool shouldRun = false;
            if (!shouldRun)
            {
                throw new ApplicationException("This test works with real funds and must be executed manually.");
            }

            const string Symbol = "DASH";
            const string Exchange = IntegrationNameRes.Coss;
            const string AltBaseSymbol = null;
            const string CompExchange = IntegrationNameRes.Binance;

            _arb.AutoSymbol(Symbol, Exchange, CompExchange, AltBaseSymbol);
        }


        [TestMethod]
        public void Arb_workflow_util__coss_auto_knc()
        {
            bool shouldRun = false;
            if (!shouldRun)
            {
                throw new ApplicationException("This test works with real funds and must be executed manually.");
            }

            const string Symbol = "KNC";
            const string Exchange = IntegrationNameRes.Coss;
            const string AltBaseSymbol = null;
            const string CompExchange = IntegrationNameRes.Binance;

            _arb.AutoSymbol(Symbol, Exchange, CompExchange, AltBaseSymbol);
        }


        [TestMethod]
        public void Arb_workflow_util__coss_auto_req()
        {
            bool shouldRun = false;
            if (!shouldRun)
            {
                throw new ApplicationException("This test works with real funds and must be executed manually.");
            }

            const string Symbol = "REQ";
            const string Exchange = IntegrationNameRes.Coss;
            const string AltBaseSymbol = null;
            const string CompExchange = IntegrationNameRes.Binance;

            _arb.AutoSymbol(Symbol, Exchange, CompExchange, AltBaseSymbol);
        }

        [TestMethod]
        public void Arb_workflow_util__coss_auto_can()
        {
            const string Symbol = "CAN";
            const string Exchange = IntegrationNameRes.Coss;
            const string AltBaseSymbol = null;
            const string CompExchange = IntegrationNameRes.KuCoin;
            const decimal MaxUsdValueToOwn = 50;

            _arb.AutoSymbol(Symbol, Exchange, CompExchange, AltBaseSymbol, false, false, MaxUsdValueToOwn);
        }

        [TestMethod]
        public void Arb_workflow_util__coss_auto_bchabc()
        {
            bool shouldRun = false;
            if (!shouldRun)
            {
                throw new ApplicationException("This test works with real funds and must be executed manually.");
            }

            const string Symbol = "BCHABC";
            const string Exchange = IntegrationNameRes.Coss;
            const string AltBaseSymbol = null;
            const string CompExchange = IntegrationNameRes.Binance;

            _arb.AutoSymbol(Symbol, Exchange, CompExchange, AltBaseSymbol, false, true);
        }

        [TestMethod]
        public void Arb_workflow_util__coss_auto_ark()
        {
            bool shouldRun = false;
            if (!shouldRun)
            {
                throw new ApplicationException("This test works with real funds and must be executed manually.");
            }

            const string Symbol = "ARK";
            const string Exchange = IntegrationNameRes.Coss;
            const string AltBaseSymbol = null;
            const string CompExchange = IntegrationNameRes.Binance;

            _arb.AutoSymbol(Symbol, Exchange, CompExchange, AltBaseSymbol);
        }

        [TestMethod]
        public void Arb_workflow_util__coss_auto_cs()
        {
            bool shouldRun = false;
            if (!shouldRun)
            {
                throw new ApplicationException("This test works with real funds and must be executed manually.");
            }

            const string Symbol = "CS";
            const string Exchange = IntegrationNameRes.Coss;
            const string AltBaseSymbol = null;
            const string CompExchange = IntegrationNameRes.KuCoin;

            _arb.AutoSymbol(Symbol, Exchange, CompExchange, AltBaseSymbol);
        }

        [TestMethod]
        public void Arb_workflow_util__bitz_auto_pix()
        {
            bool shouldRun = false;
            if (!shouldRun)
            {
                throw new ApplicationException("This test works with real funds and must be executed manually.");
            }

            const string Symbol = "PIX";
            const string Exchange = IntegrationNameRes.Bitz;
            const string AltBaseSymbol = null;
            const string CompExchange = IntegrationNameRes.HitBtc;

            _arb.AutoSymbol(Symbol, Exchange, CompExchange, AltBaseSymbol);
        }

        [TestMethod]
        public void Arb_workflow_util__bitz_auto_ark()
        {
            bool shouldRun = false;
            if (!shouldRun)
            {
                throw new ApplicationException("This test works with real funds and must be executed manually.");
            }

            const string Symbol = "ARK";
            const string Exchange = IntegrationNameRes.Bitz;
            // const string AltBaseSymbol = "TUSD";
            const string AltBaseSymbol = null;
            const string CompExchange = IntegrationNameRes.Binance;
            const bool WaiveDepositWithdrawalCheck = true;

            _arb.AutoSymbol(Symbol, Exchange, CompExchange, AltBaseSymbol, WaiveDepositWithdrawalCheck);
        }

        [TestMethod]
        public void Arb_workflow_util__bitz_auto_arn()
        {
            bool shouldRun = false;
            if (!shouldRun)
            {
                throw new ApplicationException("This test works with real funds and must be executed manually.");
            }

            const string Symbol = "ARN";
            const string Exchange = IntegrationNameRes.Bitz;
            const string AltBaseSymbol = null;
            const string CompExchange = IntegrationNameRes.Binance;
            const bool WaiveDepositWithdrawalCheck = true;

            _arb.AutoSymbol(Symbol, Exchange, CompExchange, AltBaseSymbol, WaiveDepositWithdrawalCheck);
        }

        [TestMethod]
        public void Arb_workflow_util__bitz_auto_fct()
        {
            bool shouldRun = false;
            if (!shouldRun)
            {
                throw new ApplicationException("This test works with real funds and must be executed manually.");
            }

            const string Symbol = "FCT";
            const string Exchange = IntegrationNameRes.Bitz;
            const string AltBaseSymbol = null;
            const string CompExchange = IntegrationNameRes.KuCoin;
            const bool WaiveArbDepositWithdrawalCheck = true;

            _arb.AutoSymbol(Symbol, Exchange, CompExchange, AltBaseSymbol, WaiveArbDepositWithdrawalCheck);
        }

        [TestMethod]
        public void Arb_workflow_util__bitz_auto_tky()
        {
            bool shouldRun = false;
            if (!shouldRun)
            {
                throw new ApplicationException("This test works with real funds and must be executed manually.");
            }

            const string Symbol = "TKY";
            const string Exchange = IntegrationNameRes.Bitz;
            const string AltBaseSymbol = null;
            const string CompExchange = IntegrationNameRes.KuCoin;

            _arb.AutoSymbol(Symbol, Exchange, CompExchange, AltBaseSymbol);
        }

        [TestMethod]
        public void Arb_workflow_util__coss_auto_ltc()
        {
            bool shouldRun = false;
            if (!shouldRun)
            {
                throw new ApplicationException("This test works with real funds and must be executed manually.");
            }

            const string Symbol = "LTC";
            const string Exchange = IntegrationNameRes.Coss;
            const string AltBaseSymbol = null;
            const string CompExchange = IntegrationNameRes.Binance;

            _arb.AutoSymbol(Symbol, Exchange, CompExchange, AltBaseSymbol);
        }

        [TestMethod]
        public void Arb_workflow_util__coss_auto_lala()
        {
            bool shouldRun = false;
            if (!shouldRun)
            {
                throw new ApplicationException("This test works with real funds and must be executed manually.");
            }

            const string Symbol = "LALA";
            const string Exchange = IntegrationNameRes.Coss;
            const string AltBaseSymbol = null;
            const string CompExchange = IntegrationNameRes.KuCoin;

            _arb.AutoSymbol(Symbol, Exchange, CompExchange, AltBaseSymbol);
        }

        [TestMethod]
        public void Arb_workflow_util__binance__slow_sell__ltc_for_btc()
        {
            while (true)
            {
                _arb.AutoSell(IntegrationNameRes.Binance, "LTC", "BTC");
                Thread.Sleep(TimeSpan.FromSeconds(10));
            }
        }

        [TestMethod]
        public void Arb_workflow_util__binance__slow_sell__ltc_for_eth()
        {
            while (true)
            {
                _arb.AutoSell(IntegrationNameRes.Binance, "LTC", "ETH");
                Thread.Sleep(TimeSpan.FromSeconds(10));
            }
        }

        [TestMethod]
        public void Arb_workflow_util__binance__slow_sell__neo_for_eth()
        {
            while (true)
            {
                _arb.AutoSell(IntegrationNameRes.Binance, "NEO", "ETH");
                Thread.Sleep(TimeSpan.FromSeconds(10));
            }
        }

        [TestMethod]
        public void Arb_workflow_util__binance__slow_sell__omg_for_eth()
        {
            while (true)
            {
                _arb.AutoSell(IntegrationNameRes.Binance, "OMG", "ETH");
                Thread.Sleep(TimeSpan.FromSeconds(10));
            }
        }

        [TestMethod]
        public void Arb_workflow_util__binance__slow_sell__neo_for_btc()
        {
            while (true)
            {
                _arb.AutoSell(IntegrationNameRes.Binance, "NEO", "BTC");
                Thread.Sleep(TimeSpan.FromSeconds(10));
            }
        }

        [TestMethod]
        public void Arb_workflow_util__binance__slow_sell__link_for_btc()
        {
            while (true)
            {
                _arb.AutoSell(IntegrationNameRes.Binance, "LINK", "BTC");
                Thread.Sleep(TimeSpan.FromSeconds(10));
            }
        }

        [TestMethod]
        public void Arb_workflow_util__binance__slow_sell__enj_for_eth()
        {
            while (true)
            {
                _arb.AutoSell(IntegrationNameRes.Binance, "ENJ", "ETH");
                Thread.Sleep(TimeSpan.FromSeconds(10));
            }
        }

        [TestMethod]
        public void Arb_workflow_util__binance__slow_buy__cvc_for_eth()
        {
            const decimal Quantity = 1466.30000000m;

            while (true)
            {
                _arb.AutoBuy(IntegrationNameRes.Binance, "CVC", "ETH", Quantity);
                Thread.Sleep(TimeSpan.FromSeconds(10));
            }
        }

        [TestMethod]
        public void Arb_workflow_util__binance__slow_buy__enj_for_eth()
        {
            const decimal Quantity = 1466.30000000m;

            while (true)
            {
                _arb.AutoBuy(IntegrationNameRes.Binance, "ENJ", "ETH", Quantity);
                Thread.Sleep(TimeSpan.FromSeconds(10));
            }
        }

        [TestMethod]
        public void Arb_workflow_util__binance__slow_sell__enj_for_btc()
        {
            while (true)
            {
                _arb.AutoSell(IntegrationNameRes.Binance, "ENJ", "BTC");
                Thread.Sleep(TimeSpan.FromSeconds(10));
            }
        }

        [TestMethod]
        public void Arb_workflow_util__binance__slow_sell__blz_for_btc()
        {
            while (true)
            {
                _arb.AutoSell(IntegrationNameRes.Binance, "BLZ", "BTC");
                Thread.Sleep(TimeSpan.FromSeconds(10));
            }
        }

        [TestMethod]
        public void Arb_workflow_util__binance__slow_sell__blz_for_eth()
        {
            while (true)
            {
                _arb.AutoSell(IntegrationNameRes.Binance, "BLZ", "ETH");
                Thread.Sleep(TimeSpan.FromSeconds(10));
            }
        }

        [TestMethod]
        public void Arb_workflow_util__binance__slow_sell__sub_for_eth()
        {
            while (true)
            {
                _arb.AutoSell(IntegrationNameRes.Binance, "SUB", "ETH");
                Thread.Sleep(TimeSpan.FromSeconds(10));
            }
        }

        [TestMethod]
        public void Arb_workflow_util__binance__slow_sell__cvc_for_eth()
        {
            while (true)
            {
                _arb.AutoSell(IntegrationNameRes.Binance, "CVC", "ETH");
                Thread.Sleep(TimeSpan.FromSeconds(10));
            }
        }

        [TestMethod]
        public void Arb_workflow_util__binance__slow_sell__snm_for_eth()
        {
            while (true)
            {
                _arb.AutoSell(IntegrationNameRes.Binance, "SNM", "ETH");
                Thread.Sleep(TimeSpan.FromSeconds(10));
            }
        }

        [TestMethod]
        public void Arb_workflow_util__binance__slow_sell__eth_for_btc()
        {
            while (true)
            {
                _arb.AutoSell(IntegrationNameRes.Binance, "ETH", "BTC");// , 0.01m);
                Thread.Sleep(TimeSpan.FromSeconds(10));
            }
        }

        [TestMethod]
        public void Arb_workflow_util__binance__slow_sell__eth_usdc()
        {
            while (true)
            {
                _arb.AutoSell(IntegrationNameRes.Binance, "ETH", "USDC");
                Thread.Sleep(TimeSpan.FromSeconds(10));
            }
        }

        [TestMethod]
        public void Arb_workflow_util__binance__slow_sell__xem_for_btc()
        {
            while (true)
            {
                _arb.AutoSell(IntegrationNameRes.Binance, "XEM", "BTC");
                Thread.Sleep(TimeSpan.FromSeconds(10));
            }
        }

        [TestMethod]
        public void Arb_workflow_util__binance__slow_buy__bchabc_for_btc()
        {
            while (true)
            {
                _arb.AutoBuy(IntegrationNameRes.Binance, "BCHABC", "BTC");
                Thread.Sleep(TimeSpan.FromSeconds(10));
            }
        }

        [TestMethod]
        public void Arb_workflow_util__binance__slow_buy__eth_for_btc()
        {
            while (true)
            {
                _arb.AutoBuy(IntegrationNameRes.Binance, "ETH", "BTC");
                Thread.Sleep(TimeSpan.FromSeconds(10));
            }
        }

        [TestMethod]
        public void Arb_workflow_util__binance__slow_buy__waves_for_btc()
        {
            while (true)
            {
                _arb.AutoBuy(IntegrationNameRes.Binance, "WAVES", "BTC");
                Thread.Sleep(TimeSpan.FromSeconds(10));
            }
        }

        [TestMethod]
        public void Arb_workflow_util__binance__slow_sell__eth_for_usdc()
        {
            while (true)
            {
                _arb.AutoSell(IntegrationNameRes.Binance, "ETH", "USDC");
                Thread.Sleep(TimeSpan.FromSeconds(10));
            }
        }

        [TestMethod]
        public void Arb_workflow_util__binance__slow_sell__btc_for_usdc()
        {
            while (true)
            {
                _arb.AutoSell(IntegrationNameRes.Binance, "BTC", "USDC");
                Thread.Sleep(TimeSpan.FromSeconds(10));
            }
        }

        [TestMethod]
        public void Arb_workflow_util__binance__slow_buy__tusd_for_btc()
        {
            while (true)
            {
                _arb.AutoBuy(IntegrationNameRes.Binance, "TUSD", "BTC", 1000.0m);
                Thread.Sleep(TimeSpan.FromSeconds(10));
            }
        }

        [TestMethod]
        public void Arb_workflow_util__binance__slow_buy__xrp_for_btc()
        {
            while (true)
            {
                _arb.AutoBuy(IntegrationNameRes.Binance, "XRP", "BTC", 698.97m);
                Thread.Sleep(TimeSpan.FromSeconds(10));
            }
        }

        [TestMethod]
        public void Arb_workflow_util__binance__slow_buy__tusd_for_eth()
        {
            while (true)
            {
                _arb.AutoBuy(IntegrationNameRes.Binance, "TUSD", "ETH");
                Thread.Sleep(TimeSpan.FromSeconds(10));
            }
        }

        [TestMethod]
        public void Arb_workflow_util__rolling_binance_tusd_purchase()
        {
            _arb.RollingBinanceTusdPurchase();
        }

        [TestMethod]
        public void Arb_workflow_util__blocktrade__auto_eth_btc()
        {
            _arb.AutoEthBtc(IntegrationNameRes.Blocktrade);
        }

        [TestMethod]
        public void Arb_workflow_util__blocktrade__auto_chx()
        {
            const decimal MaxValueToOwn = 100.0m;
            _arb.AutoSymbol("CHX", IntegrationNameRes.Blocktrade, IntegrationNameRes.Idex, null, true, true, MaxValueToOwn);
        }

        [TestMethod]
        public void Arb_workflow_util__hitbtc__auto_chx()
        {
            const decimal MaxValueToOwn = 100.0m;
            const decimal IdealPercentDiff = 5.0m;
            _arb.AutoSymbol("CHX", IntegrationNameRes.HitBtc, IntegrationNameRes.Idex, null, true, true, MaxValueToOwn, IdealPercentDiff);
        }

        [TestMethod]
        public void Arb_workflow_util__blocktrade__auto_ltc()
        {
            const decimal MaxValueToOwn = 100.0m;
            _arb.AutoSymbol("LTC", IntegrationNameRes.Blocktrade, IntegrationNameRes.Binance, null, true, false, MaxValueToOwn);
        }

        [TestMethod]
        public void Arb_workflow_util__blocktrade__auto_btt()
        {
            _arb.AutoStraddle(IntegrationNameRes.Blocktrade, "BTT", "ETH");
        }

        [TestMethod]
        public void Arb_workflow_util__blocktrade__auto_bat()
        {
            const decimal MaxValueToOwn = 100.0m;
            _arb.AutoSymbol("BAT", IntegrationNameRes.Blocktrade, IntegrationNameRes.Binance, null, true, false, MaxValueToOwn);
        }

        [TestMethod]
        public void Arb_workflow_util__acquire_quantity__binance__bchabc_btc__point_ninety()
        {
            const string Exchange = IntegrationNameRes.Binance;
            const string Symbol = "BCHABC";
            const string BaseSymbol = "BTC";
            const decimal Quantity = 0.9m;

            while (true)
            {
                _arb.AcquireQuantity(Exchange, Symbol, BaseSymbol, Quantity);

                Thread.Sleep(TimeSpan.FromSeconds(10));
            }
        }

        [TestMethod]
        public void Arb_workflow_util__kucoin__usdc()
        {
            _arb.KucoinUsdc();
        }        

        [TestMethod]
        public void Arb_workflow_util__kucoin__sell_can_eth()
        {
            _arb.AutoSell(IntegrationNameRes.KuCoin, "CAN", "ETH");             
        }

        [TestMethod]
        public void Arb_workflow_util__binance__sell_zen_eth()
        {
            while (true)
            {
                _arb.AutoSell(IntegrationNameRes.Binance, "ZEN", "ETH");
                Thread.Sleep(TimeSpan.FromSeconds(10));
            }
        }

        [TestMethod]
        public void Arb_workflow_util__binance__sell_omg_eth()
        {
            while (true)
            {
                _arb.AutoSell(IntegrationNameRes.Binance, "OMG", "ETH");
                Thread.Sleep(TimeSpan.FromSeconds(10));
            }
        }

        [TestMethod]
        public void Arb_workflow_util__binance__sell_tusd_eth()
        {
            while (true)
            {
                _arb.AutoSell(IntegrationNameRes.Binance, "TUSD", "ETH");
                Thread.Sleep(TimeSpan.FromSeconds(10));
            }
        }

        [TestMethod]
        public void Arb_workflow_util__kucoin__auto_straddle__can_eth()
        {
            _arb.AutoStraddle(IntegrationNameRes.KuCoin, "CAN", "ETH");
        }

        [TestMethod]
        public void Arb_workflow_util__kucoin__auto_straddle__can_btc()
        {
            _arb.AutoStraddle(IntegrationNameRes.KuCoin, "CAN", "BTC");
        }

        [TestMethod]
        public void Arb_workflow_util__kucoin__auto_straddle__vnx_eth()
        {
            _arb.AutoStraddle(IntegrationNameRes.KuCoin, "VNX", "ETH");
        }

        [TestMethod]
        public void Arb_workflow_util__bitz__tusd_btc()
        {
            _arb.AutoReverseXusd(IntegrationNameRes.Bitz, "TUSD", "BTC");
        }

        [TestMethod]
        public void Arb_workflow_util__bitz__tusd_eth()
        {
            _arb.AutoReverseXusd(IntegrationNameRes.Bitz, "TUSD", "ETH");
        }

        private ILogRepo GenerateLog()
        {
            var log = new Mock<ILogRepo>();
            log.Setup(mock => mock.Error(It.IsAny<Exception>(), It.IsAny<EventType>(), It.IsAny<Guid?>()))
                .Callback<Exception, EventType, Guid?>((ex, et, id) =>
                {
                    Console.WriteLine($"Exception: {ex}");
                });

            log.Setup(mock => mock.Error(It.IsAny<string>(), It.IsAny<Exception>(), It.IsAny<EventType>(), It.IsAny<Guid?>()))
                .Callback<string, Exception, EventType, Guid?>((message, ex, et, id) =>
                {
                    Console.WriteLine($"Message: {message}; Exception: {ex}");
                });

            log.Setup(mock => mock.Error(It.IsAny<string>(), It.IsAny<EventType>(), It.IsAny<Guid?>()))
                .Callback<string, EventType, Guid?>((message, et, id) =>
                {
                    Console.WriteLine($"Message: {message}");
                });

            log.Setup(mock => mock.Info(It.IsAny<string>(), It.IsAny<EventType>(), It.IsAny<Guid?>()))
                .Callback<string, EventType, Guid?>((message, et, id) =>
                {
                    Console.WriteLine($"Message: {message}");
                });

            return log.Object;
        }
    }
}
