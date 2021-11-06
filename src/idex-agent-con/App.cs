using binance_lib;
using bit_z_lib;
using browser_lib;
using cache_lib.Models;
using config_client_lib;
using console_lib;
using cryptocompare_lib;
using cryptopia_lib;
using exchange_client_lib;
using hitbtc_lib;
using idex_agent_lib;
using idex_agent_lib.Models;
using idex_agent_lib.res;
using idex_data_lib;
using idex_integration_lib;
using idex_model;
using kucoin_lib;
using linq_lib;
using livecoin_lib;
using log_lib;
using Newtonsoft.Json;
using OpenQA.Selenium;
using OpenQA.Selenium.Remote;
using parse_lib;
using qryptos_lib;
using res_util_lib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using task_lib;
using tidex_integration_library;
using trade_lib;
using trade_model;
using trade_res;
using trade_strategy_lib;
using wait_for_it_lib;
using yobit_lib;

namespace idex_agent_con
{
    public class App : IDisposable
    {
        // private const decimal TargetQuantity = 0.33m;
        private const decimal TargetQuantity = 0.5m;
        private const decimal IdexMinimumEthSale = 0.15m;

        private static TimeSpan TimeToSleepBetweenSymbols = TimeSpan.FromSeconds(5);

        private readonly IWaitForIt _waitforIt;
        private readonly IConfigClient _configClient;
        private readonly IIdexIntegration _idex;
        private readonly ICryptoCompareIntegration _cryptoCompare;
        private readonly IIdexHistoryRepo _idexHistoryRepo;
        private readonly IIdexOpenOrdersRepo _openOrdersRepo;
        private readonly IIdexOrderBookRepo _orderBookRepo;

        private readonly IExchangeClient _exchangeClient;

        private readonly ILogRepo _log;
        private readonly AutoOpenBid _autoOpenBid = new AutoOpenBid();
        private readonly AutoOpenAsk _autoOpenAsk = new AutoOpenAsk();

        private IBrowserUtil _browser;
        private RemoteWebDriver Driver { get { return _browser.Driver; } }

        public App(
            ICryptoCompareIntegration cryptoCompare,
            IIdexHistoryRepo idexHistoryRepo,
            IIdexIntegration idex,
            IWaitForIt waitForIt,
            IConfigClient configClient,
            IBrowserUtil browserUtil,
            IIdexOpenOrdersRepo openOrdersRepo,
            IIdexOrderBookRepo orderBookRepo,
            IExchangeClient exchangeClient,
            ILogRepo log)
        {
            _cryptoCompare = cryptoCompare;
            _idexHistoryRepo = idexHistoryRepo;
            _idex = idex;            
            _waitforIt = waitForIt;
            _configClient = configClient;
            _browser = browserUtil;
            _openOrdersRepo = openOrdersRepo;
            _orderBookRepo = orderBookRepo;

            _exchangeClient = exchangeClient;
            _log = log;

            // _idex.UseRelay = true;
        }

        public void LoginAndRefreshHistory()
        {
            if (!Login()) { return; }
            RefreshTradeHistory();
        }

        public void RunAutoOrder(string symbol)
        {
            try
            {
                if (!Login()) { return; }
                RefreshTradeHistory();
                AutoBidAndAsk(symbol);
            }
            catch (Exception exception)
            {
                _log.Error(exception);
            }
        }

        public void RunAutoOrder()
        {
            try
            {                
                if (!Login()) { return; }

                RefreshTradeHistory();
                AutoBidAndAskAllComps();
                RefreshTradeHistory();
            }
            catch (Exception exception)
            {
                _log.Error(exception);
            }
        }

        private bool RefreshTradeHistory()
        {
            Info("Refreshing History");
            try
            {
                NavigateToTradeHistory();
                var mainElement = Driver.FindElementByTagName("main");
                if (mainElement == null) { return false; }

                var tradeHistoryDiv = mainElement.FindElement(By.ClassName("trade-history"));
                if (tradeHistoryDiv == null) { return false; }

                var tbody = tradeHistoryDiv.FindElement(By.TagName("tbody"));
                if (tbody == null) { return false; }

                var rows = tbody.FindElements(By.TagName("tr"));
                if (rows == null || rows.Count <= 1)
                {
                    Thread.Sleep(TimeSpan.FromSeconds(5));
                    rows = tbody.FindElements(By.TagName("tr"));
                }

                if (rows == null) { return false; }

                var historyItems = new List<IdexHistoryItem>();
                for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
                {
                    var row = rows[rowIndex];
                    var cells = row.FindElements(By.TagName("td"));
                    if (cells.Count != 10) { continue; }

                    var historyItem = new IdexHistoryItem();
                    historyItem.AddRange(cells.Select(item => item.Text));

                    historyItems.Add(historyItem);
                }

                _idexHistoryRepo.Insert(new IdexHistoryContainer
                {
                    TimeStampUtc = DateTime.UtcNow,
                    ClientTimeZone = TimeZone.CurrentTimeZone.StandardName,
                    HistoryItems = historyItems
                });

                Info("Done refreshing history.");
                return true;
            }
            catch(Exception exception)
            {
                Info("Failed to refresh history.");
                ConsoleWrapper.WriteLine(exception);
                _log.Error(exception);
                return false;
            }
        }

