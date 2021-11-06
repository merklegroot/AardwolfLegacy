using coss_lib;
using log_lib;
using mongo_lib;
using Newtonsoft.Json;
using OpenQA.Selenium;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using trade_browser_lib.Models;
using trade_lib;
using trade_model;
using wait_for_it_lib;
using web_util;
using binance_lib;
using config_lib;
using sel_lib;
using OpenQA.Selenium.Remote;
using trade_lib.Cache;
using kucoin_lib;
using System.Threading.Tasks;
using tfa_lib;
using cryptocompare_lib;
using System.Text;
using hitbtc_lib;
using trade_browser_lib.Strategy;
using trade_strategy_lib;
using coss_lib.Models;
using rabbit_lib;
using trade_constants;

namespace trade_browser_lib
{
    public class CossAgent : ICossAgent
    {
        private static TimeSpan MaxWaitPageTime = TimeSpan.FromMinutes(5);
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
                    return _pairsToMonitorInternalProp = _cossIntegration.GetTradingPairs();
                }
            }
        }

        private readonly RemoteWebDriver _driver;

        private readonly IWaitForIt _waitForIt;
        private readonly IBinanceIntegration _binanceIntegration;
        private readonly IKucoinIntegration _kucoinIntegration;
        private readonly IHitBtcIntegration _hitBtcIntegration;
        private readonly ICossIntegration _cossIntegration;
        private readonly ICryptoCompareIntegration _cryptoCompareIntegration;
        private readonly IWebUtil _webUtil;
        private readonly ILogRepo _log;
        private readonly IOrderManager _orderManager;
        private readonly IOpenOrderRepo _openOrderRepo;
        private readonly IConfigRepo _configRepo;
        private readonly IDepositAddressValidator _depositAddressValidator;
        private readonly ITfaUtil _tfaUtil;

        private RabbitConnection _rabbit;

        public CossAgent(
            IWaitForIt waitForIt,
            ICossIntegration cossIntegration,
            IBinanceIntegration binanceIntegration,
            IKucoinIntegration kucoinIntegration,
            IHitBtcIntegration hitBtcIntegration,
            IOrderManager orderManager,
            IOpenOrderRepo openOrderRepo,
            IConfigRepo configRepo,
            IWebUtil webUtil,
            IDepositAddressValidator depositAddressValidator,
            ICryptoCompareIntegration cryptoCompareIntegration,
            ITfaUtil tfaUtil,
            ILogRepo log)
        {
            _cossIntegration = cossIntegration;
            _binanceIntegration = binanceIntegration;
            _kucoinIntegration = kucoinIntegration;
            _hitBtcIntegration = hitBtcIntegration;
            _cryptoCompareIntegration = cryptoCompareIntegration;

            _log = log;
            _orderManager = orderManager;
            _openOrderRepo = openOrderRepo;
            _configRepo = configRepo;
            _waitForIt = waitForIt;
            _webUtil = webUtil;
            _depositAddressValidator = depositAddressValidator;
            _tfaUtil = tfaUtil;
            
            _driver = ReusableRemoteWebDriver.StartDriver();
        }

        public void Start()
        {
            _log.Info("Agent is starting.", TradeEventType.AgentStarted);

            _rabbit = RabbitConnection.Connect();
            _rabbit.Listen(TradeRabbitConstants.Queues.CossAgentQueue, OnMessageReceived);

            _keepRunning = true;
            while (_keepRunning)
            {
                try
                {
                    if (!Process()) { _keepRunning = false; return; }
                }
                catch (Exception exception)
                {
                    _log.Error(exception);
                    Sleep(TimeSpan.FromMinutes(5));
                }

                Sleep(TimeSpan.FromSeconds(20));
            }
        }

        public void Stop()
        {
            _keepRunning = false;
            _rabbit.Dispose();
        }

        private void OnMessageReceived(string message)
        {
            Info($"Received: {message}");
        }

        private bool Process()
        {
            _log.Info("Check the session.");
            if (!CheckSession())
            {
                _log.Info("No session. Redirecting to the login page.");
                Login();
                _log.Info("Logged in.");

                _log.Info("Now that we're logged in, check the session again.");
                if (!Attempt(() => CheckSession(), 3))
                {
                    throw new ApplicationException("Unable to get session after logging in. Giving up.");
                }

                _log.Info("Session is good.");
            }
            else
            {
                _log.Info("Session is good.");
            }

            CheckWallet();
            AutoSell();
            AutoEthBtc();
            AutoBuy();

            //AutoOpenBid();

            RefreshExchangeHistory();

            Sleep(TimeSpan.FromMinutes(2.5));
            
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
                    Thread.Sleep(TimeSpan.FromSeconds(5));
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
            var tradingPair = new TradingPair("ETH", "BTC");
            var cossOrderBookTask = Task.Run(() => _cossIntegration.GetOrderBook(tradingPair, CachePolicy.ForceRefresh));
            var binanceOrderBookTask = Task.Run(() => _binanceIntegration.GetOrderBook(tradingPair, CachePolicy.ForceRefresh));

            var cossOrderBook = cossOrderBookTask.Result;
            var binanceOrderBook = binanceOrderBookTask.Result;

            var bestCossBid = cossOrderBook.BestBid();
            var bestBinanceAsk = binanceOrderBook.BestAsk();

            var orders = new AutoEthBtc().Execute(cossOrderBook, binanceOrderBook);
            if (!orders.Any())
            {
                _log.Info("AutoEthBtc - No orders to place.");
                return;
            }

            CancelAllForTradingPair(tradingPair);

            NavigateToExchange(tradingPair);
            foreach (var order in orders)
            {
                var logBuilder = new StringBuilder()
                    .AppendLine("AutoEthBtc - About to place order")
                    .AppendLine("Order:")
                    .AppendLine(JsonConvert.SerializeObject(order))
                    .AppendLine("Coss Order Book:")
                    .AppendLine(JsonConvert.SerializeObject(cossOrderBook))
                    .AppendLine("Binance Order Book:")
                    .AppendLine(JsonConvert.SerializeObject(binanceOrderBook));
                
                _log.Info(logBuilder.ToString(), TradeEventType.AboutToPlaceOrder);
                PlaceOrder(tradingPair, order.OrderType, order.Price, order.Quantity, true);
            }

            CancelAllForTradingPair(tradingPair);
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
            var tradingPairsToCheck = new List<(SymbolWithBases, ITradeIntegration)>();            

            foreach (var symbol in SimpleBinanceSymbols)
            {
                tradingPairsToCheck.Add((SymbolWithBases.WithBoth(symbol), _binanceIntegration));
            }

            tradingPairsToCheck.Add((SymbolWithBases.WithBtc("DASH"), _binanceIntegration));
            tradingPairsToCheck.Add((SymbolWithBases.WithBtc("LTC"), _binanceIntegration));

            tradingPairsToCheck.Add((SymbolWithBases.WithBoth("PAY"), _kucoinIntegration));
            tradingPairsToCheck.Add((SymbolWithBases.WithBoth("CS"), _kucoinIntegration));
            tradingPairsToCheck.Add((SymbolWithBases.WithBoth("BCH"), _kucoinIntegration));
            tradingPairsToCheck.Add((SymbolWithBases.WithBoth("CAN"), _kucoinIntegration));
            tradingPairsToCheck.Add((SymbolWithBases.WithBoth("GAT"), _kucoinIntegration));
            tradingPairsToCheck.Add((SymbolWithBases.WithBoth("STX"), _kucoinIntegration));
            tradingPairsToCheck.Add((SymbolWithBases.WithBtc("DAT"), _kucoinIntegration));
            tradingPairsToCheck.Add((SymbolWithBases.WithBoth("LA"), _kucoinIntegration));

            tradingPairsToCheck.Add((SymbolWithBases.WithEth("FYN"), _kucoinIntegration));

            tradingPairsToCheck.Add((SymbolWithBases.WithBoth("LALA"), _kucoinIntegration));
            tradingPairsToCheck.Add((SymbolWithBases.WithBoth("CVC"), _kucoinIntegration));

            tradingPairsToCheck.Add((SymbolWithBases.WithEth("FXT"), _hitBtcIntegration));

            CheckWallet();
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

                AutoSellSymbol(tradingPair, integration);
            }
        }

        private List<string> SimpleBinanceSymbols
        {
            get
            {
                return new List<string>
                {
                    "ARK", "ENJ", "KNC", "SNM",
                    "OMG", "VEN", "REQ", "LSK",
                    "EOS", "POE", "KNC", "BLZ",
                    "ICX", "WTC", "SUB", "LINK"
                }.Distinct().ToList();
            }
        }

        private void AutoBuy()
        {
            var autoBuy = new AutoBuy(_log);
            
            var tradingPairsToCheck = new List<TradingPair>();

            foreach (var symbol in SimpleBinanceSymbols)
            {
                tradingPairsToCheck.Add(new TradingPair(symbol, "BTC"));
                tradingPairsToCheck.Add(new TradingPair(symbol, "ETH"));
            }

            tradingPairsToCheck.Add(new TradingPair("DASH", "BTC"));
            tradingPairsToCheck.Add(new TradingPair("LTC", "BTC"));

            var pairsDisplays = tradingPairsToCheck.Select(item => $"[{item}]");
            _log.Info($"AutoBuy -- Starting Process with a threshold of {StrategyConstants.CossAutoBuyPercentThreshold}% with the following trading pairs:{Environment.NewLine}{string.Join(", ", pairsDisplays)}");

            var totalPairsWithPurchases = 0;
            foreach (var tradingPair in tradingPairsToCheck)
            {
                var cossOrderBookTask = Task.Run(() => _cossIntegration.GetOrderBook(tradingPair, CachePolicy.ForceRefresh));
                var binanceOrderBookTask = Task.Run(() => _binanceIntegration.GetOrderBook(tradingPair, CachePolicy.ForceRefresh));

                var cossOrderBook = cossOrderBookTask.Result;
                var binanceOrderBook = binanceOrderBookTask.Result;

                var minimumDictionary = new Dictionary<string, decimal>(StringComparer.InvariantCultureIgnoreCase)
                {
                    { "ETH", StrategyConstants.CossMinimumTradeEth },
                    { "BTC", StrategyConstants.CossMinimumTradeEth }
                };

                if (!minimumDictionary.ContainsKey(tradingPair.BaseSymbol)) { throw new ArgumentException($"Unexpected base commodity \"{tradingPair.BaseSymbol}\"."); }

                var minimumTrade = minimumDictionary[tradingPair.BaseSymbol];
                
                var autoBuyResult = autoBuy.Execute(cossOrderBook.Asks, binanceOrderBook.BestBid().Price, minimumTrade, StrategyConstants.CossAutoBuyPercentThreshold);
                if (autoBuyResult == null || autoBuyResult.Quantity <= 0) { continue; }

                if (autoBuyResult.Price < 0) { throw new ApplicationException($"Auto-buy price should not be less than zero, but it was \"{autoBuyResult.Price}\""); }

                var orderToPlace = new Order { Price = autoBuyResult.Price, Quantity = autoBuyResult.Quantity };

                CancelAllForTradingPair(tradingPair);

                NavigateToExchange(tradingPair);

                var logBuilder = new StringBuilder()
                    .AppendLine($"AutoBuy - About to place {tradingPair} order")
                    .AppendLine("Order:")
                    .AppendLine(JsonConvert.SerializeObject(orderToPlace))
                    .AppendLine("Coss Order Book:")
                    .AppendLine(JsonConvert.SerializeObject(cossOrderBook))
                    .AppendLine("Binance Order Book:")
                    .AppendLine(JsonConvert.SerializeObject(binanceOrderBook));

                _log.Info(logBuilder.ToString(), TradeEventType.AboutToPlaceOrder);
                totalPairsWithPurchases++;
                PlaceOrder(tradingPair, OrderType.Bid, orderToPlace.Price, orderToPlace.Quantity, true);

                CancelAllForTradingPair(tradingPair);
            }

            if (totalPairsWithPurchases == 0)
            {
                _log.Info("AutoBuy Complete. There were no purchases to make.");
            }
            else
            {
                _log.Info($"AutoBuy Complete. Made purchases from {totalPairsWithPurchases} trading pairs.");
            }
        }

        private void AutoOpenBid()
        {
            var tradingPairs = new List<TradingPair>();
            foreach (var symbol in SimpleBinanceSymbols)
            {
                tradingPairs.Add(new TradingPair(symbol, "ETH"));
                tradingPairs.Add(new TradingPair(symbol, "BTC"));
            }

            foreach (var tradingPair in tradingPairs)
            {
                AutoOpenBidForTradingPair(tradingPair);
            }
        }

        private void AutoOpenBidForTradingPair(TradingPair tradingPair)
        {   
            var binanceOrderBookTask = Task.Run(() => _binanceIntegration.GetOrderBook(tradingPair, CachePolicy.ForceRefresh));
            var cossOrderBookTask = Task.Run(() => _cossIntegration.GetOrderBook(tradingPair, CachePolicy.ForceRefresh));

            var binanceOrderBook = binanceOrderBookTask.Result;
            var cossOrderBook = cossOrderBookTask.Result;

            var bestBinanceBid = binanceOrderBook.BestBid();
            var bestBinancePrice = bestBinanceBid.Price;

            var cossBestBid = cossOrderBook.BestBid();
            var cossBestBidPrice = cossBestBid.Price;
            var cossBestAsk = cossOrderBook.BestAsk();
            var cossBestAskPrice = cossBestAsk.Price;

            decimal targetPrice;
            targetPrice = bestBinancePrice * 0.9m;
            if (targetPrice < cossBestBidPrice) { targetPrice = bestBinancePrice * 0.91m; }
            if (targetPrice < cossBestBidPrice) { targetPrice = bestBinancePrice * 0.92m; }
            if (targetPrice < cossBestBidPrice) { return; }

            // auto order should have already purchased this one.
            // if this scenario occurs, something has gone wrong or someone placed an order quickly.
            if (targetPrice > cossBestAskPrice) { return; }

            decimal targetQuantity;
            if (string.Equals(tradingPair.BaseSymbol, "ETH", StringComparison.InvariantCultureIgnoreCase))
            {
                targetQuantity = 0.25m / targetPrice;
            }
            else if (string.Equals(tradingPair.BaseSymbol, "BTC", StringComparison.InvariantCultureIgnoreCase))
            {
                targetQuantity = 0.01m / targetPrice;
            }
            else
            {
                throw new ApplicationException($"Unexpected base symbol \"{tradingPair.BaseSymbol}\".");
            }

            CancelAllForTradingPair(tradingPair);
            PlaceOrder(tradingPair, OrderType.Bid, targetPrice, targetQuantity);
        }

        private void TransferAllArkToBinance()
        {
            const string Symbol = "ARK";

            CheckWallet();
            var cossHoldings = _cossIntegration.GetHoldings(CachePolicy.ForceRefresh);
            var cossHolding = cossHoldings.Holdings.Where(item => string.Equals(item.Asset, Symbol)).SingleOrDefault();
            var availableQuantity = cossHolding.Available;
            if(availableQuantity <= 0) { return; }
            
            WithdrawToBinance(Symbol, availableQuantity);
        }

        private void AutoSellSymbol(SymbolWithBases symbolWithBases, ITradeIntegration comparableIntegration)
        {
            if (symbolWithBases == null) { throw new ArgumentNullException(nameof(symbolWithBases)); }
            if (comparableIntegration == null) { throw new ArgumentNullException(nameof(comparableIntegration)); }
            if (!symbolWithBases.Eth && !symbolWithBases.Btc) { return; }

            // avoid any accidental wash trading
            if (symbolWithBases.Eth) { CancelAllForTradingPair(symbolWithBases.ToEthTradingPair(), OrderType.Bid); }
            if (symbolWithBases.Btc) { CancelAllForTradingPair(symbolWithBases.ToBtcTradingPair(), OrderType.Bid); }
            
            var getOwnedQuantity = new Func<string, decimal>(symbol =>
            {
                CheckWallet();

                var cossHoldings = _cossIntegration.GetHoldings(CachePolicy.ForceRefresh);
                var cossHolding = cossHoldings.Holdings.Where(item => string.Equals(item.Asset, symbolWithBases.Symbol)).SingleOrDefault();
                return cossHolding?.Available ?? 0;
            });

            var ownedQuantity = getOwnedQuantity(symbolWithBases.Symbol);
            if (ownedQuantity <= 0) { return; }

            OrderBook comparableEthOrderBook = null;
            OrderBook comparableBtcOrderBook = null;
            var compOrderBookTask = Task.Run(() =>
            {
                if (symbolWithBases.Eth)
                {
                    comparableEthOrderBook = comparableIntegration.GetOrderBook(symbolWithBases.ToEthTradingPair(), CachePolicy.ForceRefresh);
                }

                if (symbolWithBases.Btc)
                {
                    comparableBtcOrderBook = comparableIntegration.GetOrderBook(symbolWithBases.ToBtcTradingPair(), CachePolicy.ForceRefresh);
                }
            });

            OrderBook cossEthOrderBook = null;
            OrderBook cossBtcOrderBook = null;

            var cossOrderBookTask = Task.Run(() =>
            {
                if (symbolWithBases.Eth)
                {
                    cossEthOrderBook = _cossIntegration.GetOrderBook(symbolWithBases.ToEthTradingPair(), CachePolicy.ForceRefresh);
                }

                if (symbolWithBases.Btc)
                {
                    cossBtcOrderBook = _cossIntegration.GetOrderBook(symbolWithBases.ToBtcTradingPair(), CachePolicy.ForceRefresh);
                }
            });

            compOrderBookTask.Wait();
            cossOrderBookTask.Wait();

            var autoSellResult = new AutoSell().ExecuteWithMultipleBaseSymbols(
                ownedQuantity, 
                cossEthOrderBook, 
                comparableEthOrderBook, 
                StrategyConstants.CossMinimumTradeEth, 
                cossBtcOrderBook, 
                comparableBtcOrderBook, 
                StrategyConstants.CossMinimumTradeBtc);

            if (autoSellResult != null)
            {
                if (autoSellResult.BtcQuantityAndPrice != null && autoSellResult.BtcQuantityAndPrice.Quantity > 0)
                {
                    _log.Info($"About to auto-sell {symbolWithBases}.");
                    var result = PlaceOrder(
                        symbolWithBases.ToBtcTradingPair(),
                        OrderType.Ask,
                        autoSellResult.BtcQuantityAndPrice.Price,
                        autoSellResult.BtcQuantityAndPrice.Quantity);

                    CancelAllForTradingPair(symbolWithBases.ToBtcTradingPair(), OrderType.Ask);
                }

                if (autoSellResult.EthQuantityAndPrice != null && autoSellResult.EthQuantityAndPrice.Quantity > 0)
                {
                    _log.Info($"About to auto-sell {symbolWithBases}.");
                    var result = PlaceOrder(
                        symbolWithBases.ToEthTradingPair(),
                        OrderType.Ask,
                        autoSellResult.EthQuantityAndPrice.Price,
                        autoSellResult.EthQuantityAndPrice.Quantity);

                    CancelAllForTradingPair(symbolWithBases.ToEthTradingPair(), OrderType.Ask);
                }
            }
        }

        private bool WithdrawToBinance(string symbol, decimal quantity)
        {
            if (string.IsNullOrWhiteSpace(symbol)) { throw new ArgumentNullException(nameof(symbol)); }
            var depositAddress = _binanceIntegration.GetDepositAddress(symbol.Trim(), CachePolicy.ForceRefresh);

            return Withdraw(symbol, quantity, depositAddress);
        }

        private bool Withdraw(string symbol, decimal quantity, DepositAddress depositAddress)
        {
            if (string.IsNullOrWhiteSpace(symbol)) { throw new ArgumentNullException(nameof(symbol)); }
            if (quantity <= 0) { throw new ArgumentOutOfRangeException(nameof(quantity)); }

            var effectiveSymbol = symbol.Trim();

            NavigateToWallet();
            var rows = _driver.FindElementsByTagName("tr");
            var matchingRow = rows.SingleOrDefault(row =>
            {
                return row.Text.StartsWith($"{effectiveSymbol} ");
            });

            if (matchingRow == null) { _log.Warn($"Could not find the \"{effectiveSymbol}\" row in order to withdraw funds."); return false; }
            var matchingAnchor = matchingRow.FindElements(By.TagName("a")).SingleOrDefault(anchor =>
            {
                var span = anchor.FindElement(By.TagName("span"));
                if (span == null) { return false; }
                var spanClass = span.GetAttribute("class");
                if (string.IsNullOrWhiteSpace(spanClass)) { return false; }
                return spanClass.Contains("icon-withdraw");
            });

            if (matchingAnchor == null) { _log.Warn($"Could not find the \"{effectiveSymbol}\" withdraw button."); return false; }

            matchingAnchor.Click();

            var inputs = _driver.FindElementsByTagName("input");
            var amountInput = inputs.SingleOrDefault(item => item.GetAttribute("formcontrolname").Equals("amount"));
            if (amountInput == null) { _log.Warn($"Could not find the amount text box for \"{effectiveSymbol}\" withdrawal."); return false; }

            SetInputTextAndVerify(amountInput, quantity.ToString());

            var walletAddressInput = inputs.SingleOrDefault(item => item.GetAttribute("formcontrolname").Equals("walletAddress"));
            if (walletAddressInput == null) { _log.Warn($"Could not find the wallet address text box for \"{effectiveSymbol}\" withdrawal."); return false; }

            _depositAddressValidator.Validate(symbol, depositAddress);

            SetInputTextAndVerify(walletAddressInput, depositAddress.Address);

            var tfaAddressInput = inputs.SingleOrDefault(item => item.GetAttribute("formcontrolname").Equals("tfaToken"));
            if (tfaAddressInput == null) { _log.Warn($"Could not find the two factor authentication text box for \"{effectiveSymbol}\" withdrawal."); return false; }

            var tfa = _tfaUtil.GetCossTfa();
            if (string.IsNullOrWhiteSpace(tfa)) { _log.Warn($"Failed to retrieve the Coss TFA value needed for \"{effectiveSymbol}\" withdrawal."); return false; }

            SetInputTextAndVerify(tfaAddressInput, tfa);

            var buttons = _driver.FindElementsByTagName("button");
            var submitWithdrawalButton = buttons.SingleOrDefault(button =>
            {
                try
                {
                    var span = button.FindElement(By.TagName("span"));
                    return span != null && span.Text.ToUpper().Contains("Submit withdrawal request".ToUpper());
                }
                catch
                {
                    return false;
                }
            });

            if (submitWithdrawalButton == null) { _log.Warn($"Could not find the submit withdrawal request button for \"{effectiveSymbol}\" withdrawal."); return false; }

            submitWithdrawalButton.Click();

            return true;
        }

        private void ConfirmEmailWithdrawal(string symbol, string quantity)
        {
            var creds = _configRepo.GetCossEmailCredentials();
        }

        private static bool _isThisTheFirstOrderWorkflowRun = true;

        private bool Maintain()
        {
            var binanceTradingPairs = _binanceIntegration.GetTradingPairs();
            foreach (var pair in _pairsToMonitor)
            {
                CheckWallet();
                RefreshExchangeHistory();

                var myOpenOrders = RefreshOpenOrders(pair);
            }

            return true;
        }

        private bool PlaceOrder(
            TradingPair tradingPair, 
            OrderType orderType, 
            decimal price, 
            decimal quantity, 
            bool alreadyOnPage = false)
        {
            if (orderType != OrderType.Bid && orderType != OrderType.Ask)
            {
                throw new ApplicationException($"Unexpected order type \"{orderType}\".");
            }

            if (!alreadyOnPage)
            {
                NavigateToExchange(tradingPair);
            }

            var toggleResult = orderType == OrderType.Bid
                ? ClickBuyToggleButton(tradingPair.Symbol)
                : orderType == OrderType.Ask ? ClickSellToggleButton() : throw new ApplicationException($"Unexpected order type \"{orderType}\".");

            if (!toggleResult) { return false; }

            if (!EnterTradePrice(price)) { return false; }
            if (!EnterTradeQuantity(quantity)) { return false; }

            var orderInfo = new { TradingPair = tradingPair, OrderType = orderType, Price = price, Quantity = quantity };
            var orderInfoText = JsonConvert.SerializeObject(orderInfo, Formatting.Indented);

            _log.Info(orderInfoText, orderType == OrderType.Bid ? TradeEventType.PlaceBid : TradeEventType.PlaceAsk);

            var transactionClickResult = orderType == OrderType.Bid
                ? ClickPerformBuyButton()
                : orderType == OrderType.Ask ? ClickPerformSellButton() : throw new ApplicationException($"Unexpected order type \"{orderType}\".");

            Sleep(TimeSpan.FromSeconds(5));

            return transactionClickResult;
        }

        private bool EnterTradePrice(decimal price)
        {
            var element = _driver.FindElementById("input-full-place-order");
            if (element == null) { return false; }
            return SetInputNumberAndVerify(element, price);     
        }

        private bool EnterTradeQuantity(decimal quantity)
        {
            var element = GetQuantityInput();
            if (element == null) { return false; }
            return SetInputNumberAndVerify(element, quantity);
        }

        private IWebElement GetQuantityInput()
        {
            var div = _driver.FindElementByClassName("input-field-full");
            if (div == null) { return null; }
            var inputs = div.FindElements(By.TagName("input"));
            if (inputs != null && inputs.Count() == 2 && inputs[0].Enabled)
            {
                return inputs[0];
            }

            return null;
        }

        private bool SetInputTextAndVerify(IWebElement input, string text)
        {
            input.Clear();
            input.SendKeys(text);

            if (!string.Equals(text, GetInputText(input), StringComparison.Ordinal))
            {
                return false;
            }

            return true;
        }

        private bool SetInputNumberAndVerify(IWebElement input, decimal num)
        {
            input.Clear();
            input.SendKeys(num.ToString());

            var currentText = GetInputText(input);
            if (!decimal.TryParse(currentText, out decimal numberOnPage)) { return false; }

            if (num == numberOnPage) { return true; }
            if (num == 0) { return false; }

            var diff = Math.Abs(num - numberOnPage);
            var percentDifference = 100.0m * Math.Abs(diff / num);

            // if it's within 0.01%, then it's probably just a truncation issue.
            return percentDifference < 0.01m;
        }

        private string GetInputText(IWebElement input)
        {
            return input.GetAttribute("value");
        }
        
        private bool ClickPerformSellButton()
        {
            /*
                <button _ngcontent-c19="" class="sell-btn mat-raised-button mat-warn" color="warn" mat-raised-button="">
                    <span class="mat-button-wrapper">Sell</span>
                    <div class="mat-button-ripple mat-ripple" matripple=""></div>
                    <div class="mat-button-focus-overlay"></div>
                </button>
            */

            const string ExpectedClass = "sell-btn"; // mat-raised-button mat-warn";
            var elements = _driver.FindElementsByClassName(ExpectedClass);
            if (elements == null || elements.Count == 0) { return false; }
            if (elements.Count > 1) { _log.Error($"Only expected to find one element with class {ExpectedClass}."); return false; }
            var matchingButton = elements.Single();

            if (matchingButton == null) { return false; }

            matchingButton.Click();
            return true;

        }

        private bool ClickPerformBuyButton()
        {
            // <span class="mat-button-wrapper">Buy</span>
            var elements = _driver.FindElementsByClassName("mat-button-wrapper");
            IWebElement matchingButton = null;
            foreach(var element in elements)
            {
                try
                {
                    var text = element.Text;
                    if (string.IsNullOrWhiteSpace(text)) { continue; }
                    if (!string.Equals(text.Trim(), "BUY", StringComparison.Ordinal)) { continue; }
                }
                catch (Exception exception)
                {
                    Console.WriteLine(exception);
                }

                matchingButton = element;
                break;
            }

            if (matchingButton == null) { return false; }

            matchingButton.Click();
            return true;
        }

        private bool ClickSellToggleButton()
        {
            var sellButton = _driver.FindElementsByClassName("mat-button-toggle-label-content")
                .SingleOrDefault(element =>
                {
                    try
                    {
                        return
                            element != null
                            && !string.IsNullOrWhiteSpace(element.Text)
                            && element.Text.Trim().ToUpper().Contains("SELL");
                    }
                    catch
                    {
                        return false;
                    }
                });

            if (sellButton == null) { return false; }

            try { sellButton.Click(); } catch { return false; }

            return true;
        }

        private bool ClickBuyToggleButton(string symbol)
        {
            var button = GetBuyToggleButton(symbol);

            if (button == null) { return false; }
            button.Click();

            return true;
        }

        private IWebElement GetBuyToggleButton(string symbol)
        {
            var allDivs = _driver.FindElementsByTagName("div");

            var expectedText = $"BUY {symbol.ToUpper()}";
            foreach (var div in allDivs)
            {
                var divText = (div.Text ?? string.Empty).Trim();
                if (string.Equals(divText, expectedText, StringComparison.InvariantCultureIgnoreCase))
                {
                    return div;
                }
            }

            return null;
        }

        private void CancelAllForTradingPair(TradingPair tradingPair, OrderType? orderType = null)
        {
            var myOpenOrders = RefreshOpenOrders(tradingPair);
            for (var i = myOpenOrders.Count() - 1; i >= 0; i--)
            {
                var myOpenOrder = myOpenOrders[i];
                if (orderType.HasValue && orderType.Value != myOpenOrder.OrderType) { continue; }
                myOpenOrder.Cancel();
            }
        }
        
        private void Cancel_orders(string symbol, string baseSymbol)
        {
            NavigateToExchange(symbol, baseSymbol);
            List<OpenOrderEx> openOrders;

            var stopWatch = new Stopwatch();
            stopWatch.Start();
            while (!(openOrders = GetOpenOrders()).Any() && stopWatch.Elapsed < TimeSpan.FromSeconds(30))
            {
                Sleep(100);
            }

            for(var index = 0; index < openOrders.Count(); index++)
            {
                var order = openOrders[index];
                order.Cancel();
                Sleep(TimeSpan.FromSeconds(1));
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

        private List<OpenOrderEx> RefreshOpenOrders(TradingPair tradingPair)
        {
            NavigateToExchange(tradingPair);
            
            var stopWatch = new Stopwatch();
            stopWatch.Start();

            var openOrders = new List<OpenOrderEx>();
            DateTime requestTime;
            DateTime responseTime;
            bool isFirstRun = true;
            do
            {
                if (isFirstRun) { isFirstRun = false; }
                else { Sleep(100); }

                requestTime = DateTime.UtcNow;
                openOrders = GetOpenOrders();
                responseTime = DateTime.UtcNow;
            }
            while (!openOrders.Any() && stopWatch.Elapsed < TimeSpan.FromSeconds(10));

            // var exchange = trade_res.Exchange.Coss;
            var info = new OpenOrderInfo
            {
                RequestTimeUtc = requestTime,
                ResponseTimeUtc = responseTime,
                OpenOrders = openOrders != null ? openOrders.Select(item => item.ToBase()).ToList() : null,
                Symbol = tradingPair.Symbol.Trim().ToUpper(),
                BaseSymbol = tradingPair.BaseSymbol.Trim().ToUpper(),
                ExchangeId = _cossIntegration.Id,
                ExchangeName = _cossIntegration.Name
            };

            _openOrderRepo.Insert(info);

            return openOrders;
        }

        private void NavigateToWallet()
        {
            const string Url = "https://profile.coss.io/user-wallet";
            Navigate_and_verify(Url, () =>
            {
                var tableHeaders = _driver.FindElementsByTagName("th");
                return tableHeaders != null && tableHeaders.Any(item => item.Text.Trim().Equals("Cryptocurrency Balance", StringComparison.InvariantCultureIgnoreCase));
            });
        }

        private bool CheckWallet()
        {
            var checkWalletCorrelationId = Guid.NewGuid();
            _log.Verbose(TradeEventType.BeginCheckWallet, checkWalletCorrelationId);

            try
            {
                var url = "https://profile.coss.io/api/user/wallets";
                var requestTime = DateTime.UtcNow;
                _driver.Navigate().GoToUrl(url);
                var responseTime = DateTime.UtcNow;

                var condition = new Func<bool>(() =>
                {
                    try
                    {
                        var contents = _driver.FindElementByTagName("pre").Text;
                        var container = new ResponseContainer
                        {
                            RequestTimeUtc = requestTime,
                            ResponseTimeUtc = responseTime,
                            Contents = contents
                        };

                        if (string.IsNullOrWhiteSpace(contents)) { return false; }
                        var session = JsonConvert.DeserializeObject<CossResponse>(contents);
                        if (session == null) { return false; }

                        _cossIntegration.InsertResponseContainer(container);

                        return session.Successful;
                    }
                    catch (Exception exception)
                    {
                        _log.Error(exception);
                        return false;
                    }
                });

                return _waitForIt.Wait(condition, TimeSpan.FromSeconds(10));
            }
            finally
            {
                _log.Verbose(TradeEventType.EndCheckWallet, checkWalletCorrelationId);
            }
        }

        private bool CheckSession()
        {
            var url = "https://exchange.coss.io/api/session";
            var requestTime = DateTime.UtcNow;
            _driver.Navigate().GoToUrl(url);
            var responseTime = DateTime.UtcNow;

            var condition = new Func<bool>(() =>
            {
                try
                {
                    var contents = _driver.FindElementByTagName("pre").Text;
                    var container = new ResponseContainer
                    {
                        RequestTimeUtc = requestTime,
                        ResponseTimeUtc = responseTime,
                        Contents = contents
                    };

                    var sessionContext = new MongoCollectionContext(_configRepo.GetConnectionString(), "coss", SessionCollectionName);
                    sessionContext.GetCollection<ResponseContainer>().InsertOne(container);

                    if (string.IsNullOrWhiteSpace(contents)) { return false; }
                    var session = JsonConvert.DeserializeObject<CossResponse>(contents);
                    if (session == null) { return false; }

                    return session.Successful;
                }
                catch (Exception exception)
                {
                    _log.Error(exception);
                    return false;
                }
            });

            return _waitForIt.Wait(condition, TimeSpan.FromSeconds(10));
        }

        private bool RefreshExchangeHistory()
        {
            try
            {
                var responses = new List<CossExchangeHistoryResponseAndUrl>();

                const int Inc = 1000;
                const int Limit = Inc + 5;
                var offset = 0;

                bool shouldKeepGettingMore = true;
                while (shouldKeepGettingMore)
                {
                    var url = $"https://profile.coss.io/api/user/history/exchange?limit={Limit}&offset={offset}";
                    var requestTime = DateTime.UtcNow;
                    _driver.Navigate().GoToUrl(url);
                    var responseTime = DateTime.UtcNow;

                    var contents = _driver.FindElementByTagName("pre").Text;
                    var parsedContents = JsonConvert.DeserializeObject<CossExchangeHistoryResponse>(contents);
                    responses.Add(new CossExchangeHistoryResponseAndUrl
                    {
                        Url = url,
                        Response = parsedContents
                    });

                    shouldKeepGettingMore = false;
                    if (parsedContents?.payload?.actions?.items != null
                        && parsedContents.successful
                        && parsedContents.payload.actions.items.Count > 0
                        && parsedContents.payload.actions.totalCount - (offset + Limit + Inc) > 0
                        )
                    {
                        shouldKeepGettingMore = true;
                        offset += Inc;
                    }
                }

                var container = new CossExchangeHistoryResponseAndUrlContainer
                {
                    TimeStampUtc = DateTime.UtcNow,
                    Responses = responses
                };

                _cossIntegration.InsertExchangeHistory(container);


                /*
                var container = new ResponseContainer
                {
                    RequestTimeUtc = requestTime,
                    ResponseTimeUtc = responseTime,
                    Contents = contents
                };

                if (string.IsNullOrWhiteSpace(contents)) { return false; }
                var cossResponse = JsonConvert.DeserializeObject<CossResponse>(contents);
                if (cossResponse == null) { return false; }

                _cossIntegration.InsertExchangeHistoryResponseContainer(container);
                */
            }
            catch (Exception exception)
            {
                _log.Error(exception);
                return false;
            }

            return true;
        }
        
        private IWebElement FindElementById(string id)
        {
            try { return _driver.FindElementById(id); } catch { return null; }
        }

        private bool AttemptLogin()
        {
            var cossCredentials = _configRepo.GetCossCredentials();
            if (cossCredentials == null || string.IsNullOrWhiteSpace(cossCredentials.UserName) || string.IsNullOrWhiteSpace(cossCredentials.Password))
            {
                return false;
            }

            var userNameTextBox = _driver.FindElementById("username");
            if (userNameTextBox == null) { return false; }

            if (!SetInputTextAndVerify(userNameTextBox, cossCredentials.UserName))
            {
                return false;
            }

            var passwordTextbox = _driver.FindElementById("password");
            if (passwordTextbox == null) { return false; }
            if (!SetInputTextAndVerify(passwordTextbox, cossCredentials.Password)) { return false; }

            var allDivs = _driver.FindElements(By.TagName("div"));

            var captchaCheckBox = allDivs[15];
            if (captchaCheckBox == null)
            {
                Console.WriteLine("Cannot find ReCaptcha checkbox.");
                return false;
            }
            
            var captchaPanel = FindElementById("rc-imageselect");
            if (captchaPanel != null) { Console.WriteLine("Found the captcha panel *before* clicking the button."); }

            captchaCheckBox.Click();
            Thread.Sleep(TimeSpan.FromSeconds(2));

            var captchaPanelAfterClicking = FindElementById("rc-imageselect");
            if (captchaPanelAfterClicking != null) { Console.WriteLine("Found the captcha panel *after* clicking the button."); }

            Thread.Sleep(TimeSpan.FromSeconds(15));

            var loginButton = _driver.FindElementByName("Login");
            if (loginButton == null) { return false; }
            loginButton.Click();
            
            if (!_waitForIt.Wait(() =>
            _driver.FindElementsByTagName("span")
                .Any(item => item.Text.Contains("Please enter the code shown on the Google Authenticator mobile app:"))
                ))
            {
                Console.WriteLine("Never made it to the google authenticator page. Probably got hit by the captcha.");
                return false;
            }

            var authenticatorTextBox = _driver.FindElementByName("valOne");
            if(authenticatorTextBox == null) { return false; }

            var tfa = GetCossTfa();
            if (string.IsNullOrWhiteSpace(tfa)) { return false; }

            if (!SetInputTextAndVerify(authenticatorTextBox, tfa.Trim())) { return false; }

            var loginButtonFromAuthenticatorPage = _driver.FindElementByName("Login");
            if (loginButtonFromAuthenticatorPage == null) { return false; }
            loginButtonFromAuthenticatorPage.Click();

            return true;
        }

        private string GetCossTfa()
        {
            return _webUtil.Get("http://localhost/tfa/api/coss-tfa").Replace("\"", "");
        }

        private void Login()
        {
            _driver.Navigate().GoToUrl(CossPage.Login);
            if (!_waitForIt.Wait(() => string.Equals((_driver.Title ?? string.Empty).Trim(), "coss.io", StringComparison.InvariantCultureIgnoreCase)))
            {
                throw new ApplicationException("Never got to the login page.");
            }

            //if (!AttemptLogin())
            //{
            //    _log.Info("Please login.");
            //}

            _log.Info("Please login.");

            var condition = new Func<bool>(() =>
            {
                try
                {
                    return _driver.Url.ToString().ToUpper().Contains("dashboard".ToUpper());
                }
                catch (Exception exception)
                {
                    Console.WriteLine(exception);
                    return false;
                }
            });

            while (!condition())
            {
                Thread.Sleep(250);
            }
        }

        private void WithdrawAll(string commodity, string destinationAddress)
        {
            if (string.IsNullOrWhiteSpace(commodity)) { throw new ArgumentNullException(nameof(commodity)); }

            var effectiveCommodity = commodity.Trim().ToUpper();
            
            // _driver.FindElementByClassName("");
            var allDivs = _driver.FindElementsByTagName("div");
            var cssClass = allDivs[0].GetAttribute("class");
            cssClass.Contains($" {commodity} ");
        }

        private void NavigateToDepositsAndWithdrawalsHistory()
        {
            Navigate_and_verify(CossPage.DepositsAndWithdrawalsHistory, () => FindElementWithText("h2", "Deposits & Withdrawals History"));
        }

        private void NavigateToExchangeHistory()
        {
            Navigate_and_verify(CossPage.ExchangeHistory, () => FindElementWithText("h2", "EXCHANGE HISTORY"));
        }

        private void NavigateToExchange(TradingPair tradingPair)
        {
            NavigateToExchange(tradingPair.Symbol, tradingPair.BaseSymbol);
        }

        private void NavigateToExchange(string symbol, string baseSymbol)
        {
            if (string.IsNullOrWhiteSpace(symbol)) { throw new ArgumentNullException(nameof(symbol)); }
            if (string.IsNullOrWhiteSpace(baseSymbol)) { throw new ArgumentNullException(nameof(baseSymbol)); }

            var url = $"https://exchange.coss.io/exchange/{symbol.Trim().ToLower()}-{baseSymbol.Trim().ToLower()}";

            var verifier = new Func<bool>(() =>
            {
                var expectedText = $"{symbol.Trim().ToUpper()}/{baseSymbol.Trim().ToUpper()}";
                return FindElementWithText("span", expectedText);
            });

            Navigate_and_verify(url, verifier);
        }

        private bool FindElementWithText(string elementType, string expectedText)
        {
            if (string.IsNullOrWhiteSpace(elementType)) { throw new ArgumentNullException(nameof(elementType)); }
            if (string.IsNullOrWhiteSpace(expectedText)) { throw new ArgumentNullException(nameof(expectedText)); }

            try
            {
                var elementsCollection = _driver.FindElementsByTagName(elementType);
                if (elementsCollection == null) { return false; }
                return elementsCollection.Any(item =>
                {                    
                    try
                    {
                        return 
                            item != null
                            && item.Text != null
                            && string.Equals(item.Text.Trim(), expectedText.Trim(), StringComparison.InvariantCultureIgnoreCase);
                    }
                    catch
                    {
                        return false;
                    }
                });
            }
            catch (Exception exception)
            {
                _log.Error(exception);
                return false;
            }
        }

        private void Navigate_and_verify(string url, Func<bool> verifier)
        {
            _driver.Navigate().GoToUrl(url);
            if (!_waitForIt.Wait(() => { try { return verifier(); } catch { return false; } }, MaxWaitPageTime))
            {
                throw new ApplicationException("Did not find expected contents within expected time frame.");
            }
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
                    if (webElement == null) { Thread.Sleep(250); }
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

        private void Sleep(int milliseconds)
        {
            Sleep(TimeSpan.FromMilliseconds(milliseconds));
        }

        private void Sleep(TimeSpan timeSpan)
        {
            var maxSleepTimeSpan = TimeSpan.FromMilliseconds(100);

            var stopWatch = new Stopwatch();
            stopWatch.Start();

            TimeSpan timeRemaining;
            while ((timeRemaining = timeSpan - stopWatch.Elapsed) > TimeSpan.Zero)
            {
                Thread.Sleep(timeRemaining >= maxSleepTimeSpan ? maxSleepTimeSpan : timeRemaining);
            }
        }

        private bool Attempt(Func<bool> method, int maxAttempts)
        {
            for (var i = 0; i < maxAttempts; i++)
            {
                if(i != 0) { Sleep(TimeSpan.FromSeconds(i)); }

                try
                {
                    if (method()) { return true; }
                }
                catch (Exception exception)
                {
                    _log.Error(exception);
                }
            }

            return false;
        }

        private void Info(string message)
        {
            Console.WriteLine($"{DateTime.Now} (local) - {message}");
            _log.Info(message);
        }
    }
}
