using coss_lib;
using log_lib;
using Newtonsoft.Json;
using OpenQA.Selenium;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using trade_browser_lib.Models;
using trade_lib;
using trade_model;
using wait_for_it_lib;
using web_util;
using config_lib;
using sel_lib;
using System.Threading.Tasks;
using tfa_lib;
using cryptocompare_lib;
using System.Text;
using trade_strategy_lib;
using rabbit_lib;
using trade_constants;
using trade_contracts;
using assembly_lib;
using System.Reflection;
using coss_data_model;
using coss_data_lib;
using res_util_lib;
using coss_agent_lib;
using integration_workflow_lib;
using trade_email_lib;
using trade_contracts.Messages;
using cache_lib.Models;
using trade_res;
using task_lib;
using coss_agent_lib.Strategy;
using System.Threading;
using exchange_client_lib;

namespace trade_browser_lib
{
    public class CossAgent : ICossAgent
    {        
        private const string SessionCollectionName = "coss-session";

        private bool _keepRunning = false;

        private List<TradingPair> _pairsToMonitorInternalProp;
        private List<TradingPair> _pairsToMonitor
        {
            get
            {
                lock (this)
                {
                    if (_pairsToMonitorInternalProp != null) { return _pairsToMonitorInternalProp; }
                    return _pairsToMonitorInternalProp = _cossIntegration.GetTradingPairs(CachePolicy.AllowCache);
                }
            }
        }

        private IRemoteWebDriver _driver;

        private static Random Random = new Random();

        private readonly IWaitForIt _waitForIt;
        private readonly ICossIntegration _cossIntegration;
        private readonly ICossHistoryRepo _cossHistoryRepo;
        private readonly ICryptoCompareIntegration _cryptoCompareIntegration;
        private readonly IWebUtil _webUtil;
        private readonly ILogRepo _log;
        private readonly IOrderManager _orderManager;
        private readonly ICossOpenOrderRepo _openOrderRepo;
        private readonly IConfigRepo _configRepo;
        private readonly IDepositAddressValidator _depositAddressValidator;
        private readonly ITfaUtil _tfaUtil;
        private readonly ICossWebDriverFactory _cossWebDriverFactory;
        private readonly IRabbitConnectionFactory _rabbitConnectionFactory;
        private readonly ICossDriver _cossDriver;
        private readonly IArbitrageWorkflow _arbitrageWorkflow;
        private readonly ITradeEmailUtil _tradeEmailUtil;
        private readonly ICossAutoBuy _cossAutoBuy;
        private readonly ICossAutoOpenBid _cossAutoOpenBid;
        private readonly IExchangeClient _exchangeClient;

        private IRabbitConnection _rabbit;
        private int _iterationOffset = Random.Next();
        private int _iteration = 0;

        public CossAgent(
            IWaitForIt waitForIt,
            ICossIntegration cossIntegration,
            ICossHistoryRepo cossUserTradeHistoryRepo,
            IOrderManager orderManager,
            ICossOpenOrderRepo openOrderRepo,
            IConfigRepo configRepo,
            IWebUtil webUtil,
            IDepositAddressValidator depositAddressValidator,
            ICryptoCompareIntegration cryptoCompareIntegration,
            ITfaUtil tfaUtil,
            ICossWebDriverFactory cossWebDriverFactory,
            IRabbitConnectionFactory rabbitConnectionFactory,
            ICossDriver cossDriver,
            IArbitrageWorkflow arbitrageWorkflow,
            ITradeEmailUtil tradeEmailUtil,

            IExchangeClient exchangeClient,
            ICossAutoBuy cossAutoBuy,
            ICossAutoOpenBid cossAutoOpenBid,

            ILogRepo log)
        {
            _exchangeClient = exchangeClient;

            _cossIntegration = cossIntegration;
            _cossHistoryRepo = cossUserTradeHistoryRepo;
            _cryptoCompareIntegration = cryptoCompareIntegration;
            _log = log;
            _orderManager = orderManager;
            _openOrderRepo = openOrderRepo;
            _configRepo = configRepo;
            _waitForIt = waitForIt;
            _webUtil = webUtil;
            _depositAddressValidator = depositAddressValidator;
            _tfaUtil = tfaUtil;

            _cossDriver = cossDriver;
            _cossWebDriverFactory = cossWebDriverFactory;
            _rabbitConnectionFactory = rabbitConnectionFactory;
            _arbitrageWorkflow = arbitrageWorkflow;
            _tradeEmailUtil = tradeEmailUtil;

            _cossAutoBuy = cossAutoBuy;
            _cossAutoOpenBid = cossAutoOpenBid;
        }

        public void Start()
        {
            _driver = _cossWebDriverFactory.Create();
            _cossDriver.Init(_driver);

            // var cookie = new Cookie()
            // _driver.Manage().Cookies.AddCookie();

            _log.Info("Agent is starting.", TradeEventType.AgentStarted);

            _rabbit = _rabbitConnectionFactory.Connect();
            _rabbit.Listen(TradeRabbitConstants.Queues.CossAgentQueue, OnMessageReceived);

            _keepRunning = true;            
            while (_keepRunning)
            {
                try
                {
                    if (!Processor()) { _keepRunning = false; return; }
                }
                catch (Exception exception)
                {
                    _log.Error(exception);
                    _cossDriver.Sleep(TimeSpan.FromMinutes(5));
                }

                _cossDriver.Sleep(TimeSpan.FromSeconds(20));

                _iteration++;
            }
        }

        public void Stop()
        {
            _keepRunning = false;
            _rabbit.Dispose();
        }

        private void OnMessageReceived(string message)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(message)) { return; }
                var lines = message.Replace("\r\n", "\r").Replace("\n", "\r").Trim().Split('\r');
                if (lines == null || !lines.Any()) { return; }