        private void AutoBidAndAskAllComps()
        {
            var symbols = IdexAgentRes.BinanceIntersection;

            foreach (var nonBinanceSymbol in IdexAgentRes.NonBinanceIntersections.Keys)
            {
                symbols.Insert(0, nonBinanceSymbol);
            }

            // Since NCASH works out so well, make sure it's updated more often.
            symbols.Insert(0, "NCASH");
            symbols = symbols.Shuffle();

            for (var i = 0; i < symbols.Count; i++)
            {
                var symbol = symbols[i];
                Info($"Starting auto bid/ask for {symbol}. ({i + 1} / {symbols.Count})");
                
                try
                {
                    AutoBidAndAsk(symbol);
                    Info($"Finished auto bid/ask for {symbol}.");
                }
                catch (Exception exception)
                {                    
                    _log.Error(exception);
                    ConsoleWrapper.WriteLine(exception);
                }
            }

            ConsoleWrapper.WriteLine("Done with the auto bid/ask process.");
        }

        public void RunUpdateData()
        {
            try
            {
                if (!Login()) { return; }

                // RefreshOpenOrders();
                // RefreshBalances();
                RefreshTradeHistory();
            }
            catch (Exception exception)
            {
                _log.Error(exception);
            }
        }

        private List<IWebElement> GetExchangeCancelAnchors()
        {
            var method = new Func<List<IWebElement>>(() =>
            Driver.FindElementsByTagName("a").Where(queryAnchor =>
            {
                return queryAnchor.Text != null && string.Equals(queryAnchor.Text.Trim(), "Cancel", StringComparison.InvariantCultureIgnoreCase);
            }).ToList());

            try { return method(); }
            catch { Thread.Sleep(TimeSpan.FromSeconds(5)); return method(); }
        }

        private bool CancelBidsAndAsksForSymbols(List<string> bidSymbols, List<string> askSymbols)
        {           
            NavigateToOpenOrders();

            var tableBody = Driver.FindElementByTagName("tbody");
            var rows = tableBody.FindElements(By.TagName("tr"));

            var openOrders = new List<IdexOpenOrder>();
            var cellGroups = rows.Select(row => row.FindElements(By.TagName("td"))).ToList();

            foreach (var cells in cellGroups)
            {
                if (cells.Count != 7) { continue; }

                var tradeType = cells[1].Text.Trim();

                var firstCellAnchor = cells[0].FindElement(By.TagName("a"));
                if (firstCellAnchor == null) { continue; }
                var market = firstCellAnchor.Text.Trim();
                var pieces = market.Split('/').ToList();
                if (pieces.Count != 2) { continue; }
                var symbol = pieces[0].Trim();

                var cancelAnchor = cells[6].FindElements(By.TagName("a"))
                    .SingleOrDefault(queryAnchor => queryAnchor.Text != null && string.Equals(queryAnchor.Text.Trim(), "Cancel", StringComparison.InvariantCultureIgnoreCase));

                if (cancelAnchor == null) { continue; }
                
                if (string.Equals(tradeType, "Buy", StringComparison.InvariantCultureIgnoreCase))
                {
                    if (!bidSymbols.Any(querySymbol => string.Equals(querySymbol, symbol, StringComparison.InvariantCultureIgnoreCase))) { continue; }
                }
                else if (string.Equals(tradeType, "Sell", StringComparison.InvariantCultureIgnoreCase))
                {
                    if (!askSymbols.Any(querySymbol => string.Equals(querySymbol, symbol, StringComparison.InvariantCultureIgnoreCase))) { continue; }
                }

                ConsoleWrapper.WriteLine($"About to cancel purchase order for \"{symbol}\".");
                cancelAnchor.Click();
                Thread.Sleep(TimeSpan.FromSeconds(5));
            }

            return true;
        }

        private bool AutoOrderWorkflow()
        {
            foreach (var symbol in IdexAgentRes.BinanceIntersection)
            {
                AutoBidAndAsk(symbol);

                Thread.Sleep(TimeToSleepBetweenSymbols);
            }

            return true;
        }

        private void ApplyLodash()
        {
            var script = ResUtil.Get("lodash.min.js", typeof(IdexAgentLibDummy).Assembly);
            ExecuteScript(script);
        }

        private void MarkControls()
        {
            var script = ResUtil.Get("mark-controls.js", typeof(IdexAgentLibDummy).Assembly);
            ExecuteScript(script);
        }

        // Assumes that lodash has already been applied to the form.
        private IdexWebOrderBook GetOrderBook()
        {
            // ApplyLodash();           

            var getOrderBook = ResUtil.Get("get-order-book.js", typeof(IdexAgentLibDummy).Assembly);
            ExecuteScript(getOrderBook);

            var orderBookDebug = Driver.FindElementById("orderBookDebug");
            var orderBookJson = orderBookDebug.Text;

            return JsonConvert.DeserializeObject<IdexWebOrderBook>(orderBookJson);
        }

        // Assumes that lodash has already been applied to the form.
        private List<IdexWebOpenOrder> GetOpenOrders()
        {
            // ApplyLodash();

            var script = ResUtil.Get("get-open-orders.js", typeof(IdexAgentLibDummy).Assembly);
            ExecuteScript(script);

            var debugElement = Driver.FindElementById("openOrdersDebug");
            var json = debugElement.Text;

            return JsonConvert.DeserializeObject<List<IdexWebOpenOrder>>(json);
        }

        private FeedbackResult CancelOpenOrder(string rowId)
        {
            ApplyLodash();

            var feedbackId = Guid.NewGuid().ToString();
            var script = ResUtil.Get("cancel-open-order", typeof(IdexAgentLibDummy).Assembly)
                .Replace("[FEEDBACK_ID]", feedbackId)
                .Replace("[ROW_ID]", rowId);

            ExecuteScript(script);

            var feedbackElement = Driver.FindElementById(feedbackId);
            var json = feedbackElement?.Text;

            return !string.IsNullOrWhiteSpace(json)
                ? JsonConvert.DeserializeObject<FeedbackResult>(json)
                : null;
        }

