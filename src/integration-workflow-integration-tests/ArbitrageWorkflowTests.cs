using cache_lib.Models;
using config_client_lib;
using coss_data_lib;
using cryptocompare_client_lib;
using dump_lib;
using env_config_lib;
using integration_workflow_lib;
using integration_workflow_lib.Models;
using client_lib;
using log_lib;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using rabbit_lib;
using res_util_lib;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using task_lib;
using trade_contracts;
using trade_email_lib;
using trade_lib;
using trade_model;
using trade_node_integration;
using trade_res;
using wait_for_it_lib;
using web_util;
using workflow_client_lib;
using exchange_client_lib;
using math_lib;
using trade_constants;

namespace integration_workflow_integration_tests
{
    [TestClass]
    public class ArbitrageWorkflowTests
    {
        private ArbitrageWorkflow _workflow;
        private ICryptoCompareClient _cryptoCompareClient;
        private IExchangeClient _exchangeClient;
        private IWorkflowClient _workflowClient;

        private decimal _btcToUsd;

        private static List<string> CossSymbolsToAvoid = new List<string>
        { "ARK", "WAVES", "XEM" };

        [TestInitialize]
        public void Setup()
        {
            var webUtil = new WebUtil();
            var configClient = new ConfigClient();
            var cossHistoryRepo = new CossHistoryRepo(configClient);
            var cossOpenOrderRepo = new CossOpenOrderRepo(configClient);
            var cossXhrOpenOrderRepo = new CossXhrOpenOrderRepo(configClient);
            var emailUtil = new TradeEmailUtil(webUtil);
            var waitForIt = new WaitForIt();

            var log = new Mock<ILogRepo>();
            var nodeUtil = new TradeNodeUtil(configClient, webUtil, log.Object);

            _cryptoCompareClient = new CryptoCompareClient();

            var rabbitConnFactory = new RabbitConnectionFactory(new EnvironmentConfigRepo());

            _btcToUsd = _cryptoCompareClient.GetUsdValue("BTC", CachePolicy.OnlyUseCacheUnlessEmpty) ?? 0;

            _exchangeClient = new ExchangeClient();
            _workflowClient = new WorkflowClient();
            var cryptoCompareClient = new CryptoCompareClient();

            _workflow = new ArbitrageWorkflow(_exchangeClient, cryptoCompareClient);
        }

        [TestMethod]
        public void Arbitrage_workflow__basic_scenario()
        {
            var data = new ArbitrageData
            {
                EthToBtcRatio = 0.1m,
                BtcToUsdRatio = 8000.0m,
                SourceWithdrawalFee = 1,

                SourceBtcOrderBook = new OrderBook
                {
                    Asks = new List<Order>
                    {
                        new Order { Quantity = 10, Price = 0.1m }
                    }
                },
                SourceEthOrderBook = new OrderBook
                {
                    Asks = new List<Order>
                    {
                        new Order { Quantity = 20, Price = 1.1m }
                    }
                },
                
                DestBtcOrderBook = new OrderBook
                {
                    Bids = new List<Order>
                    {
                        new Order { Quantity = 5m, Price = 0.11m }
                    }
                },
                DestEthOrderBook = new OrderBook
                {
                    Bids = new List<Order>
                    {
                        new Order { Quantity = 15m, Price = 1.2m }
                    }
                }
            };

            var result = _workflow.Execute(data);

            result.Dump();
        }

        [TestMethod]
        public void Arbitrage_workflow__from_json_data()
        {
            var data = ResUtil.Get<ArbitrageData>("arbitrage-data.json", GetType().Assembly);
            var results = _workflow.Execute(data);

            results.Dump();
        }

        [TestMethod]
        public void Arbitrage_workflow__bitcoin_cash()
        {
            var results = _workflow.Execute(IntegrationNameRes.Binance, IntegrationNameRes.Coss, "BCH", CachePolicy.AllowCache);
            results.Dump();
        }

        [TestMethod]
        public void Arbitrage_workflow__link()
        {
            var results = _workflow.Execute(IntegrationNameRes.Binance, IntegrationNameRes.Coss, "LINK", CachePolicy.AllowCache);
            results.Dump();
        }

