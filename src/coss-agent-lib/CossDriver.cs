using cache_lib;
using cache_lib.Models;
using config_client_lib;
using config_connection_string_lib;
using coss_agent_lib.Models;
using coss_agent_lib.res;
using coss_data_lib;
using coss_data_model;
using coss_lib;
using date_time_lib;
using log_lib;
using mongo_lib;
using Newtonsoft.Json;
using OpenQA.Selenium;
using res_util_lib;
using sel_lib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using tfa_lib;
using trade_browser_lib;
using trade_browser_lib.Models;
using trade_lib;
using trade_model;
using wait_for_it_lib;

namespace coss_agent_lib
{
    public class CossDriver : ICossDriver
    {
        // private const string CossSessionUrl = "https://exchange.coss.io/api/session";
        private const string CossSessionUrl = "https://profile.coss.io/api/session";

        private static TimeSpan MaxWaitPageTime = TimeSpan.FromMinutes(5);
        private const string SessionCollectionName = "coss-session";

        private IRemoteWebDriver _driver;
        private readonly ILogRepo _log;
        private readonly IWaitForIt _waitForIt;
        private readonly IConfigClient _configClient;
        private readonly ITfaUtil _tfaUtil;
        private readonly ICossIntegration _cossIntegration;
        private readonly IGetConnectionString _getConnectionString;
        private readonly ICossOpenOrderRepo _openOrderRepo;
        private readonly ICossXhrOpenOrderRepo _cossXhrOpenOrderRepo;
        private readonly ICacheUtil _cacheUtil = new CacheUtil();

        public CossDriver(
            IGetConnectionString getConnectionString,
            IConfigClient configClient,
            ICossIntegration cossIntegration,
            ICossOpenOrderRepo openOrderRepo,
            ICossXhrOpenOrderRepo cossXhrOpenOrderRepo,
            ITfaUtil tfaUtil,
            IWaitForIt waitForIt,
            ILogRepo log)
        {
            _getConnectionString = getConnectionString;
            _configClient = configClient;
            _cossIntegration = cossIntegration;
            _openOrderRepo = openOrderRepo;
            _cossXhrOpenOrderRepo = cossXhrOpenOrderRepo;
            _tfaUtil = tfaUtil;
            _waitForIt = waitForIt;
            _log = log;
        }

        public void Init(IRemoteWebDriver driver)
        {
            _driver = driver;
        }

        public CossSessionState SessionState { get; private set; } = new CossSessionState();

        public void LoginIfNecessary()
        {
            _log.Info("Check the session.");
            if (CheckSession()) { return; }

            ExecuteScript(CossAgentRes.SetButtonScript);
            Sleep(TimeSpan.FromSeconds(2.5));

            WaitForElementToGoAway(() => _driver.FindElementById("debugButton"));

            while (!CheckSession())
            {
                ExecuteScript(CossAgentRes.SetButtonScript);
                Sleep(TimeSpan.FromSeconds(2.5));
                WaitForElementToGoAway(() => _driver.FindElementById("debug"));
            }            
        }