        private static bool ExecuteUntestedCode = false;

        private void CreateBid(decimal targetBidPrice, decimal targetQuantity)
        {
            if (!ExecuteUntestedCode)
            {
                // throw new ApplicationException("Code is untested and must be stepped through manually.");
            }

            var desiredPriceText = targetBidPrice.ToString("N8");
            // give a quantity so that the bid is worth the target ETH.
            var desiredQuantity = (TargetQuantity * 1.000001m) / targetBidPrice;
            var desiredQuantityText = desiredQuantity.ToString("N8");

            var bidPriceInput = Driver.FindElementById("selenium_bidPriceInput");
            var bidQuantityInput = Driver.FindElementById("selenium_bidQuantityInput");
            var bidSymbolDiv = Driver.FindElementById("selenium_bidSymbolDiv");
            var buyButton = Driver.FindElementById("selenium_buyButton");

            bidPriceInput.Clear();
            bidPriceInput.SendKeys(desiredPriceText);

            bidQuantityInput.Clear();
            bidQuantityInput.SendKeys(desiredQuantityText);

            Thread.Sleep(TimeSpan.FromSeconds(1.5));

            buyButton.Click();

            Thread.Sleep(TimeSpan.FromSeconds(1.5));
        }

        private void CreateAskForAllAvailable(decimal targetAskPrice)
        {
            if (!ExecuteUntestedCode)
            {
                // throw new ApplicationException("Code is untested and must be stepped through manually.");
            }

            var targetAskPriceText = targetAskPrice.ToString("N8");

            var askPriceInput = Driver.FindElementById("selenium_askPriceInput");
            var askQuantityInput = Driver.FindElementById("selenium_askQuantityInput");
            var askSymbolDiv = Driver.FindElementById("selenium_askSymbolDiv");
            var sellButton = Driver.FindElementById("selenium_sellButton");
            var askAmountAnchor = Driver.FindElementById("selenium_askAmountAnchor");

            askPriceInput.Clear();
            askPriceInput.SendKeys(targetAskPriceText);

            askQuantityInput.Clear();           

            askAmountAnchor.Click();

            Thread.Sleep(TimeSpan.FromSeconds(1.5));

            sellButton.Click();

            Thread.Sleep(TimeSpan.FromSeconds(2.5));

            ClickOrderCreatedOkButton();
        }

        private void ClickOrderCreatedOkButton()
        {
            var script = ResUtil.Get("click-dialog-ok-button.js", typeof(IdexAgentLibDummy).Assembly);
            ExecuteScript(script);
        }


        private void ExecuteScript(string script)
        {
            ((IJavaScriptExecutor)Driver).ExecuteScript(script);
        }