        [TestMethod]
        public void Arbitrage_workflow__ark__binance_to_coss__force_refresh()
        {
            var results = _workflow.Execute(IntegrationNameRes.Binance, IntegrationNameRes.Coss, "ARK", CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Arbitrage_workflow__bch__binance_to_coss__force_refresh()
        {
            var results = _workflow.Execute(IntegrationNameRes.Binance, IntegrationNameRes.Coss, "BCH", CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Arbitrage_workflow__adh__hitbtc_to_qryptos__force_refresh()
        {
            var results = _workflow.Execute(IntegrationNameRes.HitBtc, IntegrationNameRes.Qryptos, "ADH", CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Arbitrage_workflow__stx__coss_to_qryptos__force_refresh()
        {
            var results = _workflow.Execute(IntegrationNameRes.Coss, IntegrationNameRes.Qryptos, "STX", CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Arbitrage_workflow__omisego__coss_to_binance()
        {
            var results = _workflow.Execute(IntegrationNameRes.Coss, IntegrationNameRes.Binance, "OMG", CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Arbitrage_workflow__check__havven_kucoin_to_qryptos__force_refresh()
        {
            var results = _workflow.Execute(IntegrationNameRes.KuCoin, IntegrationNameRes.Qryptos, CommodityRes.Havven, CachePolicy.ForceRefresh);
            results.Dump();            
        }

        [TestMethod]
        public void Arbitrage_workflow__check__fyn__coss_hitbtc__force_refresh()
        {
            var results = _workflow.Execute(IntegrationNameRes.Coss, IntegrationNameRes.HitBtc, "FYN", CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Arbitrage_workflow__check__fxt__hitbtc_livecoin__force_refresh()
        {
            var results = _workflow.Execute(IntegrationNameRes.HitBtc, IntegrationNameRes.Livecoin, "FXT", CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Arbitrage_workflow__check__fxt__hitbtc_livecoin__allow_cache()
        {
            var results = _workflow.Execute(IntegrationNameRes.HitBtc, IntegrationNameRes.Livecoin, "FXT", CachePolicy.AllowCache);
            results.Dump();
        }

        [TestMethod]
        public void Arbitrage_workflow__check__nox__coss_to_livecoin__force_refresh()
        {
            var results = _workflow.Execute(IntegrationNameRes.Coss, IntegrationNameRes.Livecoin, "NOX", CachePolicy.AllowCache);
            results.Dump();
        }

        [TestMethod]
        public void Arbitrage_workflow__check__fyn__coss_hitbtc__only_use_cache_unless_empty()
        {
            var results = _workflow.Execute(IntegrationNameRes.Coss, IntegrationNameRes.HitBtc, "FYN", CachePolicy.OnlyUseCacheUnlessEmpty);
            results.Dump();
        }

        [TestMethod]
        public void Arbitrage_workflow__check__act__kucoin_to_hitbtc__force_refresh()
        {
            var results = _workflow.Execute(IntegrationNameRes.KuCoin, IntegrationNameRes.HitBtc, "ACT", CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Arbitrage_workflow__check__ark__coss_to_binance__force_refresh()
        {
            var results = _workflow.Execute(IntegrationNameRes.Coss, IntegrationNameRes.Binance, "ARK", CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Arbitrage_workflow__check__rntb__hitbtc_to_bitz__force_refresh()
        {
            var results = _workflow.Execute(IntegrationNameRes.HitBtc, IntegrationNameRes.Bitz, "RNTB", CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Arbitrage_workflow__check__etn__kucoin_to_qryptos__force_refresh()
        {
            var results = _workflow.Execute(IntegrationNameRes.KuCoin, IntegrationNameRes.Qryptos, "ETN", CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Arbitrage_workflow__check__ark__binance_to_coss__force_refresh()
        {
            var results = _workflow.Execute(IntegrationNameRes.Binance, IntegrationNameRes.Coss, "ARK", CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Arbitrage_workflow__check__ark__bitz_to_binance__force_refresh()
        {
            var results = _workflow.Execute(IntegrationNameRes.Bitz, IntegrationNameRes.Binance, "ARK", CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Arbitrage_workflow__check__pay__coss_to_kucoin__force_refresh()
        {
            var results = _workflow.Execute(IntegrationNameRes.Coss, IntegrationNameRes.KuCoin, CommodityRes.PayTenX, CachePolicy.ForceRefresh);
            results.Dump();
        }
        
        [TestMethod]
        public void Arbitrage_workflow__check__prl__coss_to_kucoin__force_refresh()
        {
            var results = _workflow.Execute(IntegrationNameRes.Coss, IntegrationNameRes.KuCoin, CommodityRes.OysterPearl, CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Arbitrage_workflow__check__prl__kucoin_to_coss__force_refresh()
        {
            var results = _workflow.Execute(IntegrationNameRes.KuCoin, IntegrationNameRes.Coss, CommodityRes.OysterPearl, CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Arbitrage_workflow__check__poe__binance_to_coss__force_refresh()
        {
            var results = _workflow.Execute(IntegrationNameRes.Binance, IntegrationNameRes.Coss, CommodityRes.Poe, CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Arbitrage_workflow__check__snm__binance_to_coss__force_refresh()
        {
            var results = _workflow.Execute(IntegrationNameRes.Binance, IntegrationNameRes.Coss, CommodityRes.Sonm, CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Arbitrage_workflow__check__snm__coss_to_binance__force_refresh()
        {
            var results = _workflow.Execute(IntegrationNameRes.Coss, IntegrationNameRes.Binance, CommodityRes.Sonm, CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Arbitrage_workflow__check__sub__binance_to_coss__force_refresh()
        {
            var results = _workflow.Execute(IntegrationNameRes.Binance, IntegrationNameRes.Coss, CommodityRes.Substratum, CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Arbitrage_workflow__check__la__kucoin_to_coss__force_refresh()
        {
            var results = _workflow.Execute(IntegrationNameRes.KuCoin, IntegrationNameRes.Coss, CommodityRes.LaToken, CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Arbitrage_workflow__check__la__coss_to_kucoin__force_refresh()
        {
            var results = _workflow.Execute(IntegrationNameRes.Coss, IntegrationNameRes.KuCoin, CommodityRes.LaToken, CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Arbitrage_workflow__check__la__coss_to_hitbtc__force_refresh()
        {
            var results = _workflow.Execute(IntegrationNameRes.Coss, IntegrationNameRes.HitBtc, CommodityRes.LaToken, CachePolicy.ForceRefresh);
            results.Dump();
        }


        [TestMethod]
        public void Arbitrage_workflow__check__lala__kucoin_to_coss__force_refresh()
        {
            var results = _workflow.Execute(IntegrationNameRes.KuCoin, IntegrationNameRes.Coss, CommodityRes.LaLaWorld, CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Arbitrage_workflow__check__lala__kucoin_to_qryptos__force_refresh()
        {
            var results = _workflow.Execute(IntegrationNameRes.KuCoin, IntegrationNameRes.Qryptos, CommodityRes.LaLaWorld, CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Arbitrage_workflow__check__link__binance_to_coss__force_refresh()
        {
            var results = _workflow.Execute(IntegrationNameRes.Binance, IntegrationNameRes.Coss, "LINK", CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Arbitrage_workflow__check__omg__kucoin_to_coss__force_refresh()
        {
            var results = _workflow.Execute(IntegrationNameRes.KuCoin, IntegrationNameRes.Coss, CommodityRes.Omisego, CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Arbitrage_workflow__check__etc__kucoin_to_qryptos__only_use_cache_unless_empty()
        {
            var results = _workflow.Execute(IntegrationNameRes.KuCoin, IntegrationNameRes.Qryptos, CommodityRes.EthereumClassic, CachePolicy.OnlyUseCacheUnlessEmpty);
            results.Dump();
        }

        [TestMethod]
        public void Arbitrage_workflow__check__etc__qryptos_to_kucoin__only_use_cache_unless_empty()
        {
            var results = _workflow.Execute(IntegrationNameRes.Qryptos, IntegrationNameRes.KuCoin, CommodityRes.EthereumClassic, CachePolicy.OnlyUseCacheUnlessEmpty);
            results.Dump();
        }

        [TestMethod]
        public void Arbitrage_workflow__check__wtc__kucoin_to_coss__force_refresh()
        {
            var results = _workflow.Execute(IntegrationNameRes.KuCoin, IntegrationNameRes.Coss, CommodityRes.WaltonChain, CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Arbitrage_workflow__check__snm__kucoin_to_coss__force_refresh()
        {
            var results = _workflow.Execute(IntegrationNameRes.KuCoin, IntegrationNameRes.Coss, CommodityRes.Sonm, CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Arbitrage_workflow__check__enj__binance_to_coss__force_refresh()
        {
            var results = _workflow.Execute(IntegrationNameRes.Binance, IntegrationNameRes.Coss, CommodityRes.EnjinCoin, CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Arbitrage_workflow__check__pay__hitbtc_to_coss__force_refresh()
        {
            var results = _workflow.Execute(IntegrationNameRes.HitBtc, IntegrationNameRes.Coss, CommodityRes.PayTenX, CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Arbitrage_workflow__check__pay__kucoin_to_coss__force_refresh()
        {
            var results = _workflow.Execute(IntegrationNameRes.KuCoin, IntegrationNameRes.Coss, CommodityRes.PayTenX, CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Arbitrage_workflow__check__nox_livecoin_to_coss__force_refresh()
        {
            var results = _workflow.Execute(IntegrationNameRes.Livecoin, IntegrationNameRes.Coss, CommodityRes.Nitro, CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Arbitrage_workflow__check__fyn__hitbtc_to_coss__force_refresh()
        {
            var results = _workflow.Execute(IntegrationNameRes.HitBtc, IntegrationNameRes.Coss, CommodityRes.FundYourselfNow, CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Arbitrage_workflow__check__gat__kucoin_to_coss__force_refresh()
        {
            var results = _workflow.Execute(IntegrationNameRes.KuCoin, IntegrationNameRes.Coss, "GAT", CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Arbitrage_workflow__check__hgt__hitbtc_to_coss__force_refresh()
        {
            var results = _workflow.Execute(IntegrationNameRes.HitBtc, IntegrationNameRes.Coss, "HGT", CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Arbitrage_workflow__check__gat__coss_to_kucoin__force_refresh()
        {
            var results = _workflow.Execute(IntegrationNameRes.Coss, IntegrationNameRes.KuCoin, "GAT", CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Arbitrage_workflow__check__penta__bitz_to_hitbtc__force_refresh()
        {
            var results = _workflow.Execute(IntegrationNameRes.Bitz, IntegrationNameRes.HitBtc, CommodityRes.Penta, CachePolicy.ForceRefresh);
            results.Dump();
        }

        // TheKey (TKY)
        [TestMethod]
        public void Arbitrage_workflow__check__key__kucoin_to_bitz__force_refresh()
        {
            var results = _workflow.Execute(IntegrationNameRes.KuCoin, IntegrationNameRes.Bitz, "tky", CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Arbitrage_workflow__check__req__binance_to_coss__force_refresh()
        {
            var results = _workflow.Execute(IntegrationNameRes.Binance, IntegrationNameRes.Coss, CommodityRes.RequestNetwork, CachePolicy.ForceRefresh);
            results.Dump();
        }


        [TestMethod]
        public void Arbitrage_workflow__check__ixt__hitbtc_to_qryptos__force_refresh()
        {
            var results = _workflow.Execute(IntegrationNameRes.HitBtc, IntegrationNameRes.Qryptos, "ixt", CachePolicy.ForceRefresh);
            results.Dump();
        }

        // ASCH (XAS)
        [TestMethod]
        public void Arbitrage_workflow__check__asch__bitz_to_kucoin__force_refresh()
        {
            var results = _workflow.Execute(IntegrationNameRes.Bitz, IntegrationNameRes.KuCoin, CommodityRes.Asch, CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Arbitrage_workflow__check__blz__coss_to_binance__force_refresh()
        {
            var results = _workflow.Execute(IntegrationNameRes.Coss, IntegrationNameRes.Binance, CommodityRes.Bluzelle, CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Arbitrage_workflow__check__stu__qryptos_to_hitbtc__force_refresh()
        {
            var results = _workflow.Execute(IntegrationNameRes.Qryptos, IntegrationNameRes.HitBtc, "STU", CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Arbitrage_workflow__check__stx__hitbtc_to_coss__force_refresh()
        {
            var results = _workflow.Execute(IntegrationNameRes.HitBtc, IntegrationNameRes.Coss, "STX", CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Arbitrage_workflow__check__dat__coss_to_kucoin__force_refresh()
        {
            var results = _workflow.Execute(IntegrationNameRes.Coss, IntegrationNameRes.KuCoin, "DAT", CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Arbitrage_workflow__check__dat__kucoin_to_coss__force_refresh()
        {
            var results = _workflow.Execute(IntegrationNameRes.KuCoin, IntegrationNameRes.Coss, "DAT", CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Arbitrage_workflow__check__bch__binance_to_coss__force_refresh()
        {
            var results = _workflow.Execute(IntegrationNameRes.Binance, IntegrationNameRes.Coss, CommodityRes.BitcoinCash, CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Arbitrage_workflow__check__bch__coss_to_binance__force_refresh()
        {
            var results = _workflow.Execute(IntegrationNameRes.Coss, IntegrationNameRes.Binance, CommodityRes.BitcoinCash, CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Arbitrage_workflow__check__blz__binance_to_coss__force_refresh()
        {
            var results = _workflow.Execute(IntegrationNameRes.Binance, IntegrationNameRes.Coss, CommodityRes.Bluzelle, CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Arbitrage_workflow__check__sub__coss_to_binance__force_refresh()
        {
            var results = _workflow.Execute(IntegrationNameRes.Coss, IntegrationNameRes.Binance, CommodityRes.Substratum, CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Arbitrage_workflow__check__time__kucoin_to_livecoin__force_refresh()
        {
            var results = _workflow.Execute(IntegrationNameRes.KuCoin, IntegrationNameRes.Livecoin, "TIME", CachePolicy.ForceRefresh);
            results.Dump();
        }


        [TestMethod]
        public void Arbitrage_workflow__check__can__kucoin_to_qryptos__force_refresh()
        {
            var results = _workflow.Execute(IntegrationNameRes.KuCoin, IntegrationNameRes.Qryptos, CommodityRes.CanYaCoin, CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Arbitrage_workflow__check__can__kucoin_to_coss__force_refresh()
        {
            var results = _workflow.Execute(IntegrationNameRes.KuCoin, IntegrationNameRes.Coss, CommodityRes.CanYaCoin, CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Arbitrage_workflow__check__cvc__binance_to_coss__force_refresh()
        {
            var results = _workflow.Execute(IntegrationNameRes.Binance, IntegrationNameRes.Coss, CommodityRes.Civic, CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Arbitrage_workflow__check__cs__kucoin_to_coss__force_refresh()
        {
            var results = _workflow.Execute(IntegrationNameRes.KuCoin, IntegrationNameRes.Coss, "CS", CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Arbitrage_workflow__check__bwt__idex_to_coss__force_refresh()
        {
            var results = _workflow.Execute(IntegrationNameRes.Idex, IntegrationNameRes.Coss, "BWT", CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Arbitrage_workflow__check__dent__binance_to_qryptos__force_refresh()
        {
            var results = _workflow.Execute(IntegrationNameRes.Binance, IntegrationNameRes.Qryptos, "DENT", CachePolicy.ForceRefresh);
            results.Dump();
        }
        
        [TestMethod]
        public void Arbitrage_workflow__check__wtc__binance_to_coss__force_refresh()
        {
            var results = _workflow.Execute(IntegrationNameRes.Binance, IntegrationNameRes.Coss, CommodityRes.WaltonChain, CachePolicy.ForceRefresh);
            results.Dump();
        }
        
        [TestMethod]
        public void Arbitrage_workflow__check__mtn__kucoin_to_qryptos__force_refresh()
        {
            var results = _workflow.Execute(IntegrationNameRes.KuCoin, IntegrationNameRes.Qryptos, "mtn", CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Arbitrage_workflow__check__mith__hitbtc_to_qryptos__force_refresh()
        {
            var results = _workflow.Execute(IntegrationNameRes.HitBtc, IntegrationNameRes.HitBtc, "MITH", CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Arbitrage_workflow__check__xnk__coss_to_qryptos__force_refresh()
        {
            var results = _workflow.Execute(IntegrationNameRes.Coss, IntegrationNameRes.Qryptos, "XNK", CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Arbitrage_workflow__check__dent__kucoin_to_qryptos__force_refresh()
        {
            var results = _workflow.Execute(IntegrationNameRes.KuCoin, IntegrationNameRes.Qryptos, CommodityRes.Dent, CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Arbitrage_workflow__check__xem__binance_to_coss__force_refresh()
        {
            var results = _workflow.Execute(IntegrationNameRes.Binance, IntegrationNameRes.Coss, CommodityRes.NewEconomyMovement, CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Arbitrage_workflow__check_all_binance_to_coss__allow_cache()
        {
            Arbitrage_workflow(IntegrationNameRes.Binance, IntegrationNameRes.Coss, CachePolicy.AllowCache);
        }

        [TestMethod]
        public void Arbitrage_workflow__check_all_binance_to_coss__force_refresh()
        {
            Arbitrage_workflow(IntegrationNameRes.Binance, IntegrationNameRes.Coss, CachePolicy.ForceRefresh, null, CossSymbolsToAvoid);
        }

        [TestMethod]
        public void Arbitrage_workflow__check_all_binance_to_bitz__allow_cache()
        {
            Arbitrage_workflow(IntegrationNameRes.Binance, IntegrationNameRes.Bitz, CachePolicy.AllowCache, null, null);
        }

        [TestMethod]
        public void Arbitrage_workflow__check_all_coss_to_binance__allow_cache()
        {
            Arbitrage_workflow(IntegrationNameRes.Coss, IntegrationNameRes.Binance, CachePolicy.AllowCache, null, CossSymbolsToAvoid);
        }

        [TestMethod]
        public void Arbitrage_workflow__check_all_hitbtc_to_coss__allow_cache()
        {
            Arbitrage_workflow(IntegrationNameRes.HitBtc, IntegrationNameRes.Coss, CachePolicy.AllowCache, null, CossSymbolsToAvoid);
        }

        [TestMethod]
        public void Arbitrage_workflow__check_all_coss_to_hitbtc__force_refresh()
        {
            Arbitrage_workflow(IntegrationNameRes.Coss, IntegrationNameRes.HitBtc, CachePolicy.ForceRefresh);
        }

        [TestMethod]
        public void Arbitrage_workflow__check_all_coss_to_hitbtc__allow_cache()
        {
            Arbitrage_workflow(IntegrationNameRes.Coss, IntegrationNameRes.HitBtc, CachePolicy.AllowCache);
        }

        [TestMethod]
        public void Arbitrage_workflow__check_all_coss_to_binance__force_refresh()
        {
            Arbitrage_workflow(IntegrationNameRes.Coss, IntegrationNameRes.Binance, CachePolicy.ForceRefresh, null, CossSymbolsToAvoid);
        }

        [TestMethod]
        public void Arbitrage_workflow__check_lampix_hitbtc_to_coss__force_refresh()
        {
            Arbitrage_workflow(IntegrationNameRes.HitBtc, IntegrationNameRes.Coss, CachePolicy.ForceRefresh, "PIX");
        }

        [TestMethod]
        public void Arbitrage_workflow__check_all_coss_to_kucoin__allow_cache()
        {
            Arbitrage_workflow(IntegrationNameRes.Coss, IntegrationNameRes.KuCoin, CachePolicy.AllowCache);
        }

        [TestMethod]
        public void Arbitrage_workflow__check_all_coss_to_kucoin__force_refresh()
        {
            Arbitrage_workflow(IntegrationNameRes.Coss, IntegrationNameRes.KuCoin, CachePolicy.ForceRefresh);
        }

        [TestMethod]
        public void Arbitrage_workflow__check_all_kucoin_to_coss__force_refresh()
        {
            Arbitrage_workflow(IntegrationNameRes.KuCoin, IntegrationNameRes.Coss, CachePolicy.ForceRefresh);
        }

        [TestMethod]
        public void Arbitrage_workflow__check_all_kucoin_to_livecoin__force_refresh()
        {
            Arbitrage_workflow(IntegrationNameRes.KuCoin, IntegrationNameRes.Livecoin, CachePolicy.ForceRefresh);
        }

        [TestMethod]
        public void Arbitrage_workflow__check_all_kucoin_to_qryptos__allow_cache()
        {
            Arbitrage_workflow(IntegrationNameRes.KuCoin, IntegrationNameRes.Qryptos, CachePolicy.AllowCache);
        }

        [TestMethod]
        public void Arbitrage_workflow__check_all_kucoin_to_qryptos__force_refresh()
        {
            Arbitrage_workflow(IntegrationNameRes.KuCoin, IntegrationNameRes.Qryptos, CachePolicy.ForceRefresh);
        }

        [TestMethod]
        public void Arbitrage_workflow__check_all_kucoin_to_livecoin__allow_cache()
        {
            Arbitrage_workflow(IntegrationNameRes.KuCoin, IntegrationNameRes.Livecoin, CachePolicy.ForceRefresh);
        }

        [TestMethod]
        public void Arbitrage_workflow__check_all_kucoin_to_coss__allow_cache()
        {
            Arbitrage_workflow(IntegrationNameRes.KuCoin, IntegrationNameRes.Coss, CachePolicy.AllowCache);
        }

        [TestMethod]
        public void Arbitrage_workflow__check_all_kucoin_to_yobit__allow_cache()
        {
            Arbitrage_workflow(IntegrationNameRes.KuCoin, IntegrationNameRes.Yobit, CachePolicy.AllowCache);
        }

        [TestMethod]
        public void Arbitrage_workflow__act_on_arbitrage_result__knc__kucoin_to_coss()
        {
            var commodity = CommodityRes.Knc;

            Act_on_arbitrage_result(IntegrationNameRes.KuCoin, IntegrationNameRes.Coss, commodity);
        }

        [TestMethod]
        public void Arbitrage_workflow__act_on_arbitrage_result__la__kucoin_to_coss()
        {
            var commodity = CommodityRes.LaToken;

            Act_on_arbitrage_result(IntegrationNameRes.KuCoin, IntegrationNameRes.Coss, commodity);
        }

        [TestMethod]
        public void Arbitrage_workflow__act_on_arbitrage_result__wtc__binance_to_coss()
        {
            var commodity = CommodityRes.WaltonChain;
            Act_on_arbitrage_result(IntegrationNameRes.Binance, IntegrationNameRes.Coss, commodity);
        }

        [TestMethod]
        public void Arbitrage_workflow__act_on_arbitrage_result__ark__binance_to_coss()
        {
            var commodity = CommodityRes.Ark;

            Act_on_arbitrage_result(IntegrationNameRes.Binance, IntegrationNameRes.Coss, commodity);
        }

        [TestMethod]
        public void Arbitrage_workflow__act_on_arbitrage_result__enj__binance_to_coss()
        {
            var commodity = CommodityRes.EnjinCoin;

            Act_on_arbitrage_result(IntegrationNameRes.Binance, IntegrationNameRes.Coss, commodity);
        }

        [TestMethod]
        public void Arbitrage_workflow__act_on_arbitrage_result__cvc__binance_to_coss()
        {
            var commodity = CommodityRes.Civic;

            Act_on_arbitrage_result(IntegrationNameRes.Binance, IntegrationNameRes.Coss, commodity);
        }

        [TestMethod]
        public void Arbitrage_workflow__act_on_arbitrage_result__can__kucoin_to_qryptos()
        {
            var commodity = CommodityRes.CanYaCoin;

            Act_on_arbitrage_result(IntegrationNameRes.KuCoin, IntegrationNameRes.Qryptos, commodity, true);
        }

        [TestMethod]
        public void Arbitrage_workflow__act_on_arbitrage_result__can__kucoin_to_coss()
        {
            var commodity = CommodityRes.CanYaCoin;

            Act_on_arbitrage_result(IntegrationNameRes.KuCoin, IntegrationNameRes.Coss, commodity, true);
        }

        [TestMethod]
        public void Arbitrage_workflow__act_on_arbitrage_result__prl__kucoin_to_coss()
        {
            var commodity = CommodityRes.OysterPearl;

            Act_on_arbitrage_result(IntegrationNameRes.KuCoin, IntegrationNameRes.Coss, commodity);
        }

        [TestMethod]
        public void Arbitrage_workflow__act_on_arbitrage_result__fota__kucoin_to_hitbtc()
        {
            var commodity = CommodityRes.Fortuna;

            Act_on_arbitrage_result(IntegrationNameRes.KuCoin, IntegrationNameRes.HitBtc, commodity);
        }

        [TestMethod]
        public void Arbitrage_workflow__act_on_arbitrage_result__key__kucoin_to_bitz()
        {
            Act_on_arbitrage_result(IntegrationNameRes.KuCoin, IntegrationNameRes.Bitz, CommodityRes.TheKey);
        }

        [TestMethod]
        public void Arbitrage_workflow__act_on_arbitrage_result__poe__binance_to_coss()
        {
            var commodity = CommodityRes.Poe;

            Act_on_arbitrage_result(IntegrationNameRes.Binance, IntegrationNameRes.Coss, commodity);
        }

        [TestMethod]
        public void Arbitrage_workflow__act_on_arbitrage_result__req__binance_to_coss()
        {
            var commodity = CommodityRes.RequestNetwork;

            Act_on_arbitrage_result(IntegrationNameRes.Binance, IntegrationNameRes.Coss, commodity);
        }

        [TestMethod]
        public void Arbitrage_workflow__act_on_arbitrage_result__bnt__binance_to_coss()
        {
            var commodity = CommodityRes.BancorNetworkToken;
            Act_on_arbitrage_result(IntegrationNameRes.Binance, IntegrationNameRes.Coss, commodity);
        }

        [TestMethod]
        public void Arbitrage_workflow__act_on_arbitrage_result__knc__binance_to_coss()
        {
            var commodity = CommodityRes.Knc;
            Act_on_arbitrage_result(IntegrationNameRes.Binance, IntegrationNameRes.Coss, commodity);
        }

        [TestMethod]
        public void Arbitrage_workflow__act_on_arbitrage_result__blz__binance_to_coss()
        {
            var commodity = CommodityRes.Bluzelle;

            Act_on_arbitrage_result(IntegrationNameRes.Binance, IntegrationNameRes.Coss, commodity);
        }


        [TestMethod]
        public void Arbitrage_workflow__act_on_arbitrage_result__bch__binance_to_coss()
        {
            var commodity = CommodityRes.BitcoinCash;

            Act_on_arbitrage_result(IntegrationNameRes.Binance, IntegrationNameRes.Coss, commodity);
        }

        [TestMethod]
        public void Arbitrage_workflow__act_on_arbitrage_result__snm__binance_to_coss()
        {
            var commodity = CommodityRes.Sonm;

            Act_on_arbitrage_result(IntegrationNameRes.Binance, IntegrationNameRes.Coss, commodity);
        }

        [TestMethod]
        public void Arbitrage_workflow__act_on_arbitrage_result__ont__binance_to_kucoin()
        {
            var commodity = CommodityRes.Ontology;

            Act_on_arbitrage_result(IntegrationNameRes.Binance, IntegrationNameRes.KuCoin, commodity);
        }

        [TestMethod]
        public void Arbitrage_workflow__act_on_arbitrage_result__omisego__binance_to_coss()
        {
            var commodity = CommodityRes.Omisego;

            Act_on_arbitrage_result(IntegrationNameRes.Binance, IntegrationNameRes.Coss, commodity);
        }

        [TestMethod]
        public void Arbitrage_workflow__act_on_arbitrage_result__omg__kucoin_to_coss()
        {
            var commodity = CommodityRes.Omisego;

            Act_on_arbitrage_result(IntegrationNameRes.KuCoin, IntegrationNameRes.Coss, commodity);
        }

        [TestMethod]
        public void Arbitrage_workflow__act_on_arbitrage_result__ven__binance_to_coss()
        {
            var commodity = CommodityRes.VeChain;

            Act_on_arbitrage_result(IntegrationNameRes.Binance, IntegrationNameRes.Coss, commodity);
        }

        [TestMethod]
        public void Arbitrage_workflow__act_on_arbitrage_result__ont__kucoin_to_livecoin()
        {
            var commodity = CommodityRes.Ontology;

            Act_on_arbitrage_result(IntegrationNameRes.KuCoin, IntegrationNameRes.Livecoin, commodity);
        }

        [TestMethod]
        public void Arbitrage_workflow__act_on_arbitrage_result__dent__kucoin_to_qryptos()
        {
            var commodity = CommodityRes.Dent;

            Act_on_arbitrage_result(IntegrationNameRes.KuCoin, IntegrationNameRes.Qryptos, commodity);
        }

        [TestMethod]
        public void Arbitrage_workflow__act_on_arbitrage_result__havven__kucoin_to_qryptos()
        {
            var commodity = CommodityRes.Havven;

            Act_on_arbitrage_result(IntegrationNameRes.KuCoin, IntegrationNameRes.Qryptos, commodity);
        }

        private static bool ShouldCommit = false;

        // TODO: Remove this and make the methods pass in the exchange name.
        public void Act_on_arbitrage_result(
            ITradeIntegration source,
            ITradeIntegration destination,
            Commodity commodity,
            bool overrideDepositability = false)
        {
            Act_on_arbitrage_result(source.Name, destination.Name, commodity, overrideDepositability);
        }

        public void Act_on_arbitrage_result(
            string source,
            string destination,
            Commodity commodity,
            bool overrideDepositability = false)
        {
            if (!ShouldCommit)
            {
                Assert.Fail($"This test actually buys things. Turn on \"{nameof(ShouldCommit)}\" to commit.");
                return;
            }

            var getDepositAddresTask = Task.Run(() => _exchangeClient.GetDepositAddress(destination, commodity.Symbol, CachePolicy.ForceRefresh));
            var getSourceCommoditiesTask = Task.Run(() => _exchangeClient.GetCommoditiesForExchange(source, CachePolicy.ForceRefresh));
            var destinationCommoditiesTask = Task.Run(() => _exchangeClient.GetCommoditiesForExchange(destination, CachePolicy.ForceRefresh));

            var depositAddress = getDepositAddresTask.Result;
            if (depositAddress == null || string.IsNullOrWhiteSpace(depositAddress.Address))
            {
                Console.WriteLine("Deposit address not available.");
                return; 
            }

            var sourceCommodities = getSourceCommoditiesTask.Result;
            var sourceCommodity = (sourceCommodities ?? new List<CommodityForExchange>())
                .SingleOrDefault(item => string.Equals(item.Symbol, commodity.Symbol, StringComparison.InvariantCultureIgnoreCase));

            if (sourceCommodity == null)
            {
                Console.WriteLine("Unable to retrieve source commodity.");
                return;
            }

            if (!sourceCommodity.CanWithdraw.HasValue)
            {
                Console.WriteLine("Source's withdrawability not specified.");
                return;
            }

            if (!sourceCommodity.CanWithdraw.Value)
            {
                Console.WriteLine("Source does not permit withdrawals of this commodity.");
                return;
            }

            var destinationCommodities = destinationCommoditiesTask.Result;
            var destinationCommodity = (destinationCommodities ?? new List<CommodityForExchange>())
                .SingleOrDefault(item => string.Equals(item.Symbol, commodity.Symbol, StringComparison.InvariantCultureIgnoreCase));

            if (destinationCommodity == null)
            {
                Console.WriteLine("Unable to retrieve destination commodity.");
                return;
            }

            if (!destinationCommodity.CanDeposit.HasValue && !overrideDepositability)
            {
                Console.WriteLine("Destination's depositability not specified.");
                return;
            }

            if (destinationCommodity.CanDeposit.HasValue && !destinationCommodity.CanDeposit.Value)
            {
                Console.WriteLine("Destination does not permit deposits of this commodity.");
                return;
            }

            var results = _workflow.Execute(source, destination, commodity.Symbol, CachePolicy.ForceRefresh);
            if (results.TotalQuantity <= 0)
            {
                Console.WriteLine("No profits.");
                return;
            }

            var sourceHoldings = _exchangeClient.GetBalances(source, CachePolicy.ForceRefresh);
            var ethAvailable = sourceHoldings.GetAvailableForSymbol("ETH");
            var btcAvailable = sourceHoldings.GetAvailableForSymbol("BTC");

            var issues = new List<string>();
            if (results.EthNeeded.HasValue && results.EthNeeded.Value > 0 && ethAvailable < results.EthNeeded.Value)
            {
                issues.Add($"We need {results.EthQuantity} but we only have {ethAvailable}.");
            }

            if (results.BtcNeeded.HasValue && results.BtcNeeded.Value > 0 && ethAvailable < results.BtcNeeded.Value)
            {
                issues.Add($"We need {results.BtcQuantity} but we only have {btcAvailable}.");
            }

            if (issues.Any())
            {
                Console.WriteLine(string.Join(Environment.NewLine, issues));
                return;
            }

            if (results.EthQuantity > 0)
            {
                var tradingPair = new TradingPair(commodity.Symbol, "ETH");
                var quantityAndPrice = new QuantityAndPrice
                {
                    Quantity = results.EthQuantity,
                    Price = results.EthPrice.Value
                };

                _exchangeClient.BuyLimit(source, tradingPair.Symbol, tradingPair.BaseSymbol, quantityAndPrice);
            }

            if (results.BtcQuantity > 0)
            {
                var tradingPair = new TradingPair(commodity.Symbol, "BTC");
                var quantityAndPrice = new QuantityAndPrice
                {
                    Quantity = results.BtcQuantity,
                    Price = results.BtcPrice.Value
                };

                _exchangeClient.BuyLimit(source, tradingPair.Symbol, tradingPair.BaseSymbol, quantityAndPrice);
            }

            var holding = _exchangeClient.GetBalance(source, commodity.Symbol, CachePolicy.ForceRefresh);
            var available = holding.Available;

            var amountStillNeeded = results.TotalQuantity - available;

            if (amountStillNeeded > 0 && (amountStillNeeded / results.TotalQuantity) > 0.05m)
            {
                throw new ApplicationException($"Tried to buy {results.TotalQuantity} {commodity.Symbol} from {source} but only ended up with {available}.");
            }
     
            var quantityToWithdraw = available;
            var shouldSubtractWithdrawalFee = string.Equals(source, IntegrationNameRes.KuCoin, StringComparison.InvariantCultureIgnoreCase);
            if (shouldSubtractWithdrawalFee)
            {
                var withdrawalFee = _exchangeClient.GetWithdrawalFee(source, commodity.Symbol, CachePolicy.ForceRefresh);

                quantityToWithdraw -= withdrawalFee.Value;
            }

            Console.WriteLine($"Transferring {quantityToWithdraw} {commodity.Symbol}.");
            var withdrawalResult = _exchangeClient.Withdraw(source, commodity.Symbol, quantityToWithdraw, depositAddress);

            withdrawalResult.Dump();
        }

        [TestMethod]
        public void Arbitrage_workflow__check_all_idex_to_binance()
        {
            var cachePolicy = CachePolicy.ForceRefresh;

            var source = IntegrationNameRes.Idex;
            var dest = IntegrationNameRes.Binance;

            var sourceTradingPairsTask = Task.Run(() => _exchangeClient.GetTradingPairs(source, cachePolicy));
            var destTradingPairsTask = Task.Run(() => _exchangeClient.GetTradingPairs(dest, cachePolicy));
            var destTradingPairs = destTradingPairsTask.Result;

            var sourceTradingPairs = sourceTradingPairsTask.Result;

            var intersections = sourceTradingPairs.Where(sourcePair => destTradingPairs.Any(destPair => string.Equals(sourcePair.Symbol, destPair.Symbol, StringComparison.InvariantCultureIgnoreCase))).ToList();

            intersections.Select(item => item.Symbol).ToList().Dump();

            for (var i = 0; i < intersections.Count; i++)
            {
                var tradingPair = intersections[i];
                var binanceOrderBook = _exchangeClient.GetOrderBook(IntegrationNameRes.Binance, tradingPair.Symbol, tradingPair.BaseSymbol, CachePolicy.AllowCache);
            }
        }

        // TODO: Remove this and make the calling methods pass in the integration name.
        public void Arbitrage_workflow(ITradeIntegration source, ITradeIntegration dest, CachePolicy cachePolicy = CachePolicy.ForceRefresh, string symbolToCheck = null)
        {
            Arbitrage_workflow(source.Name, dest.Name, cachePolicy, symbolToCheck);
        }

        public void Arbitrage_workflow(string source, string dest, CachePolicy cachePolicy = CachePolicy.ForceRefresh, string symbolToCheck = null, List<string> symbolsToAvoid = null)
        {
            var valuationsDictionary = new Dictionary<string, decimal>();
            var valuationTask = LongRunningTask.Run(() =>
            {
                var nullableEthValue = _workflowClient.GetUsdValue("ETH", cachePolicy);
                if (!nullableEthValue.HasValue) { throw new ApplicationException("Failed to retrieve ETH value."); }
                valuationsDictionary["ETH"] = nullableEthValue.Value;

                var nullableBtcValue = _workflowClient.GetUsdValue("BTC", cachePolicy);
                if (!nullableBtcValue.HasValue) { throw new ApplicationException("Failed to retrieve BTC value."); }
                valuationsDictionary["BTC"] = nullableBtcValue.Value;
            });

            Console.WriteLine($"Starting at {DateTime.Now} local time.");

            var tradingPairCachePolicy = cachePolicy == CachePolicy.ForceRefresh ? CachePolicy.AllowCache : cachePolicy;
            var sourceTradingPairsTask = Task.Run(() => _exchangeClient.GetTradingPairs(source, tradingPairCachePolicy));
            var destTradingPairsTask = Task.Run(() => _exchangeClient.GetTradingPairs(dest, tradingPairCachePolicy));

            valuationTask.Wait();
            var sourceTradingPairs = sourceTradingPairsTask.Result;
            var destTradingPairs = destTradingPairsTask.Result;

            var intersections = sourceTradingPairs
                .Where(sourceItem => destTradingPairs.Any(destItem => sourceItem.Equals(destItem)))
                .Where(item => item.Symbol != "ETH").ToList();

            var symbolsWithBoth = intersections.Where(item =>
                item.BaseSymbol == "BTC"
                && intersections.Any(queryTradingPair => queryTradingPair.Equals(new TradingPair(item.Symbol, "ETH")))
                && (string.IsNullOrWhiteSpace(symbolToCheck) || string.Equals(item.Symbol, symbolToCheck, StringComparison.InvariantCultureIgnoreCase))
                )
                .Select(item => item.Symbol)
                .OrderBy(item => item)
                .ToList();

            bool wereAnyProfitsFound = false;
            var exceptions = new List<Exception>();
            for (var i = 0; i < symbolsWithBoth.Count; i++)
            {
                var symbol = symbolsWithBoth[i];

                if (symbolsToAvoid != null && symbolsToAvoid.Any(querySymbol => string.Equals(querySymbol, symbol, StringComparison.InvariantCultureIgnoreCase)))
                { continue; }

                try
                {
                    var result = _workflow.Execute(source, dest, symbol, cachePolicy);
                    if (result.TotalQuantity > 0)
                    {
                        if (cachePolicy == CachePolicy.AllowCache)
                        {
                            result = _workflow.Execute(source, dest, symbol, valuationsDictionary, CachePolicy.ForceRefresh);
                        }
                    }
                    if (result.TotalQuantity > 0)
                    {
                        wereAnyProfitsFound = true;
                        Console.WriteLine($"Profit of ${result.ExpectedUsdProfit} found for [{symbol}].");
                        result.Dump();
                    }
                    else
                    {
                        Console.WriteLine($"No profit found for [{symbol}].");
                    }
                }
                catch (Exception exception)
                {
                    Console.WriteLine($"An exception was thrown for {symbol}.");
                    Console.WriteLine(exception);
                    exceptions.Add(exception);
                }
            }

            if (!wereAnyProfitsFound) { Console.WriteLine("No profits found."); }
        }

        public void Arbitrage_workflow_bi_directional(string source, string dest, CachePolicy cachePolicy = CachePolicy.ForceRefresh)
        {
            Console.WriteLine($"Starting at {DateTime.Now} local time.");

            var sourceTradingPairsTask = Task.Run(() => _exchangeClient.GetTradingPairs(source, cachePolicy));
            var destTradingPairsTask = Task.Run(() => _exchangeClient.GetTradingPairs(dest, cachePolicy));

            var sourceTradingPairs = sourceTradingPairsTask.Result;
            var destTradingPairs = destTradingPairsTask.Result;

            var intersections = sourceTradingPairs
                .Where(sourceItem => destTradingPairs.Any(destItem => sourceItem.Equals(destItem)))
                .Where(item => item.Symbol != "ETH").ToList();

            var symbolsWithBoth = intersections.Where(item =>
                item.BaseSymbol == "BTC"
                && intersections.Any(queryTradingPair => queryTradingPair.Equals(new TradingPair(item.Symbol, "ETH"))))
                .Select(item => item.Symbol)
                .OrderBy(item => item)
                .ToList();

            bool wereAnyProfitsFound = false;
            var exceptions = new List<Exception>();
            for (var i = 0; i < symbolsWithBoth.Count; i++)
            {
                var symbol = symbolsWithBoth[i];

                try
                {
                    
                    var result = _workflow.Execute(source, dest, symbol, cachePolicy);
                    if (result.TotalQuantity > 0)
                    {
                        if (cachePolicy == CachePolicy.AllowCache)
                        {
                            result = _workflow.Execute(source, dest, symbol, CachePolicy.ForceRefresh);
                        }
                    }
                    if (result.TotalQuantity > 0)
                    {
                        wereAnyProfitsFound = true;
                        Console.WriteLine($"{source} to {dest} -- Profit of ${result.ExpectedUsdProfit} found for [{symbol}].");
                        result.Dump();
                    }
                    else
                    {
                        Console.WriteLine($"{source} to {dest} -- No profit found for [{symbol}].");
                    }
                }
                catch (Exception exception)
                {
                    Console.WriteLine($"An exception was thrown for {symbol}.");
                    Console.WriteLine(exception);
                    exceptions.Add(exception);
                }
            }

            if (!wereAnyProfitsFound) { Console.WriteLine("No profits found."); }
        }

        [TestMethod]
        public void Compare_trading_pairs()
        {
            var cossTradingPairs = _exchangeClient.GetTradingPairs(IntegrationNameRes.Coss, CachePolicy.OnlyUseCacheUnlessEmpty);            
            var cryptopiaTradingPairs = _exchangeClient.GetTradingPairs(IntegrationNameRes.Cryptopia, CachePolicy.OnlyUseCacheUnlessEmpty);
            var binanceTradingPairs = _exchangeClient.GetTradingPairs(IntegrationNameRes.Binance, CachePolicy.OnlyUseCacheUnlessEmpty);

            var cossBluezelleBtc = cossTradingPairs.Single(item => string.Equals(item.Symbol, "BLZ", StringComparison.InvariantCultureIgnoreCase) && string.Equals(item.BaseSymbol, "BTC", StringComparison.InvariantCultureIgnoreCase));
            var cryptopiaBlazeCoinBtc = cryptopiaTradingPairs.Single(item => string.Equals(item.Symbol, "BLZ", StringComparison.InvariantCultureIgnoreCase) && string.Equals(item.BaseSymbol, "BTC", StringComparison.InvariantCultureIgnoreCase));
            var binanceBluezelleBtc = binanceTradingPairs.Single(item => string.Equals(item.Symbol, "BLZ", StringComparison.InvariantCultureIgnoreCase) && string.Equals(item.BaseSymbol, "BTC", StringComparison.InvariantCultureIgnoreCase));

            cossBluezelleBtc.Equals(cryptopiaBlazeCoinBtc).ShouldBe(false);
            binanceBluezelleBtc.Equals(cossBluezelleBtc).ShouldBe(true);
            binanceBluezelleBtc.Equals(cryptopiaBlazeCoinBtc).ShouldBe(false);

            var cossArkBtc = cossTradingPairs.Single(item => string.Equals(item.Symbol, "ARK", StringComparison.InvariantCultureIgnoreCase) && string.Equals(item.BaseSymbol, "BTC", StringComparison.InvariantCultureIgnoreCase));
            var cryptopiaArkBtc = cryptopiaTradingPairs.Single(item => string.Equals(item.Symbol, "ARK", StringComparison.InvariantCultureIgnoreCase) && string.Equals(item.BaseSymbol, "BTC", StringComparison.InvariantCultureIgnoreCase));

            cossArkBtc.Equals(cryptopiaArkBtc).ShouldBe(true);
        }
    }
}