        public bool CheckSession()
        {
            var requestTime = DateTime.UtcNow;
            _driver.Navigate().GoToUrl(CossSessionUrl);
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

                    var sessionContext = new MongoCollectionContext(DbContext, SessionCollectionName);
                    sessionContext.GetCollection<ResponseContainer>().InsertOne(container);

                    if (string.IsNullOrWhiteSpace(contents)) { return false; }
                    var session = JsonConvert.DeserializeObject<CossResponse>(contents);
                    if (session == null) { return false; }

                    SessionState = new CossSessionState { AsOf = DateTime.UtcNow, IsLoggedIn = session.Successful };
                    return session.Successful;
                }
                catch (Exception exception)
                {
                    SessionState = new CossSessionState { AsOf = DateTime.UtcNow, IsLoggedIn = false };
                    _log.Error(exception);
                    return false;
                }
            });

            return _waitForIt.Wait(condition, TimeSpan.FromSeconds(10));
        }

        public string PerformRequest(string url, string method = "GET")
        {
            if (url.StartsWith("https://profile.coss.io"))
            {
                if (!_driver.Url.StartsWith("https://profile.coss.io"))
                {
                    NavigateToDashboard();
                }
            }
            else if (url.StartsWith("https://exchange.coss.io/"))
            {
                if (!_driver.Url.StartsWith("https://exchange.coss.io/"))
                {
                    NavigateToExchange(new TradingPair("ETH", "BTC"));
                }
            }

            var debugId = Guid.NewGuid().ToString();

            var script = ResUtil.Get("coss-perform-req.js", typeof(CossAgentResDummy).Assembly);
            var effectiveScript = script
                .Replace("{url}", url)
                .Replace("{method}", method)
                .Replace("{debugId}", debugId);

            ExecuteScript(effectiveScript);

            Thread.Sleep(100);
            IWebElement match = null;
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            while (match == null && stopwatch.ElapsedMilliseconds < 5000)
            {
                try { match = _driver.FindElementById(debugId); } catch { }
                if (match == null) { Thread.Sleep(50); }
            }

            if (match != null)
            {
                return match.Text;
            }

            throw new ApplicationException($"Response div was never filled.");
        }

        private void NavigateJs(string url)
        {
            ExecuteScript($"window.location = '{url}';");
            Sleep(TimeSpan.FromSeconds(0.25));
        }

        public void ExecuteScript(string script)
        {
            _driver.ExecuteScript(script);
        }

        public void Login()
        {
            _driver.Navigate().GoToUrl(CossPage.Login);
            // NavigateJs(CossPage.Login);

            if (!_waitForIt.Wait(() => string.Equals((_driver.Title ?? string.Empty).Trim(), "coss.io", StringComparison.InvariantCultureIgnoreCase)))
            {
                throw new ApplicationException("Never got to the login page.");
            }

            // FillInLoginInfo();
            _log.Info("Please login.");

            bool hasSetTfa = false;
            var condition = new Func<bool>(() =>
            {
                try
                {
                    if (!hasSetTfa)
                    {
                        var tfaTextBox = GetTfaTextBoxForLogin();
                        if (tfaTextBox != null)
                        {
                            var tfaValue = _tfaUtil.GetCossTfa();
                            if (SetInputTextAndVerify(tfaTextBox, tfaValue))
                            {
                                hasSetTfa = true;
                            }

                            tfaTextBox.SendKeys(Keys.Enter);
                        }
                    }

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

        private bool FillInLoginInfo()
        {
            var cossCredentials = _configClient.GetCossCredentials();
            if (cossCredentials == null || string.IsNullOrWhiteSpace(cossCredentials.UserName) || string.IsNullOrWhiteSpace(cossCredentials.Password))
            {
                return false;
            }

            var userNameTextBox = WaitForElement(() => _driver.FindElementById("mat-input-0"));
            if (userNameTextBox == null) { return false; }

            if (!SetInputTextAndVerify(userNameTextBox, cossCredentials.UserName))
            {
                return false;
            }

            var passwordTextbox = WaitForElement(() => _driver.FindElementById("mat-input-1"));
            if (passwordTextbox == null) { return false; }
            if (!SetInputTextAndVerify(passwordTextbox, cossCredentials.Password)) { return false; }

            return true;
        }

        public IWebElement WaitForElement(Func<IWebElement> method)
        {
            IWebElement element = null;

            _waitForIt.Wait(() =>
            {
                try
                {
                    element = method();
                    return element != null;
                }
                catch
                {
                    return false;
                }
            }, TimeSpan.FromSeconds(30));

            return element;
        }

        public bool WaitForElementToGoAway(Func<IWebElement> method)
        {
            Thread.Sleep(TimeSpan.FromSeconds(2.5));

            IWebElement debugButton;
            do
            {
                debugButton = null;
                try
                {
                    debugButton = method();
                }
                catch (NoSuchElementException)
                {
                    return true;
                }

                if(debugButton == null) { return true; }

                if (debugButton != null) { Thread.Sleep(TimeSpan.FromSeconds(0.5)); }
            } while (debugButton != null);

            return false;
        }

        public bool SetInputTextAndVerify(IWebElement input, string text)
        {
            input.Clear();
            input.SendKeys(text);

            if (!string.Equals(text, GetInputText(input), StringComparison.Ordinal))
            {
                return false;
            }

            return true;
        }

        public bool SetInputNumberAndVerify(IWebElement input, decimal num)
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

        public string GetInputText(IWebElement input)
        {
            return input.GetAttribute("value");
        }

        public bool Attempt(Func<bool> method, int maxAttempts)
        {
            for (var i = 0; i < maxAttempts; i++)
            {
                if (i != 0) { Sleep(TimeSpan.FromSeconds(i)); }

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

        private IWebElement GetTfaTextBoxForLogin()
        {
            try
            {
                return _driver.FindElementById("mat-input-2");
            }
            catch
            {
                return null;
            }
        }

        public void Sleep(int milliseconds)
        {
            Sleep(TimeSpan.FromMilliseconds(milliseconds));
        }

        public void Sleep(TimeSpan timeSpan)
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

        public bool CheckWallet()
        {
            var retriever = new Func<string>(() =>
            {
                var currentUrl = _driver.Url;
                var prefix = currentUrl.StartsWith("https://exchange.coss.io")
                    ? "exchange"
                    : "profile";

                var url = $"https://{prefix}.coss.io/api/user/wallets";

                return PerformRequest(url);
            });

            var checkWalletCorrelationId = Guid.NewGuid();
            _log.Verbose(TradeEventType.BeginCheckWallet, checkWalletCorrelationId);

            try
            {
                var condition = new Func<bool>(() =>
                {
                    try
                    {
                        var requestTime = DateTime.UtcNow;
                        var contents = retriever();
                        var responseTime = DateTime.UtcNow;

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

        public void NavigateAndVerify(string url, Func<bool> verifier)
        {
            _driver.Navigate().GoToUrl(url);
            if (!_waitForIt.Wait(() => { try { return verifier(); } catch { return false; } }, MaxWaitPageTime))
            {
                throw new ApplicationException("Did not find expected contents within expected time frame.");
            }
        }

        public void Navigate(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) { throw new ArgumentNullException(url); }

            _driver.Navigate().GoToUrl(url);
        }

        public void NavigateToExchange(TradingPair tradingPair)
        {
            NavigateToExchange(tradingPair.Symbol, tradingPair.BaseSymbol);
        }

        public void NavigateToExchange(string symbol, string baseSymbol)
        {
            if (string.IsNullOrWhiteSpace(symbol)) { throw new ArgumentNullException(nameof(symbol)); }
            if (string.IsNullOrWhiteSpace(baseSymbol)) { throw new ArgumentNullException(nameof(baseSymbol)); }

            var url = $"https://exchange.coss.io/exchange/{symbol.Trim().ToLower()}-{baseSymbol.Trim().ToLower()}";

            var verifier = new Func<bool>(() =>
            {
                var expectedText = $"{symbol.Trim().ToUpper()}/{baseSymbol.Trim().ToUpper()}";
                return FindElementWithText("span", expectedText);
            });

            NavigateAndVerify(url, verifier);
        }

        public void NavigateToDashboard()
        {
            const string Url = "https://profile.coss.io/dashboard";

            var verifier = new Func<bool>(() => string.Equals(_driver.Title, "COSS exchange - Crypto One-Stop Solution platform - Dashboard"));

            NavigateAndVerify(Url, verifier);
        }

        public List<OpenOrderEx> RefreshOpenOrders(TradingPair tradingPair)
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

            var container = new CossOpenOrdersForTradingPairContainer
            {
                TimeStampUtc = DateTime.UtcNow,
                Symbol = tradingPair.Symbol,
                BaseSymbol = tradingPair.BaseSymbol,
                OpenOrders = openOrders != null ? openOrders.Select(item => new CossOpenOrder
                {
                    Quantity = item.Quantity,
                    Price = item.Price,
                    OrderType = item.OrderType
                }).ToList() : new List<CossOpenOrder>()
            };

            _openOrderRepo.Insert(container);

            return openOrders;
        }

        public void CancelAllForTradingPair(TradingPair tradingPair, OrderType? orderType = null)
        {
            var myOpenOrders = RefreshOpenOrders(tradingPair);
            for (var i = myOpenOrders.Count() - 1; i >= 0; i--)
            {
                var myOpenOrder = myOpenOrders[i];
                if (orderType.HasValue && orderType.Value != myOpenOrder.OrderType) { continue; }
                myOpenOrder.Cancel();
            }
        }

        public void CancelOrder(string orderId, string symbol, string baseSymbol)
        {
            var script = ResUtil.Get("coss-cancel-order.js", typeof(CossAgentResDummy).Assembly);
            var debugId = "debug_" + Guid.NewGuid().ToString();

            var effectiveScript = script
                .Replace("{orderId}", orderId)
                .Replace("{symbol}", symbol)
                .Replace("{baseSymbol}", baseSymbol)
                .Replace("{debugId}", debugId);

            ExecuteScript(effectiveScript);

            Thread.Sleep(100);
            IWebElement match = null;
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            while (match == null && stopwatch.ElapsedMilliseconds < 5000)
            {
                try { match = _driver.FindElementById(debugId); } catch { }
                if (match == null) { Thread.Sleep(50); }
            }

            if (match != null)
            {
                var text = match.Text;
                var response = JsonConvert.DeserializeObject<CossCancelOrderResponse>(text);
                if (!response.Successful)
                {
                    throw new ApplicationException($"Coss did not report success when attempting to cancel order for order id \"{orderId}\", symbol \"{symbol}\", baseSymbol \"{baseSymbol}\".");
                }
            }

            throw new ApplicationException($"Cancel order failed for order id \"{orderId}\", symbol \"{symbol}\", baseSymbol \"{baseSymbol}\".");
        }

        private class CossCancelOrderResponse
        {
            [JsonProperty("successful")]
            public bool Successful { get; set; }

            [JsonProperty("payload")]
            public string Payload { get; set; }
        }

        public string Xhr(string url)
        {
            var script = ResUtil.Get("coss-xhr.js", typeof(CossAgentResDummy).Assembly);
            var reqId = "debug_" + Guid.NewGuid().ToString();

            var effectiveScript = script
                .Replace("{debugId}", reqId)
                .Replace("{url}", url);

            ExecuteScript(effectiveScript);

            Thread.Sleep(100);
            IWebElement match = null;
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            while (match == null && stopwatch.ElapsedMilliseconds < 5000)
            {
                try { match = _driver.FindElementById(reqId); } catch { }
                if (match == null) { Thread.Sleep(50); }
            }

            if (match != null)
            {
                return match.Text;
            }

            throw new ApplicationException($"Xhr for url {url} failed.");
        }

        public List<OpenOrderForTradingPair> GetOpenOrdersForTradingPair(string symbol, string baseSymbol)
        {
            var xhrs = GetXhrOpenOrders(symbol, baseSymbol);
            return xhrs != null
                ? xhrs.Select(item =>
                {
                    return _cossXhrOpenOrderRepo.XhrToOpenOrder(item);
                }).ToList()
                : new List<OpenOrderForTradingPair>();
        }

        private List<CossXhrOpenOrder> GetXhrOpenOrders(string symbol, string baseSymbol)
        {
            if (string.IsNullOrWhiteSpace(symbol)) { throw new ArgumentNullException(nameof(symbol)); }
            if (string.IsNullOrWhiteSpace(baseSymbol)) { throw new ArgumentNullException(nameof(symbol)); }

            var translator = new Func<string, List<CossXhrOpenOrder>>(text =>
            {
                return !string.IsNullOrWhiteSpace(text)
                    ? JsonConvert.DeserializeObject<List<CossXhrOpenOrder>>(text)
                    : new List<CossXhrOpenOrder>();
            });

            var validator = new Func<string, bool>(text =>
            {
                return !string.IsNullOrWhiteSpace(text);
            });

            var effectiveSymbol = symbol.Trim().ToLower();
            var effectiveBaseSymbol = baseSymbol.Trim().ToLower();
            var key = $"{effectiveSymbol}-{effectiveBaseSymbol}";

            var retriever = new Func<string>(() =>
            {
                try
                {
                    var unixTimeStamp = DateTimeUtil.GetUnixTimeStamp();
                    var url = $"https://exchange.coss.io/api/user/orders/{key}?{unixTimeStamp}";

                    return PerformRequest(url);
                    // return Xhr(url);
                }
                catch
                {
                    Thread.Sleep(TimeSpan.FromSeconds(2.5));
                    try
                    {
                        var unixTimeStamp = DateTimeUtil.GetUnixTimeStamp();
                        var url = $"https://exchange.coss.io/api/user/orders/{key}?{unixTimeStamp}";

                        return PerformRequest(url);
                        // return Xhr(url);
                    }
                    catch
                    {
                        Thread.Sleep(TimeSpan.FromSeconds(2.5));

                        try
                        {
                            var unixTimeStamp = DateTimeUtil.GetUnixTimeStamp();
                            var url = $"https://exchange.coss.io/api/user/orders/{key}?{unixTimeStamp}";

                            return PerformRequest(url);
                            // return Xhr(url);
                        }
                        catch
                        {
                            try { _log.Error($"CossDriver Failed to get open orders for {symbol}-{baseSymbol}"); } catch { }
                            throw;
                        }
                    }
                }
            });

            var colletionContext = _cossXhrOpenOrderRepo.CollectionContext;
            var threshold = TimeSpan.FromMinutes(10);

            var cacheResult = _cacheUtil.GetCacheableEx(ThrottleContext, retriever, colletionContext, threshold, CachePolicy.ForceRefresh, validator, null, key);
            var openOrders = translator(cacheResult?.Contents);

            return openOrders;
        }

        private static object ThrottleLocker = new object();
        private static ThrottleContext ThrottleContext = new ThrottleContext
        {
            Locker = ThrottleLocker,
            ThrottleThreshold = TimeSpan.FromSeconds(0.5)
        };
        
        public List<OpenOrderEx> GetOpenOrders()
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

        public bool PlaceOrder(
            TradingPair tradingPair,
            OrderType orderType,
            QuantityAndPrice quantityAndPrice,
            bool alreadyOnPage = false)
        {
            var quantity = quantityAndPrice.Quantity;
            var price = quantityAndPrice.Price;

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

        public bool ClickPerformSellButton()
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

        public bool ClickBuyToggleButton(string symbol)
        {
            var button = GetBuyToggleButton(symbol);

            if (button == null) { return false; }
            button.Click();

            return true;
        }

        public bool ClickPerformBuyButton()
        {
            // <span class="mat-button-wrapper">Buy</span>
            var elements = _driver.FindElementsByClassName("mat-button-wrapper");
            IWebElement matchingButton = null;
            foreach (var element in elements)
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

        public bool ClickSellToggleButton()
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

        private bool EnterTradePrice(decimal price)
        {
            var element = _driver.FindElementById("input-full-place-order");
            if (element == null) { return false; }
            return SetInputNumberAndVerify(element, price);
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

        private IMongoDatabaseContext DbContext
        {
            get
            {
                return new MongoDatabaseContext(_getConnectionString.GetConnectionString(), "coss");
            }
        }
    }
}