        private bool AutoBidAndAsk(string symbol, bool isAfterCancel = false)
        {
            var holdings = _idex.GetHolding(symbol, CachePolicy.ForceRefresh);
            var quantityAvailableToSell = holdings?.Total ?? 0;

            NavigateToExchange(symbol);

            var exchangeWrapper = _browser.WaitForElement(() =>
            {
                return Driver.FindElementByClassName("layout--exchange-wrapper");
            });

            ApplyLodash();
            MarkControls();

            var bidSymbolDiv = Driver.FindElementById("selenium_bidSymbolDiv");
            var bidSymbol = bidSymbolDiv.Text;
            if (!string.Equals(symbol, (bidSymbol ?? string.Empty).Trim(), StringComparison.InvariantCultureIgnoreCase))
            {
                throw new ApplicationException($"Expected {symbol} got {bidSymbol}");
            }

            var openOrders = GetOpenOrders();
            if (openOrders != null && openOrders.Any())
            {
                foreach (var openOrder in openOrders)
                {
                    var result = CancelOpenOrder(openOrder.RowId);
                    Console.WriteLine(result != null ? JsonConvert.SerializeObject(result) : "null result");
                }
            }

            var webOrderBook = GetOrderBook();
            var idexOrderBook = new OrderBook
            {
                Asks = (webOrderBook?.Asks ?? new List<IdexWebOrderBook.IdexWebOrderBookItem>())
                    .Where(item => item != null)
                    .Select(item => new Order
                    {
                        Price = item.Price,
                        Quantity = item.SymbolQuantity
                    }).ToList(),
                Bids = (webOrderBook?.Bids ?? new List<IdexWebOrderBook.IdexWebOrderBookItem>())
                    .Where(item => item != null)
                    .Select(item => new Order
                    {
                        Price = item.Price,
                        Quantity = item.SymbolQuantity
                    }).ToList(),
            };

            var idexAsks = idexOrderBook.Asks;
            var idexBids = idexOrderBook.Bids;

            var isThisABinanceSymbol = IdexAgentRes.BinanceIntersection.Any(binanceSymbol => string.Equals(binanceSymbol, symbol, StringComparison.InvariantCultureIgnoreCase));

            OrderBook binanceOrderBook = null;
            Order bestBinanceBid = null;
            Order bestBinanceAsk = null;
            if (isThisABinanceSymbol)
            {
                binanceOrderBook = _exchangeClient.GetOrderBook(ExchangeNameRes.Binance, symbol, "ETH", CachePolicy.ForceRefresh);
                if (binanceOrderBook == null
                    || binanceOrderBook.Bids == null
                    || !binanceOrderBook.Bids.Any()
                    || binanceOrderBook.Asks == null
                    || !binanceOrderBook.Asks.Any()) { return false; }

                bestBinanceBid = binanceOrderBook.BestBid();
                bestBinanceAsk = binanceOrderBook.BestAsk();
            }

            if (!idexAsks.Any())
            {
                Info($"Didn't find any Idex asks for {symbol}.");
                return false;
            }
            else if (!idexBids.Any())
            {
                Info($"Didn't find any Idex bids for {symbol}.");
                return false;
            }

            if (quantityAvailableToSell <= 0)
            {
                Info($"We don't have any {symbol} to sell.");
            }
            else
            {
                var bestIdexAsk = idexAsks.OrderBy(item => item.Price).FirstOrDefault();
                var bestIdexAskPrice = bestIdexAsk.Price;

                decimal? targetAskPrice = null;

                if (isThisABinanceSymbol)
                {
                    targetAskPrice = _autoOpenAsk.ExecuteAgainstHighVolumeExchange(idexOrderBook, binanceOrderBook);
                }
                else
                {
                    var tradingPair = new TradingPair(symbol, "ETH");
                    var compTasks = new List<Task<OrderBook>>();
                    var compIntegrationNames = IdexAgentRes.NonBinanceIntersections[symbol];

                    foreach (var compIntegrationName in compIntegrationNames)
                    {
                        var compTask = LongRunningTask.Run(() => _exchangeClient.GetOrderBook(compIntegrationName, tradingPair.Symbol, tradingPair.BaseSymbol, CachePolicy.ForceRefresh));
                        compTasks.Add(compTask);
                    }

                    var cryptoCompareTask = Task.Run(() => _cryptoCompare.GetPrice(symbol, "ETH", CachePolicy.ForceRefresh));

                    var comps = compTasks.Select(compTask => compTask.Result).ToList();
                    Info($"Comparing Idex prices against {string.Join(", ", compIntegrationNames)}.");

                    var cryptoComparePrice = cryptoCompareTask.Result;

                    if (cryptoComparePrice.HasValue)
                    {
                        targetAskPrice = new AutoOpenAsk().ExecuteAgainstRegularExchanges(idexOrderBook, comps, cryptoComparePrice.Value);
                    }
                }

                if (targetAskPrice.HasValue)
                {
                    var potentialTotalSale = targetAskPrice * quantityAvailableToSell;
                    if (potentialTotalSale > IdexMinimumEthSale && targetAskPrice < bestIdexAskPrice)
                    {
                        // CreateAskForAllAvailable(targetAskPrice.Value);
                        CreateAskForAllAvailable(targetAskPrice.Value);
                        Thread.Sleep(TimeSpan.FromSeconds(5));
                    }
                }
                else
                {
                    Info($"Didn't find enough room to place an Ask for our {symbol}.");
                }
            }

            var bestIdexBid = idexBids.OrderByDescending(item => item.Price).First();
            var bestIdexBidPrice = bestIdexBid.Price;

            decimal? targetBidPrice = null;

            if (isThisABinanceSymbol)
            {
                targetBidPrice = _autoOpenBid.ExecuteAgainstHighVolumeExchange(idexOrderBook, binanceOrderBook);
            }
            else
            {
                var tradingPair = new TradingPair(symbol, "ETH");
                var compTasks = new List<Task<OrderBook>>();
                var compIntegrationNames = IdexAgentRes.NonBinanceIntersections[symbol];

                foreach (var compIntegrationName in compIntegrationNames)
                {
                    var compTask = LongRunningTask.Run(() => 
                        _exchangeClient.GetOrderBook(compIntegrationName, tradingPair.Symbol, tradingPair.BaseSymbol, CachePolicy.ForceRefresh));
                    compTasks.Add(compTask);
                }

                var cryptoCompareTask = Task.Run(() => _cryptoCompare.GetPrice(symbol, "ETH", CachePolicy.ForceRefresh));

                var comps = compTasks.Select(compTask => compTask.Result).ToList();

                var cryptoComparePrice = cryptoCompareTask.Result;

                if (cryptoComparePrice.HasValue)
                {
                    targetBidPrice = _autoOpenBid.ExecuteAgainstRegularExchanges(idexOrderBook, comps, cryptoComparePrice.Value);
                }
            }

            if (targetBidPrice.HasValue)
            {
                var desiredPriceText = targetBidPrice.Value.ToString("N8");
                // give a quantity so that the bid is worth the target ETH.
                var desiredQuantity = (TargetQuantity * 1.000001m) / targetBidPrice.Value;
                var desiredQuantityText = desiredQuantity.ToString("N8");

                Info($"Placing bid for {desiredQuantity} {symbol} at {targetBidPrice}.");

                CreateBid(targetBidPrice.Value, desiredQuantity);
                Thread.Sleep(TimeSpan.FromSeconds(5));
            }
            else
            {
                Info($"Didn't find enough room to place a Bid for {symbol}.");
            }

            return true;
        }        