                Info($"Received:{Environment.NewLine}{message}");

                var messageTypeText = lines.First();
                var messagePayload = string.Join(Environment.NewLine, lines.Skip(1));

                if (string.Equals(messageTypeText, typeof(GetStatusRequestMessage).FullName, StringComparison.Ordinal))
                {
                    var contract = JsonConvert.DeserializeObject<GetStatusRequestMessage>(messagePayload);
                    GetStatusRequestMessageHandler(contract);
                    return;
                }

                if (string.Equals(messageTypeText, typeof(CancelOrderRequestMessage).FullName, StringComparison.Ordinal))
                {
                    var contract = JsonConvert.DeserializeObject<CancelOrderRequestMessage>(messagePayload);
                    CancelOrderRequestMessageHandler(contract);
                    return;
                }

                Console.WriteLine("Didn't recognize the message type.");
            }
            catch (Exception exception)
            {
                Console.WriteLine("Failed to handle message.");
                _log.Error(exception);
            }
        }

        private void GetStatusRequestMessageHandler(GetStatusRequestMessage message)
        {
            Console.WriteLine("GetStatusRequestMessageHandler - Entry point");
            if (!string.IsNullOrWhiteSpace(message.ResponseQueue))
            {
                Console.WriteLine($"GetStatusRequestMessageHandler - sending response to {message.ResponseQueue}");

                _rabbit.PublishContract(message.ResponseQueue, new GetStatusResponseMessage
                {
                    StatusText = (_cossDriver.SessionState.IsLoggedIn ? "Logged in" : "Not logged in") + " as of " + _cossDriver.SessionState.AsOf,
                    ProcessStartTime = Process.GetCurrentProcess().StartTime,
                    BuildDate = AssemblyUtil.GetBuildDate(Assembly.GetExecutingAssembly()),
                    CorrelationId = message.CorrelationId
                });
            }
            else
            {
                Console.WriteLine("GetStatusRequestMessageHandler - Response queue not specified.");
            }
        }

        private List<CancelOrderRequestMessage> _ordersToCancel = new List<CancelOrderRequestMessage>();

        private void CancelOrderRequestMessageHandler(CancelOrderRequestMessage message)
        {
            //if (_ordersToCancel.Any(item => item.)) { }
            //_ordersToCancel.Add(message);
            Info("I'll cancel it when I get around to it.");
        }

        private bool TestWithdrawal()
        {
            _cossDriver.LoginIfNecessary();

            if (!_cossDriver.CheckWallet()) { return false; }
            const string Symbol = "ZEN";

            var holding = _cossIntegration.GetHolding(Symbol, CachePolicy.ForceRefresh);
            if (holding == null || holding.Available <= 0) { return false; }

            return Withdraw(Symbol, ExchangeNameRes.Binance, holding.Available);
        }

        public bool PerformArbitrage()
        {
            _cossDriver.LoginIfNecessary();
            var arbitrageSymbols = CossAgentRes.ArbitrageSymbols;

            foreach (var symbol in arbitrageSymbols)
            {
                PerformArbitrageOnSymbol(symbol);
            }
            
            return true;
        }
        private void PerformArbitrageOnSymbol(string symbol)
        {
            var arbitrageResult = _arbitrageWorkflow.Execute(ExchangeNameRes.Coss, ExchangeNameRes.Binance, symbol, CachePolicy.ForceRefresh);
            if (arbitrageResult == null || 
                (arbitrageResult.BtcQuantity <= 0 && arbitrageResult.EthQuantity <= 0)
                || arbitrageResult.ExpectedUsdProfit < 1)
            {
                return;
            }

            if (!_cossDriver.CheckWallet()) { return; }
            var holdings = _cossIntegration.GetHoldings(CachePolicy.ForceRefresh);
            if (holdings == null || holdings.Holdings == null) { return; }

            var ethHolding = holdings.Holdings.SingleOrDefault(item => string.Equals(item.Asset, "ETH", StringComparison.InvariantCultureIgnoreCase));
            var ethAvailable = ethHolding?.Available ?? 0;

            var btcHolding = holdings.Holdings.SingleOrDefault(item => string.Equals(item.Asset, "BTC", StringComparison.InvariantCultureIgnoreCase));
            var btcAvailable = btcHolding?.Available ?? 0;

            if (arbitrageResult.EthQuantity > 0 && arbitrageResult.EthPrice.HasValue && arbitrageResult.EthPrice.Value > 0)
            {
                var totalEthCost = arbitrageResult.EthQuantity * arbitrageResult.EthPrice.Value;
                if (totalEthCost > ethAvailable)
                {
                    Info($"Not enough ETH available to execute arbitrage on {symbol}.");
                }
            }

            if (arbitrageResult.BtcQuantity > 0 && arbitrageResult.BtcPrice.HasValue && arbitrageResult.BtcPrice.Value > 0)
            {
                var totalBtcCost = arbitrageResult.BtcQuantity * arbitrageResult.BtcPrice.Value;
                if (totalBtcCost > btcAvailable)
                {
                    Info($"Not enough BTC available to execute arbitrage on {symbol}.");
                }
            }

            if (arbitrageResult.EthQuantity > 0 && arbitrageResult.EthPrice.HasValue && arbitrageResult.EthPrice.Value > 0)
            {
                var tradingPair = new TradingPair(symbol, "ETH");
                var quantityAndPrice = new QuantityAndPrice { Quantity = arbitrageResult.EthQuantity, Price = arbitrageResult.EthPrice.Value };
                _cossDriver.PlaceOrder(tradingPair, OrderType.Bid, quantityAndPrice, false);
                _cossDriver.CancelAllForTradingPair(tradingPair);
            }

            if (arbitrageResult.BtcQuantity > 0 && arbitrageResult.BtcPrice.HasValue && arbitrageResult.BtcPrice.Value > 0)
            {
                var tradingPair = new TradingPair(symbol, "BTC");
                var quantityAndPrice = new QuantityAndPrice { Quantity = arbitrageResult.BtcQuantity, Price = arbitrageResult.BtcPrice.Value };
                _cossDriver.PlaceOrder(tradingPair, OrderType.Bid, quantityAndPrice, false);
                _cossDriver.CancelAllForTradingPair(tradingPair);
            }

            _cossDriver.CheckWallet();
            var updatedHoldings = _cossIntegration.GetHoldings(CachePolicy.ForceRefresh);

            var symbolHolding = updatedHoldings.Holdings.SingleOrDefault(item => string.Equals(item.Asset, symbol, StringComparison.InvariantCultureIgnoreCase));

            Withdraw(symbol, ExchangeNameRes.Binance, symbolHolding.Available);
        }

        private DateTime? _lastArb = null;
        private readonly static TimeSpan ArbitrageInterval = TimeSpan.FromHours(1);       

        private bool Processor()
        {
            _cossDriver.LoginIfNecessary();

            try { RefreshDepositAndWithdrawalHistory(); } catch (Exception exception) { _log.Error(exception); }
            try { RefreshExchangeHistory(); } catch (Exception exception) { _log.Error(exception); }
            try { RefreshSomeOpenOrders(); } catch(Exception exception) { _log.Error(exception); }

            try { _cossDriver.CheckWallet(); } catch (Exception exception) { _log.Error(exception); }

            var agentConfig = _configRepo.GetCossAgentConfig();
            if (agentConfig.IsCossAutoTradingEnabled)
            {
                try { AutoSell(); } catch (Exception exception) { _log.Error(exception); }
                try { AutoEthBtc(); } catch (Exception exception) { _log.Error(exception); }
                try { _cossAutoBuy.Execute(); } catch (Exception exception) { _log.Error(exception); }

                //try
                //{
                //    if (!_lastArb.HasValue || (DateTime.UtcNow - _lastArb.Value) >= ArbitrageInterval)
                //    {
                //        _lastArb = DateTime.UtcNow;
                //        PerformArbitrage();
                //    }
                //}
                //catch (Exception exception)
                //{
                //    _log.Error(exception);
                //}

                try { _cossAutoOpenBid.Execute(); } catch (Exception exception) { _log.Error(exception); }
                //try { AutoOpenAsk(); } catch (Exception exception) { _log.Error(exception); }
            }

            try { RefreshSomeOpenOrders(); } catch (Exception exception) { _log.Error(exception); }
            try { RefreshExchangeHistory(); } catch (Exception exception) { _log.Error(exception); }
            try { RefreshDepositAndWithdrawalHistory(); } catch (Exception exception) { _log.Error(exception); }

            _cossDriver.Sleep(TimeSpan.FromMinutes(2.5));

            return true;
        }

        private void CancelOpenOrdersOnCurrentPage()
        {
            List<IWebElement> cancelButtons;
            var iterations = 0;
            do
            {
                cancelButtons = GetCancelOrderButtons();
                if (cancelButtons.Any())
                {
                    cancelButtons.First().Click();
                    _cossDriver.Sleep(TimeSpan.FromSeconds(5));
                }

                iterations++;
            }
            while (cancelButtons.Any() && iterations < 10);
        }

        private List<IWebElement> GetCancelOrderButtons()
        {
            var allButtons = _driver.FindElementsByTagName("button");
            return allButtons.Where(button =>
            {
                var matIcons = button.FindElements(By.TagName("mat-icon"));
                return matIcons.Any(matIcon =>
                {
                    if (string.IsNullOrWhiteSpace(matIcon.Text)) { return false; }
                    return matIcon.Text.ToUpper().Contains("clear".ToUpper());
                });
            }).ToList();
        }

        private decimal GetEthBtcConversionRate()
        {
            return _cryptoCompareIntegration.GetEthToBtcRatio(CachePolicy.ForceRefresh);
        }

        private void AutoEthBtc()
        {
            _cossDriver.CheckWallet();
            var cossHoldings = _cossIntegration.GetHoldings(CachePolicy.ForceRefresh);

            var tradingPair = new TradingPair("ETH", "BTC");
            var cossOrderBookTask = LongRunningTask.Run(() =>
                _exchangeClient.GetOrderBook(ExchangeNameRes.Coss, tradingPair.Symbol, tradingPair.BaseSymbol, CachePolicy.ForceRefresh)
            );

            var binanceOrderBookTask = LongRunningTask.Run(() =>
                _exchangeClient.GetOrderBook(ExchangeNameRes.Binance, tradingPair.Symbol, tradingPair.BaseSymbol, CachePolicy.ForceRefresh)
            );

            var cossOrderBook = cossOrderBookTask.Result;
            var binanceOrderBook = binanceOrderBookTask.Result;

            var bestCossBid = cossOrderBook.BestBid();
            var bestBinanceAsk = binanceOrderBook.BestAsk();

            var config = _configRepo.GetCossAgentConfig();
            var strategyAction = new AutoEthBtc().Execute(cossOrderBook, binanceOrderBook, config.EthThreshold, CossStrategyConstants.CossMinimumTradeEth, CossStrategyConstants.CossMinimumTradeBtc);
            if (strategyAction == null || strategyAction.ActionType == StrategyActionEnum.DoNothing || strategyAction.ActionType == StrategyActionEnum.Unknown)
            {
                _log.Info("AutoEthBtc - No orders to place.");
                return;
            }

            _cossDriver.CancelAllForTradingPair(tradingPair);

            var ethHolding = cossHoldings.Holdings.SingleOrDefault(item => string.Equals(item.Asset, "ETH", StringComparison.InvariantCultureIgnoreCase));
            var ethAvailable = ethHolding?.Available ?? 0;

            var btcHolding = cossHoldings.Holdings.SingleOrDefault(item => string.Equals(item.Asset, "BTC", StringComparison.InvariantCultureIgnoreCase));
            var btcAvailable = btcHolding?.Available ?? 0;

            if (strategyAction.ActionType == StrategyActionEnum.PlaceBid)
            {
                var totalBtcNeeded = strategyAction.Price * strategyAction.Quantity;
                if (totalBtcNeeded > btcAvailable)
                {
                    _log.Info("Not enough BTC available to buy the desired ETH.");
                    return;
                }
            }
            else if (strategyAction.ActionType == StrategyActionEnum.PlaceAsk)
            {
                var totalEthNeeded = strategyAction.Price * strategyAction.Quantity;

                if (totalEthNeeded > ethAvailable)
                {
                    _log.Info("Not enough ETH available to buy the desired BTC.");
                }
            }

            _cossDriver.NavigateToExchange(tradingPair);

            var orderType = strategyAction.ActionType == StrategyActionEnum.PlaceBid
                ? OrderType.Bid
                : OrderType.Ask;

            var logBuilder = new StringBuilder()
                .AppendLine("AutoEthBtc - About to place order")
                .AppendLine("Order:")
                .AppendLine(JsonConvert.SerializeObject(strategyAction))
                .AppendLine("Coss Order Book:")
                .AppendLine(JsonConvert.SerializeObject(cossOrderBook))
                .AppendLine("Binance Order Book:")
                .AppendLine(JsonConvert.SerializeObject(binanceOrderBook));

            _log.Info(logBuilder.ToString(), TradeEventType.AboutToPlaceOrder);
            _cossDriver.PlaceOrder(tradingPair, orderType, new QuantityAndPrice { Price = strategyAction.Price, Quantity = strategyAction.Quantity }, true);

            _cossDriver.CancelAllForTradingPair(tradingPair);
        }

        private class SymbolWithBases
        {
            public string Symbol { get; set; }
            public bool Eth { get; set; }
            public bool Btc { get; set; }

            public TradingPair ToEthTradingPair() => new TradingPair(Symbol, "ETH");
            public TradingPair ToBtcTradingPair() => new TradingPair(Symbol, "BTC");

            public static SymbolWithBases WithEth(string symbol)
            {
                return new SymbolWithBases { Symbol = symbol, Eth = true };
            }

            public static SymbolWithBases WithBtc(string symbol)
            {
                return new SymbolWithBases { Symbol = symbol, Btc = true };
            }

            public static SymbolWithBases WithBoth(string symbol)
            {
                return new SymbolWithBases { Symbol = symbol, Eth = true, Btc = true };
            }

            public override string ToString()
            {
                var builder = new StringBuilder();
                builder.Append(!string.IsNullOrWhiteSpace(Symbol) ? Symbol.Trim() : "(not specified)");
                if (Eth) { builder.Append("-ETH"); }
                if (Btc) { builder.Append("-BTC"); }

                return builder.ToString();
            }
        }

        private void AutoSell()
        {
            var tradingPairsToCheck = new List<(SymbolWithBases, string)>();

            foreach (var symbol in CossAgentRes.SimpleBinanceSymbols)
            {
                tradingPairsToCheck.Add((SymbolWithBases.WithBoth(symbol), ExchangeNameRes.Binance));
            }

            // if EOS withdrawals are disabled, we should still try to sell EOS on Coss.
            if (!CossAgentRes.SimpleBinanceSymbols.Any(item => string.Equals(item, "EOS", StringComparison.InvariantCultureIgnoreCase)))
            {
                tradingPairsToCheck.Add((SymbolWithBases.WithBoth("EOS"), ExchangeNameRes.Binance));
            }

            tradingPairsToCheck.Add((SymbolWithBases.WithBtc("DASH"), ExchangeNameRes.Binance));
            tradingPairsToCheck.Add((SymbolWithBases.WithBtc("LTC"), ExchangeNameRes.Binance));

            tradingPairsToCheck.Add((SymbolWithBases.WithBoth("PAY"), ExchangeNameRes.KuCoin));
            tradingPairsToCheck.Add((SymbolWithBases.WithBoth("CS"), ExchangeNameRes.KuCoin));
            tradingPairsToCheck.Add((SymbolWithBases.WithBoth("BCH"), ExchangeNameRes.KuCoin));
            tradingPairsToCheck.Add((SymbolWithBases.WithBoth("CAN"), ExchangeNameRes.KuCoin));
            tradingPairsToCheck.Add((SymbolWithBases.WithBoth("GAT"), ExchangeNameRes.KuCoin));
            tradingPairsToCheck.Add((SymbolWithBases.WithBoth("STX"), ExchangeNameRes.KuCoin));
            tradingPairsToCheck.Add((SymbolWithBases.WithBoth("PRL"), ExchangeNameRes.KuCoin));
            tradingPairsToCheck.Add((SymbolWithBases.WithBtc("DAT"), ExchangeNameRes.KuCoin));
            tradingPairsToCheck.Add((SymbolWithBases.WithBoth("LA"), ExchangeNameRes.KuCoin));

            tradingPairsToCheck.Add((SymbolWithBases.WithEth("FYN"), ExchangeNameRes.KuCoin));

            tradingPairsToCheck.Add((SymbolWithBases.WithBoth("LALA"), ExchangeNameRes.KuCoin));

            tradingPairsToCheck.Add((SymbolWithBases.WithEth("FXT"), ExchangeNameRes.HitBtc));
            tradingPairsToCheck.Add((SymbolWithBases.WithBtc("UFR"), ExchangeNameRes.Cryptopia));

            _cossDriver.CheckWallet();
            var cossHoldings = _cossIntegration.GetHoldings(CachePolicy.ForceRefresh);

            foreach (var tradingPairAndIntegration in tradingPairsToCheck)
            {
                var tradingPair = tradingPairAndIntegration.Item1;
                var integration = tradingPairAndIntegration.Item2;
                var cossHolding = cossHoldings.Holdings.Where(item => string.Equals(item.Asset, tradingPair.Symbol)).SingleOrDefault();

                if (cossHolding == null || cossHolding.Total == 0) { continue; }

                var cryptoComparePrice = _cryptoCompareIntegration.GetUsdValue(tradingPair.Symbol, CachePolicy.AllowCache);
                if (cryptoComparePrice.HasValue)
                {
                    var estimatedUsd = cryptoComparePrice.Value * cossHolding.Total;
                    // Don't try when the quantity is less than what we're permitted to sell.
                    // This minimum value is an estimate, but is close enough for now.
                    const decimal MinimumUsdValuetoSell = 0.10m;
                    if (estimatedUsd < MinimumUsdValuetoSell)
                    {
                        continue;
                    }
                }

                AutoSellSymbol(
                    tradingPair, 
                    cossHolding.Available,
                    integration);
            }
        }
        
        private void AutoOpenAsk()
        {
            // TODO: do the math against the symbol instead of against the pair.
            var tradingPairs = new List<TradingPair>
            {
                new TradingPair("BCH", "BTC"),
                // new TradingPair("ZEN", "BTC"),
                new TradingPair("WTC", "ETH"),
                new TradingPair("EOS", "BTC"),
                new TradingPair("VEN", "ETH"),
                // new TradingPair("ARK", "ETH"),
                new TradingPair("KNC", "ETH"),
                new TradingPair("LINK", "ETH"),
                new TradingPair("POE", "ETH"),
                new TradingPair("ENJ", "ETH"),
                new TradingPair("LTC", "BTC"),
                new TradingPair("BLZ", "ETH"),
                new TradingPair("ICX", "ETH")
            };

            foreach (var tradingPair in tradingPairs)
            {
                AutoOpenAskForTradingPair(tradingPair);
            }
        }

        private void AutoOpenAskForTradingPair(TradingPair tradingPair)
        {
            var getOwnedQuantity = new Func<string, Holding>(symbol =>
            {
                _cossDriver.CheckWallet();

                var cossHoldings = _cossIntegration.GetHoldings(CachePolicy.ForceRefresh);
                var cossHolding = cossHoldings.Holdings.Where(item => string.Equals(item.Asset, symbol)).SingleOrDefault();
                return cossHolding;
            });

            var ownedQuantity = getOwnedQuantity(tradingPair.Symbol);
            if (ownedQuantity == null || ownedQuantity.Total <= 0) { return; }

            // good enough to check to see if we have enough for this to be worth selling.
            // this gets us out of the method early without affecting the rate limit as much.
            var oldBinanceOrderBook = _exchangeClient.GetOrderBook(ExchangeNameRes.Binance, tradingPair.Symbol, tradingPair.BaseSymbol, CachePolicy.OnlyUseCacheUnlessEmpty);
            if (oldBinanceOrderBook != null && oldBinanceOrderBook.Bids != null && oldBinanceOrderBook.Bids.Any())
            { 
                var potentialSale = oldBinanceOrderBook.BestBid().Price * ownedQuantity.Total;
                var minSale = tradingPair.BaseSymbol == "ETH" ? CossStrategyConstants.CossMinimumTradeEth : CossStrategyConstants.CossMinimumTradeBtc;
                if (potentialSale < minSale) { return; }
            }

            if (ownedQuantity.InOrders > 0)
            {
                _cossDriver.CancelAllForTradingPair(tradingPair, OrderType.Ask);
                ownedQuantity = getOwnedQuantity(tradingPair.Symbol);
            }

            var binanceOrderBookTask = LongRunningTask.Run(() => _exchangeClient.GetOrderBook(ExchangeNameRes.Binance, tradingPair.Symbol, tradingPair.BaseSymbol, CachePolicy.ForceRefresh));
            var cossOrderBookTask = LongRunningTask.Run(() => _cossIntegration.GetOrderBook(tradingPair, CachePolicy.ForceRefresh));

            var binanceOrderBook = binanceOrderBookTask.Result;
            var cossOrderBook = cossOrderBookTask.Result;
            var priceToAsk = new AutoOpenAsk().ExecuteAgainstHighVolumeExchange(cossOrderBook, binanceOrderBook);
            if (!priceToAsk.HasValue) { return; }

            _cossDriver.PlaceOrder(tradingPair, OrderType.Ask, new QuantityAndPrice { Price = priceToAsk.Value, Quantity = ownedQuantity.Available });
        }

        private void AutoSellSymbol(
            SymbolWithBases symbolWithBases,
            decimal availableQuantity,
            string comparableIntegration)
        {
            if (symbolWithBases == null) { throw new ArgumentNullException(nameof(symbolWithBases)); }
            if (comparableIntegration == null) { throw new ArgumentNullException(nameof(comparableIntegration)); }
            if (!symbolWithBases.Eth && !symbolWithBases.Btc) { return; }

            if (availableQuantity <= 0) { return; }

            OrderBook comparableEthOrderBook = null;
            OrderBook comparableBtcOrderBook = null;
            var compOrderBookTask = Task.Run(() =>
            {
                if (symbolWithBases.Eth)
                {
                    comparableEthOrderBook = _exchangeClient.GetOrderBook(comparableIntegration, symbolWithBases.Symbol, CommodityRes.Eth.Symbol, CachePolicy.ForceRefresh);
                }

                if (symbolWithBases.Btc)
                {
                    comparableBtcOrderBook = _exchangeClient.GetOrderBook(comparableIntegration, symbolWithBases.Symbol, CommodityRes.Bitcoin.Symbol, CachePolicy.ForceRefresh);
                }
            });

            OrderBook cossEthOrderBook = null;
            OrderBook cossBtcOrderBook = null;

            var cossOrderBookTask = Task.Run(() =>
            {
                if (symbolWithBases.Eth)
                {
                    var ethTradingPair = symbolWithBases.ToEthTradingPair();
                    cossEthOrderBook = _exchangeClient.GetOrderBook(ExchangeNameRes.Coss, 
                        ethTradingPair.Symbol,
                        ethTradingPair.BaseSymbol,
                        CachePolicy.ForceRefresh);
                }

                if (symbolWithBases.Btc)
                {
                    var btcTradingPair = symbolWithBases.ToBtcTradingPair();
                    cossBtcOrderBook = _exchangeClient.GetOrderBook(ExchangeNameRes.Coss,
                        btcTradingPair.Symbol,
                        btcTradingPair.BaseSymbol,
                        CachePolicy.ForceRefresh);
                }
            });

            compOrderBookTask.Wait();
            cossOrderBookTask.Wait();

            var lotSize = string.Equals(symbolWithBases.Symbol, "GAT", StringComparison.InvariantCultureIgnoreCase)
                ? 10
                : (int?)null;

            var autoSellResult = new AutoSellStrategy().ExecuteWithMultipleBaseSymbols(
                availableQuantity, 
                cossEthOrderBook, 
                comparableEthOrderBook, 
                CossStrategyConstants.CossMinimumTradeEth, 
                cossBtcOrderBook, 
                comparableBtcOrderBook, 
                CossStrategyConstants.CossMinimumTradeBtc,
                lotSize);

            if (autoSellResult != null)
            {
                if (autoSellResult.BtcQuantityAndPrice != null && autoSellResult.BtcQuantityAndPrice.Quantity > 0)
                {
                    _log.Info($"About to auto-sell {symbolWithBases}.");
                    var result = _cossDriver.PlaceOrder(
                        symbolWithBases.ToBtcTradingPair(),
                        OrderType.Ask,
                        new QuantityAndPrice
                        {
                            Price = autoSellResult.BtcQuantityAndPrice.Price,
                            Quantity = autoSellResult.BtcQuantityAndPrice.Quantity
                        });

                    _cossDriver.CancelAllForTradingPair(symbolWithBases.ToBtcTradingPair(), OrderType.Ask);
                }

                if (autoSellResult.EthQuantityAndPrice != null && autoSellResult.EthQuantityAndPrice.Quantity > 0)
                {
                    _log.Info($"About to auto-sell {symbolWithBases}.");
                    var result = _cossDriver.PlaceOrder(
                        symbolWithBases.ToEthTradingPair(),
                        OrderType.Ask,
                        new QuantityAndPrice
                        {
                            Price = autoSellResult.EthQuantityAndPrice.Price,
                            Quantity = autoSellResult.EthQuantityAndPrice.Quantity
                        });

                    _cossDriver.CancelAllForTradingPair(symbolWithBases.ToEthTradingPair(), OrderType.Ask);
                }
            }
        }

        private void ConfirmEmailWithdrawal(string symbol, string quantity)
        {
            var creds = _configRepo.GetCossEmailCredentials();
        }        
        
        private void Cancel_orders(string symbol, string baseSymbol)
        {
            _cossDriver.NavigateToExchange(symbol, baseSymbol);
            List<OpenOrderEx> openOrders;

            var stopWatch = new Stopwatch();
            stopWatch.Start();
            while (!(openOrders = GetOpenOrders()).Any() && stopWatch.Elapsed < TimeSpan.FromSeconds(30))
            {
                _cossDriver.Sleep(100);
            }

            for(var index = 0; index < openOrders.Count(); index++)
            {
                var order = openOrders[index];
                order.Cancel();
                _cossDriver.Sleep(TimeSpan.FromSeconds(1));
            }
        }

        private List<OpenOrderEx> GetOpenOrders()
        {
            var openOrdersCard = _driver.FindElementByClassName("my-open-orders-card");
            if (openOrdersCard == null) { return new List<OpenOrderEx>(); }

            var rows = openOrdersCard.FindElements(By.ClassName("datatable-row-wrapper"));
            if (rows == null) { return new List<OpenOrderEx>(); }

            var openOrders = new List<OpenOrderEx>();
            foreach (var row in rows)
            {
                var cells = row.FindElements(By.ClassName("datatable-body-cell"));
                if (cells == null || cells.Count != 5) { continue; }
                var priceCell = cells[0];
                var priceSpan = priceCell.FindElement(By.TagName("span"));
                var priceSpanClass = priceSpan.GetAttribute("class");
                OrderType orderType = OrderType.Unknown;
                if (string.Equals(priceSpanClass, "sell", StringComparison.InvariantCultureIgnoreCase))
                {
                    orderType = OrderType.Ask;
                }
                else if (string.Equals(priceSpanClass, "buy", StringComparison.InvariantCultureIgnoreCase))
                {
                    orderType = OrderType.Bid;
                }

                var priceText = priceSpan.Text;
                if (!decimal.TryParse(priceText, out decimal price)) { continue; }

                var quantityCell = cells[1];
                var quantityDiv = quantityCell.FindElement(By.TagName("div"));
                var quantityText = string.Join(string.Empty, 
                    (quantityDiv.Text ?? string.Empty)
                    .Where(ch => char.IsDigit(ch) || ch == '.'));

                if (!decimal.TryParse(quantityText, out decimal quantity)) { continue; }

                var cancelCell = cells[4];
                var cancelButton = cancelCell.FindElement(By.TagName("button"));
                if (cancelButton == null) { continue; }

                var orderRow = new OpenOrderEx(price, quantity, orderType, cancelButton);
                openOrders.Add(orderRow);
            }

            return openOrders;
        }

        // Refreshes the open orders that we believe are there.
        // Also refershes one other open order based on the iteration.
        // This way, all open orders are eventually refreshed
        // and the process doesn't bog everything else down so much.
        private void RefreshSomeOpenOrders()
        {
            var openOrders = _exchangeClient.GetOpenOrders(ExchangeNameRes.Coss, CachePolicy.AllowCache);
            if ((openOrders?.Any() ?? false))
            {
                var combos = openOrders.Select(queryOpenOrder => queryOpenOrder.Symbol.ToUpper() + "_" + queryOpenOrder.BaseSymbol.ToUpper())
                    .Distinct()
                    .ToList();

                foreach (var combo in combos)
                {
                    var pieces = combo.Split('_').ToList();
                    if (pieces.Count != 2) { continue; }

                    var symbol = pieces[0];
                    var baseSymbol = pieces[1];

                    _cossDriver.GetOpenOrdersForTradingPair(symbol, baseSymbol);
                }
            }

            var tradingPairs = _cossIntegration.GetTradingPairs(CachePolicy.AllowCache);
            if (tradingPairs.Any())
            {
                var index = (_iteration + _iterationOffset) % tradingPairs.Count;
                var tradingPair = tradingPairs[index];

                _cossDriver.GetOpenOrdersForTradingPair(tradingPair.Symbol, tradingPair.BaseSymbol);
            }
        }

        private void RefreshAllOpenOrders()
        {
            var cossTradingPairs = _cossIntegration.GetTradingPairs(CachePolicy.AllowCache);
            // _cossDriver.NavigateToExchange("COSS", "ETH");
            foreach (var tradingPair in cossTradingPairs)
            {                
                try
                {
                    var openOrders = _cossDriver.GetOpenOrdersForTradingPair(tradingPair.Symbol, tradingPair.BaseSymbol);
                    Thread.Sleep(TimeSpan.FromMilliseconds(50));
                }
                catch(Exception exception)
                {
                    _log.Error(exception);
                    Thread.Sleep(TimeSpan.FromSeconds(2.5));
                }
            }
        }

        private void NavigateToWallet()
        {
            const string Url = "https://profile.coss.io/wallet";
            _cossDriver.NavigateAndVerify(Url, () =>
            {
                var spans = _driver.FindElementsByTagName("span");
                if (spans == null) { return false; }

                return spans.Any(span => span.Text != null
                    && span.Text.ToUpper().Contains("Export Csv".ToUpper()));
            });
        }
        
        //private class SessionState
        //{
        //    public DateTime AsOf { get; set; } = DateTime.UtcNow;
        //    public bool IsLoggedIn { get; set; }
        //}

        //private SessionState _sessionState = new SessionState();

        private void RefreshExchangeHistory()
        {
            const int Limit = 100;
            const int Offset = 0;
            var url = $"https://profile.coss.io/api/user/history/exchange?limit={Limit}&offset={Offset}";
            var contents = _cossDriver.PerformRequest(url);

            var parsedContents = JsonConvert.DeserializeObject<CossExchangeHistoryResponse>(contents);

            var container = new CossResponseContainer<CossExchangeHistoryResponse>
            {
                TimeStampUtc = DateTime.UtcNow,
                Response = parsedContents,
                Url = url
            };

            _cossHistoryRepo.Insert(container);
        }

        private void RefreshDepositAndWithdrawalHistory()
        {
            var url = $"https://profile.coss.io/api/user/history/deposits-and-withdrawals";// ?&limit={Limit}&offset={Offset}";
            var contents = _cossDriver.PerformRequest(url);

            var parsedContents = JsonConvert.DeserializeObject<CossDepositAndWithdrawalHistoryResponse>(contents);

            var container = new CossResponseContainer<CossDepositAndWithdrawalHistoryResponse>
            {
                TimeStampUtc = DateTime.UtcNow,
                Url = url,
                Response = parsedContents
            };

            _cossHistoryRepo.Insert(container);
        }
        
        private IWebElement FindElementById(string id)
        {
            try { return _driver.FindElementById(id); } catch { return null; }
        }

        private bool Withdraw(string symbol, string destinationIntegration, decimal quantity)
        {
            if (string.Equals(destinationIntegration, ExchangeNameRes.Coss, StringComparison.InvariantCultureIgnoreCase)) { throw new ArgumentException(nameof(destinationIntegration)); }
            var destinationAddress = _exchangeClient.GetDepositAddress(destinationIntegration, symbol, CachePolicy.ForceRefresh);
            if (destinationAddress == null || string.IsNullOrWhiteSpace(destinationAddress.Address))
            {
                throw new ApplicationException($"Unable to retrieve deposit address for {symbol} on {destinationIntegration}.");
            }

            return Withdraw(symbol, destinationAddress, quantity);
        }

        private bool Withdraw(string symbol, DepositAddress destinationAddress, decimal quantity)
        {
            if (string.IsNullOrWhiteSpace(symbol)) { throw new ArgumentNullException(nameof(symbol)); }
            if (destinationAddress == null || string.IsNullOrWhiteSpace(destinationAddress.Address)) { throw new ArgumentNullException(nameof(destinationAddress.Address)); }
            if (quantity <= 0) { throw new ArgumentOutOfRangeException(nameof(quantity)); }

            var effectiveSymbol = symbol.Trim().ToUpper();
            var effectiveAddress = destinationAddress.Address.Trim();

            if (!NavigateToWithdrawSymbol(symbol)) { return false; }

            var quantityText = quantity.ToString();


            var inputs = _driver.FindElementsByTagName("input");

            var quantityInput = _cossDriver.WaitForElement(() => _driver.FindElementsByTagName("input").SingleOrDefault(item =>
            {
                var formControlName = item.GetAttribute("formcontrolname");
                return formControlName != null && string.Equals(formControlName, "amount", StringComparison.InvariantCultureIgnoreCase);
            }));
            
            var walletAddressInput = _cossDriver.WaitForElement(() => _driver.FindElementsByTagName("input").SingleOrDefault(item =>
            {
                var formControlName = item.GetAttribute("formcontrolname");
                return formControlName != null && string.Equals(formControlName, "walletaddress", StringComparison.InvariantCultureIgnoreCase);
            }));

            if (quantityInput == null || walletAddressInput == null) { return false; }

            quantityInput.Clear();
            quantityInput.SendKeys(quantityText);

            walletAddressInput.Clear();
            walletAddressInput.SendKeys(effectiveAddress);

            ExecuteScript("$(\"button[type='submit']\").click();");

            var tfaInput = _cossDriver.WaitForElement(() => _driver.FindElementsByTagName("input").SingleOrDefault(item =>
            {
                var formControlName = item.GetAttribute("formcontrolname");
                return formControlName != null && string.Equals(formControlName, "tfaToken", StringComparison.InvariantCultureIgnoreCase);
            }));

            if (tfaInput == null) { return false; }

            var tfaValue = _tfaUtil.GetCossTfa();
            if (string.IsNullOrWhiteSpace(tfaValue)) { return false; }

            if (!_cossDriver.SetInputTextAndVerify(tfaInput, tfaValue)) { return false; }

            var buttons = _driver.FindElementsByTagName("button");
            var submitButton = _cossDriver.WaitForElement(() => _driver.FindElementsByTagName("button").SingleOrDefault(item =>
            {
                return item.Text != null && string.Equals(item.Text.Trim(), "Confirm Request", StringComparison.InvariantCultureIgnoreCase);
            }));

            if (submitButton == null) { return false; }

            submitButton.Click();

            // give it some time to send out the email before attempting to retrieve the email.
            _cossDriver.Sleep(TimeSpan.FromSeconds(10));

            if (!ConfirmEmailLinkWithdrawl(symbol, quantity)) { return false; }

            return true;
        }

        private bool ConfirmEmailLinkWithdrawl(string symbol, decimal quantity)
        {
            string emailLink = null;
            _waitForIt.Wait(() =>
            {
                emailLink = _tradeEmailUtil.GetCossWithdrawalLink(symbol, quantity);
                return emailLink != null;
            }, TimeSpan.FromMinutes(10), TimeSpan.FromSeconds(30));

            if (string.IsNullOrWhiteSpace(emailLink)) { return false; }

            _cossDriver.Navigate(emailLink);

            IWebElement confirmButton = null;
            _waitForIt.Wait(() =>
            {
                try
                {
                    confirmButton = _driver.FindElementsByTagName("button")
                        .SingleOrDefault(item =>
                        {
                            return string.Equals(item.Text, "Confirm", StringComparison.InvariantCultureIgnoreCase);
                        });
                }
                catch
                {
                }

                return confirmButton != null;
            });

            if (confirmButton == null) { return false; }

            confirmButton.Click();

            _cossDriver.Sleep(TimeSpan.FromSeconds(30));

            return true;
        }

        private void SetFormControlValue(string formControlName, string value)
        {
            ExecuteScript($"$(\"[formcontrolname = '{formControlName}']\").val(\"{value}\");");
        }

        private bool NavigateToWithdrawSymbol(string symbol)
        {
            if (string.IsNullOrWhiteSpace(symbol)) { throw new ArgumentNullException(nameof(symbol)); }
            var effectiveSymbol = symbol.Trim().ToUpper();

            NavigateToWallet();

            var script = ResUtil.Get("coss-withdraw.js", GetType().Assembly)
                .Replace("[SYMBOL]", effectiveSymbol);

            ExecuteScript(script);

            return _waitForIt.Wait(() =>
            {
                try
                {
                    var matches = _driver.FindElementsByClassName("balance-width");
                    return matches.Any(match => match != null && match.Text != null &&
                        match.Text.Trim().ToUpper().StartsWith("Available Balance".ToUpper())
                        && match.Text.Trim().ToUpper().EndsWith($" {effectiveSymbol}".ToUpper()));
                }
                catch
                {
                    return false;
                }
            });
        }

        private bool ClickByClassName(string className)
        {
            return PerformDomAction(el => el.Click(), () => _driver.FindElementByClassName(className));
        }

        private bool ClickById(string id)
        {
            return PerformDomAction(el => el.Click(), () => _driver.FindElementById(id));
        }

        private bool PerformDomAction(Action<IWebElement> perform, Func<IWebElement> find)
        {
            IWebElement webElement = null;
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            do
            {
                try
                {
                    webElement = find();
                    if (webElement == null) { _cossDriver.Sleep(TimeSpan.FromMilliseconds(250)); }
                }
                catch (Exception exception)
                {
                    _log.Error(exception);
                }
            } while (webElement == null && stopWatch.Elapsed < TimeSpan.FromSeconds(30));

            if (webElement == null) { return false; }

            try
            {
                perform(webElement);
            }
            catch (Exception exception)
            {
                _log.Error(exception);
                return false;
            }

            return true;
        }

        public void Dispose()
        {
            // if (_driver != null) { _driver.Dispose(); }
        }

        private void ExecuteScript(string script)
        {
            _driver.ExecuteScript(script);
        }

        private void Info(string message)
        {
            Console.WriteLine($"{DateTime.Now} (local) - {message}");
            _log.Info(message);
        }
    }
}
