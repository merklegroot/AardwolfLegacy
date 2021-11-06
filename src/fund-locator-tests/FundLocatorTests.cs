using cache_lib.Models;
using cryptocompare_client_lib;
using dump_lib;
using client_lib;
using math_lib;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using trade_contracts;
using trade_model;
using trade_res;
using web_util;
using exchange_client_lib;

namespace fund_locator_tests
{
    [TestClass]
    public class FundLocatorTests
    {
        private const string Coss = "coss";
        private const string Bitz = "bitz";
        private const string Binance = "binance";

        private IExchangeClient _exchangeClient;
        private ICryptoCompareClient _cryptoCompareClient;

        [TestInitialize]
        public void Setup()
        {
            var webUtil = new WebUtil();
            var invoker = new ServiceInvoker(webUtil);
            _exchangeClient = new ExchangeClient();
            _cryptoCompareClient = new CryptoCompareClient();
        }

        [TestMethod]
        public void Get_coss_ark_commodity()
        {
            var commodity = _exchangeClient.GetCommoditiyForExchange(Coss, CommodityRes.Ark.Symbol, null, CachePolicy.OnlyUseCacheUnlessEmpty);
            commodity.Dump();
        }

        [TestMethod]
        public void Get_binance_ark_commodity()
        {
            var commodity = _exchangeClient.GetCommoditiyForExchange(Binance, CommodityRes.Ark.Symbol, null, CachePolicy.OnlyUseCacheUnlessEmpty);
            commodity.Dump();
        }

        [TestMethod]
        public void Get_coss_commodities()
        {
            var response = _exchangeClient.GetCommoditiesForExchange(Coss, CachePolicy.OnlyUseCacheUnlessEmpty);
            response.Dump();
        }

        [TestMethod]
        public void Get_coss_ark_deposit_address()
        {
            var result = _exchangeClient.GetDepositAddress(Coss, CommodityRes.Ark.Symbol, CachePolicy.AllowCache);
            result.Dump();
        }

        public class HistoryItemWithExchange : HistoryItemContract
        {
            public string Exchange { get; set; }
        }

        [TestMethod]
        public void Compare_waves_transmissions()
        {
            CompareTransmissions(CommodityRes.Waves);
        }

        [TestMethod]
        public void Compare_ark_transmissions()
        {
            CompareTransmissions(CommodityRes.Ark);
        }

        [TestMethod]
        public void Compare_ark_transmissions__only_use_cache_unless_empty()
        {
            CompareTransmissions(CommodityRes.Ark, CachePolicy.OnlyUseCacheUnlessEmpty);
        }

        [TestMethod]
        public void Compare_lisk_transmissions()
        {
            CompareTransmissions(CommodityRes.Lisk);
        }

        [TestMethod]
        public void Compare_zen_transmissions()
        {
            CompareTransmissions(CommodityRes.ZenCash);
        }

        [TestMethod]
        public void Compare_dash_transmissions()
        {
            CompareTransmissions(CommodityRes.Dash);
        }

        [TestMethod]
        public void Check_all_coss_eth_commodities()
        {
            var commodities = _exchangeClient.GetCommoditiesForExchange(Coss, CachePolicy.OnlyUseCacheUnlessEmpty)
                .Select(querySimpleCommodity =>
                {
                    var commodity = _exchangeClient.GetCommoditiyForExchange(Coss, querySimpleCommodity.Symbol, null, CachePolicy.OnlyUseCacheUnlessEmpty);
                    var depositAddress = _exchangeClient.GetDepositAddress(Coss, querySimpleCommodity.Symbol, CachePolicy.AllowCache);
                    
                    return new { Commodity = commodity, DepositAddress = depositAddress };
                })
                .ToList();

            var symbolAndAddress = 
                commodities
                .Where(item => (item?.DepositAddress?.Address ?? string.Empty).StartsWith("0x"))
                .Select(item =>
                {                    
                    return new
                    {
                       Symbol = item.Commodity.Symbol,
                       DepositAddress = item.DepositAddress.Address
                    };
                }).ToList();

            symbolAndAddress.Dump();
        }

        public void CompareTransmissions(Commodity commodity, CachePolicy cachePolicy = CachePolicy.AllowCache)
        {
            CompareTransmissions(commodity.Symbol, cachePolicy);
        }