        private bool AutoBidAndAskOld(string symbol, bool isAfterCancel = false)
        {
            var holdings = _idex.GetHolding(symbol, CachePolicy.ForceRefresh);
            var quantityAvailableToSell = holdings?.Total ?? 0;

            NavigateToExchange(symbol);

            var exchangeWrapper = _browser.WaitForElement(() =>
            {
                return Driver.FindElementByClassName("layout--exchange-wrapper");
            });

            var h2s = exchangeWrapper.FindElements(By.TagName("h2"));
            var expectedText = $"{symbol.ToUpper()} / ETH";
            if (!h2s.Any(item => string.Equals((item.Text ?? string.Empty).Trim(), expectedText, StringComparison.InvariantCultureIgnoreCase)))
            {
                return false;
            }

            if (exchangeWrapper == null) { return false; }

            var exchangeRegionB = exchangeWrapper.FindElement(By.ClassName("layout--exchange-region-b"));
            if (exchangeRegionB == null) { return false; }

            var components = exchangeRegionB.FindElements(By.ClassName("component"));
            var openOrdersComponent = components[4];
            var openOrdersRows = openOrdersComponent.FindElements(By.TagName("tr"));

            bool didWeCancelAnyOrders = false;
            for (var i = openOrdersRows.Count - 1; i >= 0; i--)
            {
                var row = openOrdersRows[i];
                var cells = row.FindElements(By.TagName("td"));
                if (cells.Count != 6) { continue; }

                var orderTypeText = cells[0].Text;
                var orderTypeDictionary = new Dictionary<string, OrderType>(StringComparer.InvariantCultureIgnoreCase)
                {
                    { "Sell", OrderType.Ask },
                    { "Buy", OrderType.Bid }
                };

                var orderType = orderTypeDictionary.ContainsKey((orderTypeText ?? string.Empty).Trim())
                    ? orderTypeDictionary[orderTypeText.Trim()]
                    : OrderType.Unknown;

                var priceText = cells[1].Text;
                var quantityText = cells[2].Text;
                var totalText = cells[3].Text;
                var dateText = cells[4].Text;
                var cancelCell = cells[5];
                var cancelAnchor = cancelCell.FindElement(By.TagName("a"));

                if (cancelAnchor != null)
                {
                    Info($"Cancelling order to {orderTypeText} {quantityText} {symbol} at {priceText}.");
                    cancelAnchor.Click();
                }

                didWeCancelAnyOrders = true;
            }

            // reloaded after cancelling the open orders for this symbol.
            // there should not have been any open orders in the grid.
            if (didWeCancelAnyOrders)
            {
                if (isAfterCancel) { return false; }
                return AutoBidAndAsk(symbol, true);
            }

            var componentPanels = components
                .Where(item => string.Equals(item.GetAttribute("class"), "component panel"))
                .ToList();

            if (componentPanels == null || componentPanels.Count != 2) { return false; }
            var bidsPanel = componentPanels[0];
            var bidsPanelText = bidsPanel.Text;
            if (!bidsPanelText.StartsWith($"BUY {symbol.ToUpper()}")) { return false; }

            var asksPanel = componentPanels[1];
            var asksPanelText = asksPanel.Text;
            if (!asksPanelText.StartsWith($"SELL {symbol.ToUpper()}")) { return false; }

            var asksGrid = asksPanel.FindElement(By.ClassName("grid"));
            if (asksGrid == null) { return false; }

            var bidsGrid = bidsPanel.FindElement(By.ClassName("grid"));
            if (bidsGrid == null) { return false; }

            var asksGridDivs = asksGrid.FindElements(By.TagName("div"));
            const int ExpectedAsksGridDivCount = 18;
            if (asksGridDivs == null || asksGridDivs.Count != ExpectedAsksGridDivCount) { return false; }
            var askPriceInput = asksGridDivs[4].FindElement(By.TagName("input"));
            if (askPriceInput == null) { return false; }

            var askAmountAnchor = asksGridDivs[6].FindElement(By.TagName("a"));
            if (askAmountAnchor == null) { return false; }

            var askQuantityInput = asksGridDivs[7].FindElement(By.TagName("input"));
            if (askPriceInput == null) { return false; }

            var bidsGridDivs = bidsGrid.FindElements(By.TagName("div"));
            const int ExpectedBidsGridDivCount = 18;
            if (bidsGridDivs == null || bidsGridDivs.Count != ExpectedBidsGridDivCount) { return false; }
            var bidPriceInput = bidsGridDivs[4].FindElement(By.TagName("input"));
            if (bidPriceInput == null) { return false; }
            var bidQuantityInput = bidsGridDivs[7].FindElement(By.TagName("input"));
            if (bidQuantityInput == null) { return false; }

            var sellButton = asksGridDivs[16].FindElement(By.TagName("a"));
            if (sellButton == null) { return false; }
            var sellButtonText = sellButton.Text;
            if (!string.Equals((sellButtonText ?? string.Empty).Trim(), "SELL", StringComparison.InvariantCultureIgnoreCase)) { return false; }

            var buyButton = bidsGridDivs[16].FindElement(By.TagName("a"));
            if (buyButton == null) { return false; }
            var buyButtonText = buyButton.Text;
            if (!string.Equals((buyButtonText ?? string.Empty).Trim(), "BUY", StringComparison.InvariantCultureIgnoreCase)) { return false; }

            var asksComponent = components[2];
            var bidsComponent = components[3];

            var asksComponentHeader = asksComponent.FindElement(By.TagName("header"));
            if (asksComponentHeader == null) { return false; }

            var bidsComponentHeader = bidsComponent.FindElement(By.TagName("header"));
            if (bidsComponentHeader == null) { return false; }

            var asksComponentHeaderUnorderedList = asksComponentHeader.FindElement(By.TagName("ul"));
            if (asksComponentHeaderUnorderedList == null) { return false; }

            var bidsComponentHeaderUnorderedList = bidsComponentHeader.FindElement(By.TagName("ul"));
            if (bidsComponentHeaderUnorderedList == null) { return false; }

            var asksComponentHeaderListItems = asksComponentHeaderUnorderedList.FindElements(By.TagName("li"));
            if (asksComponentHeaderListItems == null || asksComponentHeaderListItems.Count != 2) { return false; }
            if (!string.Equals((asksComponentHeaderListItems[0].Text ?? string.Empty).Trim(), "Asks", StringComparison.InvariantCultureIgnoreCase)) { return false; }

            var bidsComponentHeaderListItems = bidsComponentHeaderUnorderedList.FindElements(By.TagName("li"));
            if (bidsComponentHeaderListItems == null || bidsComponentHeaderListItems.Count != 2) { return false; }
            if (!string.Equals((bidsComponentHeaderListItems[0].Text ?? string.Empty).Trim(), "Bids", StringComparison.InvariantCultureIgnoreCase)) { return false; }

            var asksTable = asksComponent.FindElement(By.TagName("table"));
            if (asksTable == null) { return false; }

            var bidsTable = bidsComponent.FindElement(By.TagName("table"));
            if (bidsTable == null) { return false; }

            var asksTableBody = asksTable.FindElement(By.TagName("tbody"));
            if (asksTableBody == null) { return false; }

            var bidsTableBody = bidsTable.FindElement(By.TagName("tbody"));
            if (bidsTableBody == null) { return false; }

            var asksTableBodyRows = asksTableBody.FindElements(By.TagName("tr"));
            var bidsTableBodyRows = bidsTableBody.FindElements(By.TagName("tr"));

            var idexAsks = new List<Order>();
            for (var i = 0; i < asksTableBodyRows.Count && i < 10; i++)
            {
                var row = asksTableBodyRows[i];
                var cells = row.FindElements(By.TagName("td"));
                if (cells.Count != 4) { continue; }
                var priceText = cells[0].Text;
                var price = ParseUtil.DecimalTryParse(priceText);

                var quantityText = cells[1].Text;
                var quantity = ParseUtil.DecimalTryParse(quantityText);

                var ethText = cells[2].Text;
                var sumEthText = cells[3].Text;

                if (price.HasValue && quantity.HasValue)
                {
                    idexAsks.Add(new Order { Price = price.Value, Quantity = quantity.Value });
                }
            }

            var idexBids = new List<Order>();
            for (var i = 0; i < bidsTableBodyRows.Count && i < 10; i++)
            {
                var row = bidsTableBodyRows[i];
                var cells = row.FindElements(By.TagName("td"));
                if (cells.Count != 4) { continue; }
                var priceText = cells[0].Text;
                var price = ParseUtil.DecimalTryParse(priceText);

                var quantityText = cells[1].Text;
                var quantity = ParseUtil.DecimalTryParse(quantityText);

                var ethText = cells[2].Text;
                var sumEthText = cells[3].Text;

                if (price.HasValue && quantity.HasValue)
                {
                    idexBids.Add(new Order { Price = price.Value, Quantity = quantity.Value });
                }
            }

            var idexOrderBook = new OrderBook
            {
                Asks = idexAsks,
                Bids = idexBids
            };

            var container = new IdexOrderBookContainer
            {
                TimeStampUtc = DateTime.UtcNow,
                Symbol = symbol,
                BaseSymbol = "ETH",
                OrderBook = idexOrderBook
            };

            _orderBookRepo.Insert(container);

            var isThisABinanceSymbol = IdexAgentRes.BinanceIntersection.Any(binanceSymbol => string.Equals(binanceSymbol, symbol, StringComparison.InvariantCultureIgnoreCase));

            OrderBook binanceOrderBook = null;
            Order bestBinanceBid = null;
            Order bestBinanceAsk = null;
            if (isThisABinanceSymbol)
            {
                binanceOrderBook = _exchangeClient.GetOrderBook(ExchangeNameRes.Binance, symbol, "ETH", CachePolicy.ForceRefresh);
                if (binanceOrderBook == null
                    || binanceOrderBook.Bids == null
                    || !binanceOrderBook.Bids.Any()
                    || binanceOrderBook.Asks == null
                    || !binanceOrderBook.Asks.Any()) { return false; }

                bestBinanceBid = binanceOrderBook.BestBid();
                bestBinanceAsk = binanceOrderBook.BestAsk();
            }

            if (!idexAsks.Any())
            {
                Info($"Didn't find any Idex asks for {symbol}.");
                return false;
            }
            else if (!idexBids.Any())
            {
                Info($"Didn't find any Idex bids for {symbol}.");
                return false;
            }

            if (quantityAvailableToSell <= 0)
            {
                Info($"We don't have any {symbol} to sell.");
            }
            else
            {
                var bestIdexAsk = idexAsks.OrderBy(item => item.Price).FirstOrDefault();
                var bestIdexAskPrice = bestIdexAsk.Price;

                decimal? targetAskPrice = null;

                if (isThisABinanceSymbol)
                {
                    targetAskPrice = _autoOpenAsk.ExecuteAgainstHighVolumeExchange(idexOrderBook, binanceOrderBook);
                }
                else
                {
                    var tradingPair = new TradingPair(symbol, "ETH");
                    var compTasks = new List<Task<OrderBook>>();
                    var compIntegrationNames = IdexAgentRes.NonBinanceIntersections[symbol];

                    foreach (var compIntegrationName in compIntegrationNames)
                    {
                        var compTask = LongRunningTask.Run(() => _exchangeClient.GetOrderBook(compIntegrationName, tradingPair.Symbol, tradingPair.BaseSymbol, CachePolicy.ForceRefresh));
                        compTasks.Add(compTask);
                    }

                    var cryptoCompareTask = Task.Run(() => _cryptoCompare.GetPrice(symbol, "ETH", CachePolicy.ForceRefresh));

                    var comps = compTasks.Select(compTask => compTask.Result).ToList();
                    Info($"Comparing Idex prices against {string.Join(", ", compIntegrationNames)}.");

                    var cryptoComparePrice = cryptoCompareTask.Result;

                    if (cryptoComparePrice.HasValue)
                    {
                        targetAskPrice = new AutoOpenAsk().ExecuteAgainstRegularExchanges(idexOrderBook, comps, cryptoComparePrice.Value);
                    }
                }

                if (targetAskPrice.HasValue)
                {
                    var potentialTotalSale = targetAskPrice * quantityAvailableToSell;
                    if (potentialTotalSale > IdexMinimumEthSale && targetAskPrice < bestIdexAskPrice)
                    {
                        Info($"Creating ask for {quantityAvailableToSell} {symbol} at {targetAskPrice}");

                        var targetAskPriceText = targetAskPrice.Value.ToString("N8");
                        askPriceInput.Clear();
                        askPriceInput.SendKeys(targetAskPriceText);

                        Thread.Sleep(TimeSpan.FromSeconds(1));

                        askAmountAnchor.Click();

                        Thread.Sleep(TimeSpan.FromSeconds(2.5));

                        sellButton.Click();
                        Thread.Sleep(TimeSpan.FromSeconds(5));

                        var okButton = _browser.WaitForElement(() =>
                        {
                            var items = Driver.FindElementsByTagName("a");
                            if (items == null) { return null; }
                            foreach (var item in items)
                            {
                                var itemText = item.Text;
                                if (!string.Equals((itemText ?? string.Empty).Trim(), "OK", StringComparison.InvariantCultureIgnoreCase)) { continue; }

                                var itemClass = item.GetAttribute("class");
                                if (!string.Equals((itemClass ?? string.Empty).Trim(), "ui--button", StringComparison.InvariantCultureIgnoreCase)) { continue; }

                                return item;
                            }

                            return null;
                        });

                        if (okButton == null) { return false; }
                        okButton.Click();
                        Thread.Sleep(TimeSpan.FromSeconds(5));
                    }
                }
                else
                {
                    Info($"Didn't find enough room to place an Ask for our {symbol}.");
                }
            }

            var bestIdexBid = idexBids.OrderByDescending(item => item.Price).First();
            var bestIdexBidPrice = bestIdexBid.Price;

            decimal? targetBidPrice = null;

            if (isThisABinanceSymbol)
            {
                targetBidPrice = _autoOpenBid.ExecuteAgainstHighVolumeExchange(idexOrderBook, binanceOrderBook);
            }
            else
            {
                var tradingPair = new TradingPair(symbol, "ETH");
                var compTasks = new List<Task<OrderBook>>();
                var compIntegrationNames = IdexAgentRes.NonBinanceIntersections[symbol];

                foreach (var compIntegrationName in compIntegrationNames)
                {
                    var compTask = LongRunningTask.Run(() => _exchangeClient.GetOrderBook(compIntegrationName, tradingPair.Symbol, tradingPair.BaseSymbol, CachePolicy.ForceRefresh));
                    compTasks.Add(compTask);
                }

                var cryptoCompareTask = Task.Run(() => _cryptoCompare.GetPrice(symbol, "ETH", CachePolicy.ForceRefresh));

                var comps = compTasks.Select(compTask => compTask.Result).ToList();

                var cryptoComparePrice = cryptoCompareTask.Result;

                if (cryptoComparePrice.HasValue)
                {
                    targetBidPrice = _autoOpenBid.ExecuteAgainstRegularExchanges(idexOrderBook, comps, cryptoComparePrice.Value);
                }
            }

            if (targetBidPrice.HasValue)
            {
                var desiredPriceText = targetBidPrice.Value.ToString("N8");
                // give a quantity so that the bid is worth the target ETH.
                var desiredQuantity = (TargetQuantity * 1.000001m) / targetBidPrice.Value;
                var desiredQuantityText = desiredQuantity.ToString("N8");

                Info($"Placing bid for {desiredQuantity} {symbol} at {targetBidPrice}.");

                bidPriceInput.Clear();
                bidPriceInput.SendKeys(desiredPriceText);

                bidQuantityInput.Clear();
                bidQuantityInput.SendKeys(desiredQuantityText);

                buyButton.Click();

                Thread.Sleep(TimeSpan.FromSeconds(5));
            }
            else
            {
                Info($"Didn't find enough room to place a Bid for {symbol}.");
            }

            return true;
        }

        private IWebElement GetPurchaseSection()
        {
            return _browser.WaitForElement(() =>
            {
                var transactionSections = _browser.Driver.FindElementsByClassName("transaction");
                return transactionSections.SingleOrDefault(querySection =>
                {
                    var anchors = querySection.FindElements(By.TagName("a"));
                    return anchors.Any(queryAnchor => queryAnchor.Text != null && string.Equals(queryAnchor.Text.Trim(), "Buy", StringComparison.InvariantCultureIgnoreCase));
                });
            });
        }

        private IWebElement GetSaleSection()
        {
            return _browser.WaitForElement(() =>
            {
                var transactionSections = _browser.Driver.FindElementsByClassName("transaction");
                return transactionSections.SingleOrDefault(querySection =>
                {
                    var anchors = querySection.FindElements(By.TagName("a"));
                    return anchors.Any(queryAnchor => queryAnchor.Text != null && string.Equals(queryAnchor.Text.Trim(), "Sell", StringComparison.InvariantCultureIgnoreCase));
                });
            });
        }

        private bool Login()
        {
            try
            {
                Info("Logging in...");
                var loginResult = PerformSteps(new List<Func<bool>>
                {
                    NavigateToBalances,
                    ClickUnlockWalletButton,
                    EnterMewFileName,
                    EnterPasswordAndSubmit,
                    () => { Thread.Sleep(TimeSpan.FromSeconds(5)); return true; }
                });

                if (loginResult) { Info("Logged in successfully."); }
                else { Info("Failed to login."); }

                return loginResult;
            }
            catch (Exception exception)
            {
                ConsoleWrapper.WriteLine("Failed to login.");
                ConsoleWrapper.WriteLine(exception);
                _log.Error(exception);
                throw;
            }
            finally
            {
                Info("Done with login process.");
            }
        }
        