        public void CompareTransmissions(string symbol, CachePolicy cachePolicy = CachePolicy.AllowCache)
        {
            DetailedExchangeCommodity cossCommodity = null;
            List<HistoricalTrade> cossExchangeHistory = null;

            DetailedExchangeCommodity binanceCommodity = null;
            List<HistoricalTrade> binanceExchangeHistory = null;

            var cossTask = new Task(() =>
            {
                cossCommodity = _exchangeClient.GetCommoditiyForExchange(Coss, symbol, null, CachePolicy.OnlyUseCacheUnlessEmpty);
                cossExchangeHistory = _exchangeClient.GetExchangeHistory(Coss, 0, cachePolicy)?.History;
            }, TaskCreationOptions.LongRunning);
            cossTask.Start();

            var binanceTask = new Task(() =>
            {
                binanceCommodity = _exchangeClient.GetCommoditiyForExchange(Binance, symbol, null, CachePolicy.OnlyUseCacheUnlessEmpty);
                binanceExchangeHistory = _exchangeClient.GetExchangeHistory(Binance, 0, cachePolicy)?.History;
            }, TaskCreationOptions.LongRunning);
            binanceTask.Start();

            decimal? valuation = null;
            var valuationTask = new Task(() =>
            {
                valuation = _cryptoCompareClient.GetUsdValue(symbol, CachePolicy.ForceRefresh);
            }, TaskCreationOptions.LongRunning);
            valuationTask.Start();

            cossTask.Wait();
            binanceTask.Wait();
            valuationTask.Wait();

            var cossWithdrawalFee = cossCommodity.WithdrawalFee ?? 0;
            var binanceWithdrawalFee = binanceCommodity.WithdrawalFee ?? 0;

            var cossWithdrawalsWithoutDeposits = FindMissingDeposits(cossExchangeHistory, binanceExchangeHistory, symbol, cossWithdrawalFee, binanceCommodity.DepositAddress);
            var binanceWithdrawalsWithoutDeposits = FindMissingDeposits(binanceExchangeHistory, cossExchangeHistory, symbol, binanceWithdrawalFee, cossCommodity.DepositAddress)
                .Where(item => string.Equals(item.WalletAddress, cossCommodity.DepositAddress, StringComparison.InvariantCultureIgnoreCase))
                .ToList();

            Console.WriteLine("Coss withdrawals never received:");
            cossWithdrawalsWithoutDeposits.Dump();
            var cossMissingValue = cossWithdrawalsWithoutDeposits.Sum(item => item.Quantity * valuation ?? 0);
            Console.WriteLine($"Coss missing value: ${cossMissingValue}");

            Console.WriteLine("Binance withdrawals never received:");
            binanceWithdrawalsWithoutDeposits.Dump();
            var binanceMissingValue = binanceWithdrawalsWithoutDeposits.Sum(item => item.Quantity * valuation ?? 0);
            Console.WriteLine($"Binance missing value: ${binanceMissingValue}");
        }

        private List<HistoricalTrade> FindMissingDeposits(
            List<HistoricalTrade> exchangeHistoryA,
            List<HistoricalTrade> exchangeHistoryB,
            string symbol,            
            decimal withdrawalFee,
            string depositAddress)
        {
            var withdrawalsMissingDeposits = new List<HistoricalTrade>();

            var symbolWithdrawls = exchangeHistoryA.Where(item =>
                string.Equals(item.Symbol, symbol, StringComparison.InvariantCultureIgnoreCase)
                && (string.IsNullOrWhiteSpace(item.WalletAddress) || string.Equals(depositAddress, item.WalletAddress, StringComparison.InvariantCultureIgnoreCase))
                && item.TradeType == TradeTypeEnum.Withdraw
                ).ToList();

            var symbolDeposits = exchangeHistoryB.Where(item =>
                string.Equals(item.Symbol, symbol, StringComparison.InvariantCultureIgnoreCase)
                && item.TradeType == TradeTypeEnum.Deposit
                ).ToList();

            foreach (var withdrawal in symbolWithdrawls)
            {
                if (withdrawal.TradeStatus == TradeStatusEnum.Canceled) { continue; }

                var expectedDepositQuantity = withdrawal.Quantity - withdrawalFee;
                var minReceivedQuantity = expectedDepositQuantity - 0.001m;
                var maxReceivedQuantity = expectedDepositQuantity + 0.001m;

                var matchingDeposits = symbolDeposits.Where(queryDeposit =>
                    MathUtil.IsBetween(queryDeposit.Quantity, minReceivedQuantity, maxReceivedQuantity)
                ).ToList();

                if (!matchingDeposits.Any())
                {
                    withdrawalsMissingDeposits.Add(withdrawal);
                }
            }

            return withdrawalsMissingDeposits;
        }
    }
}