        private bool PerformSteps(List<Func<bool>> steps)
        {
            foreach(var step in steps)
            {
                if (!step()) { return false; }
            }

            return true;
        }

        private bool EnterMewFileName()
        {
            var walletFileName = _configClient.GetMewWalletFileName();
            if (string.IsNullOrWhiteSpace(walletFileName))
            {
                _log.Error("MEW Wallet File Name is not configured.");
                return false;
            }

            var fileInput = Driver.FindElementsByTagName("input")
                .FirstOrDefault(item =>
                {
                    var inputType = item.GetAttribute("type");
                    if (inputType == null) { return false; }

                    return string.Equals(inputType, "file");
                });

            if (fileInput == null) { return false; }
            fileInput.SendKeys(walletFileName);

            return true;
        }

        private bool EnterPasswordAndSubmit()
        {
            var passwordTextBox = GetPasswordTextBox();
            if (passwordTextBox == null) { return false; }

            var mewPassword = _configClient.GetMewPassword();
            if(string.IsNullOrWhiteSpace(mewPassword))
            {
                _log.Error("MEW Password not configured");
                return false;
            }

            passwordTextBox.SendKeys(mewPassword);
            passwordTextBox.SendKeys(Keys.Enter);

            return true;
        }

        private bool ClickUnlockSubmitButton()
        {
            var button = GetUnlockSubmitButton();
            if (button == null) { return false; }

            button.Click();
            return true;
        }

        private IWebElement GetPasswordTextBox()
        {
            return _browser.WaitForElement(() =>
            {
                var inputs = Driver.FindElementsByTagName("input");
                if (inputs == null) { return null; }
                return inputs.FirstOrDefault(item =>
                {
                    var itemType = item.GetAttribute("type");
                    return string.Equals(itemType, "password", StringComparison.InvariantCultureIgnoreCase);
                });
            });
        }

        private bool ClickUnlockWalletButton()
        {
            var unlockWalletButton = GetUnlockWalletButton();
            if (unlockWalletButton == null) { return false; }
            unlockWalletButton.SendKeys(Keys.Enter);
            return true;
        }

        private IWebElement GetUnlockSubmitButton()
        {
            return _browser.WaitForElement(() =>
            {
                var elements = Driver.FindElementsByTagName("button");
                if (elements == null) { return null; }
                return elements.FirstOrDefault(item =>
                {
                    var itemType = item.GetAttribute("type");
                    return string.Equals(itemType, "submit", StringComparison.InvariantCultureIgnoreCase);
                });
            });
        }

        private IWebElement GetSelectWalletFileButton()
        {
            return _browser.WaitForElement(() =>
            {
                var divs = Driver.FindElementsByTagName("div");
                if (divs == null) { return null; }
                var uiButtons = divs.Where(item =>
                {
                    var cssClass = item.GetAttribute("class");
                    return string.Equals(cssClass, "ui--button");
                }).ToList();

                return uiButtons.FirstOrDefault(item => 
                item != null && item.Text != null && item.Text.ToUpper().Contains("Select Wallet File".ToUpper()));
            });
        }

        private IWebElement GetUnlockWalletButton()
        {
            return _browser.WaitForElement(() =>
            {
                var anchors = Driver.FindElementsByTagName("a");
                if (anchors == null) { return null; }
                var match = anchors.FirstOrDefault(item => string.Equals(item.Text, "Unlock Wallet", StringComparison.InvariantCultureIgnoreCase));
                return match;
            });
        }

        public void Dispose()
        {
            if (_browser != null) { _browser.Dispose(); }
        }

        private bool NavigateToOpenOrders()
        {
            const string Url = "https://idex.market/open";
            _browser.GoToUrl(Url);
            return true;
        }

        private void NavigateToTradeHistory()
        {
            const string Url = "https://idex.market/trades";
            _browser.GoToUrl(Url);
        }

        private bool NavigateToBalances()
        {
            _browser.GoToUrl("https://idex.market/balances");
            return true;
        }

        private void NavigateToExchange(string symbol)
        {
            var url = $"https://idex.market/eth/{symbol.ToLower()}";
            _browser.GoToUrl(url);            
        }

        private bool ClickHideZeroBalancesCheckBox()
        {
            var checkBox = GetHideZeroBalancesCheckBox();
            if (checkBox == null) { return false; }

            checkBox.Click();

            return true;
        }
        
        private IWebElement GetHideZeroBalancesCheckBox()
        {
            return Driver.FindElementById("stars-balance");
        }

        private void Info(string message)
        {
            ConsoleWrapper.WriteLine($"{DateTime.Now} (local) - {message}");
            _log.Info(message);
        }
    }
}
