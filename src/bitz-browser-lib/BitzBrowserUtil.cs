using bit_z_model;
using bitz_data_lib;
using browser_lib;
using config_client_lib;
using log_lib;
using OpenQA.Selenium;
using OpenQA.Selenium.Remote;
using parse_lib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using tfa_lib;
using trade_model;
using wait_for_it_lib;

namespace bitz_browser_lib
{
    public class BitzBrowserUtil : IBitzBrowserUtil
    {
        private readonly IConfigClient _configClient;
        private readonly ITfaUtil _tfaUtil;
        private readonly IWaitForIt _waitForIt;
        private readonly IBitzTradeHistoryRepo _tradeHistoryRepo;
        private readonly IBitzFundsRepo _fundsRepo;
        private readonly IBrowserUtil _browserUtil;
        private readonly ILogRepo _log;

        public BitzBrowserUtil(
            IConfigClient configClient,
            IBitzTradeHistoryRepo tradeHistoryRepo,
            IBitzFundsRepo fundsRepo,
            ITfaUtil tfaUtil,
            IWaitForIt waitForIt,
            ILogRepo log)
        {
            _configClient = configClient;
            _tradeHistoryRepo = tradeHistoryRepo;
            _fundsRepo = fundsRepo;
            _tfaUtil = tfaUtil;
            _waitForIt = waitForIt;
            _log = log;

            _browserUtil = new BrowserUtil(waitForIt);
        }
        
        private Dictionary<string, object> _propertyContainer = new Dictionary<string, object>();

        private object DriverLocker = new object();
        private RemoteWebDriver Driver
        {
            get { return _browserUtil.Driver; }
        }

        public void Run()
        {
            Login();
        }

        public bool RefreshHistory()
        {
            if (!Login()) { return false; }
            if (!NavigateToTradeHistoryPage()) { return false; }

            var currentPageNumber = 1;
            bool keepGoing = true;

            var history = new List<BitzTradeHistoryItem>();
            while (keepGoing)
            {
                keepGoing = false;

                var orderHistoryTable = GetOrderHistoryTableElement();
                if (orderHistoryTable == null) { return false; }
                var rows = orderHistoryTable.FindElements(By.TagName("tr"));

                foreach (var row in rows)
                {
                    var historyItem = new BitzTradeHistoryItem();
                    historyItem.PageNumber = currentPageNumber;

                    var cells = row.FindElements(By.TagName("td"));
                    // Market	Type	Price	Amount	Total	Transaction Time
                    if (cells.Count != 6) { continue; }
                    historyItem.Market = cells[0].Text;
                    historyItem.Type = cells[1].Text;
                    if (decimal.TryParse(cells[2].Text, out decimal price))
                    {
                        historyItem.Price = price;
                    }

                    if (decimal.TryParse(cells[3].Text, out decimal amount))
                    {
                        historyItem.Amount = amount;
                    }

                    historyItem.Total = cells[4].Text;

                    if (DateTime.TryParse(cells[5].Text, out DateTime transactionTime))
                    {
                        historyItem.TransactionTime = transactionTime;
                    }

                    history.Add(historyItem);
                }

                var allUnorderedLists = Driver.FindElementsByTagName("ul");
                if (allUnorderedLists == null) { return false; }
                var pageUl = allUnorderedLists.FirstOrDefault(item =>
                {
                    var cssClass = item.GetAttribute("class");
                    return cssClass != null && cssClass.ToUpper().Contains("pageUl".ToUpper());
                });

                var listItems = pageUl.FindElements(By.TagName("li"));
                var listItemForNextPage = listItems.FirstOrDefault(li => li.Text != null && string.Equals(li.Text.Trim(), (currentPageNumber + 1).ToString()));

                if (listItemForNextPage != null)
                {
                    var anchor = listItemForNextPage.FindElement(By.TagName("a"));
                    if (anchor != null)
                    {
                        keepGoing = true;
                        anchor.SendKeys(Keys.Enter);

                        currentPageNumber++;
                    }
                }
            }

            var container = new BitzTradeHistoryContainer
            {
                TimeStampUtc = DateTime.UtcNow,
                History = history
            };

            _tradeHistoryRepo.InsertTradeHistory(container);

            return true;
        }

        private bool _isLoggedIn = false;
        public bool Login()
        {
            NavigateToLoginPage();

            // <a class="active" href="/lang/switch?locale=en">English</a>
            var anchors = Driver.FindElementsByTagName("a");
            var englishLinkAnchor = anchors.SingleOrDefault(queryAnchor =>
            {
                const string EnglishLinkText = "/lang/switch?locale=en";
                var href = queryAnchor.GetAttribute("href");
                return href != null && href.ToUpper().Contains(EnglishLinkText.ToUpper());
            });

            if (englishLinkAnchor == null) { throw new ApplicationException("Failed to find the English link anchor."); }
            englishLinkAnchor.Click();

            var tabItems = Driver.FindElementsByClassName("tab-item");
            var emailTabItem = tabItems.SingleOrDefault(item =>
            {
                return !string.IsNullOrWhiteSpace(item.Text) && item.Text.ToUpper().Contains("Email".ToUpper());
            });

            if (emailTabItem == null)
            {
                throw new ApplicationException("Failed to find the Email tab item.");
            }

            emailTabItem.Click();

            var creds = _configClient.GetBitzLoginCredentials();
            if (creds == null) { throw new ApplicationException("Failed to retrieve Bit-Z login credentials."); }
            if (string.IsNullOrWhiteSpace(creds.UserName)) { throw new ApplicationException("Empty Bit-Z user name."); }
            if (string.IsNullOrWhiteSpace(creds.Password)) { throw new ApplicationException("Empty Bit-Z password."); }

            Thread.Sleep(TimeSpan.FromSeconds(5));

            var scriptFormat =
@"
var userNameTextBox = document.querySelectorAll('[name=""username""]')[1];
userNameTextBox.value = '{0}';

var passwordTextBox = document.querySelectorAll('[name=""pwd""]')[0];
passwordTextBox.value = '{1}';
";

            var script = string.Format(scriptFormat, creds.UserName, creds.Password);

            Driver.ExecuteScript(script);

            var signInButton = Driver.FindElementById("captcha-button"); //  Driver.FindElementById("lg_btn");
            if (signInButton == null) { return false; }
            signInButton.Click();
            signInButton.SendKeys(" ");

            if (!WaitForTwoFactorLogin()) { return false; }

            var tfa = _tfaUtil.GetBitzTfa();
            if (string.IsNullOrWhiteSpace(tfa)) { throw new ApplicationException("Failed to get Bit-Z TFA"); }

            var twoFactorTextBox = Driver.FindElementById("code");
            if (twoFactorTextBox == null) { return false; }
            if (!SetInputTextAndVerify(twoFactorTextBox, tfa)) { return false; }

            var continueButton = Driver.FindElementsByTagName("input").FirstOrDefault(item => string.Equals(item.GetAttribute("type"), "submit", StringComparison.InvariantCultureIgnoreCase));
            if (continueButton == null) { return false; }
            continueButton.Click();

            _isLoggedIn = true;

            return true;
        }

        private void Info(string message)
        {
            Console.WriteLine($"{DateTime.Now} (local) - {message}");
            _log.Info(message);
        }

        public bool UpdateFunds()
        {
            Info("Beginning the Update Funds Workflow.");

            if (!_isLoggedIn) { Login(); }
            if (!_isLoggedIn) { return false; }

            NavigateToBalancePage();

            var balanceTable = _browserUtil.WaitForElement(() => Driver.FindElementByClassName("ucbal_table"));
            if (balanceTable == null) { return false; }

            var rows = balanceTable.FindElements(By.TagName("tr"));

            var funds = new List<BitzFund>();
            // skip past the first row.
            foreach (var row in rows.Skip(1).ToList())
            {                
                var cells = row.FindElements(By.TagName("td"));
                if (cells.Count != 8) { continue; }

                var fund = new BitzFund
                {
                    Symbol = cells[0].Text.Trim(),
                    Name = cells[1].Text.Trim(),
                    TotalBalance = ParseUtil.DecimalTryParse(cells[2].Text.Trim()),
                    AvailableBalance = ParseUtil.DecimalTryParse(cells[3].Text.Trim()),
                    FrozenBalance = ParseUtil.DecimalTryParse(cells[4].Text.Trim()),
                    BtcBalanceValue = ParseUtil.DecimalTryParse(cells[5].Text.Trim())
                };

                // cell 6 is a spacer
                var actionsCell = cells[7];
                var actionAnchors = actionsCell.FindElements(By.TagName("a"));
                if (actionAnchors.Count == 2)
                {
                    var depositAnchor = actionAnchors[0];                    
                    fund.CanDeposit = depositAnchor.Text.ToUpper().Contains("Deposit".ToUpper());
                    if (fund.CanDeposit) { fund.DepositLink = depositAnchor.GetAttribute("href"); }

                    var withdrawAnchor = actionAnchors[1];
                    fund.CanWithdraw = withdrawAnchor.Text.ToUpper().Contains("Withdraw".ToUpper());
                    if (fund.CanWithdraw) { fund.WithdrawLink = withdrawAnchor.GetAttribute("href"); }
                }

                funds.Add(fund);
            }

            foreach (var fund in funds)
            {
                if (!fund.CanDeposit || string.IsNullOrWhiteSpace(fund.DepositLink)) { continue; }
                Driver.Navigate().GoToUrl(fund.DepositLink);
                Thread.Sleep(TimeSpan.FromSeconds(1));

                var addressElement = Driver.FindElementByClassName("coinin_address");
                var addressPieceDivs = addressElement.FindElements(By.TagName("div"));

                if (addressPieceDivs == null || !addressPieceDivs.Any())
                {
                    // simple address
                    fund.DepositAddress = new DepositAddress
                    {
                        Address = addressElement.Text.Trim()
                    };
                }
                else
                {
                    // complex address
                    if (addressPieceDivs.Count != 2)
                    {
                        // only setup to work with 2 piece complex addresses.
                        continue;
                    }

                    var addressPiece = addressPieceDivs[0];
                    var addressSpans = addressPiece.FindElements(By.TagName("span"));
                    if (addressSpans.Count != 2) { continue; }
                    var addressText = addressSpans[1].Text.Trim();

                    var memoPiece = addressPieceDivs[1];
                    var memoSpans = addressPiece.FindElements(By.TagName("span"));
                    if (memoSpans.Count != 2) { continue; }
                    var memoText = memoSpans[1].Text.Trim();

                    fund.DepositAddress = new DepositAddress
                    {
                        Address = addressText,
                        Memo = memoText
                    };
                }
            }

            var container = new BitzFundsContainer
            {
                TimeStampUtc = DateTime.UtcNow,
                Funds = funds
            };

            _fundsRepo.Insert(container);

            return true;
        }

        private bool NavigateToBalancePage()
        {
            const string Url = "https://www.bit-z.com/user_balance";
            Driver.Navigate().GoToUrl(Url);

            return true;
        }

        private bool NavigateToTradeHistoryPage()
        {
            const string Url = "https://www.bit-z.com/user_orders/history";
            Driver.Navigate().GoToUrl(Url);

            return true;
        }

        private bool NavigateToLoginPage()
        {
            // "https://www.bit-z.com/user/signin";

            const string LoginUrl = "https://u.bitz.com/login?ref=home";            
            Driver.Navigate().GoToUrl(LoginUrl);

            return true;
        }

        private IWebElement GetOrderHistoryTableElement()
        {
            return Driver.FindElementsByTagName("table").FirstOrDefault(item =>
            {
                try
                {
                    var cssClass = item.GetAttribute("class");
                    return string.Equals(cssClass, "ordhis_table", StringComparison.InvariantCultureIgnoreCase);
                }
                catch
                {
                    return false;
                }
            });
        }

        private bool WaitForTwoFactorLogin()
        {
            return _waitForIt.Wait(() => Driver.Url.ToUpper().Contains("/signin2".ToUpper()), TimeSpan.FromSeconds(10), TimeSpan.FromMilliseconds(500));
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

        private string GetInputText(IWebElement input)
        {
            return input.GetAttribute("value");
        }

        public void Dispose()
        {
            Driver.Dispose();
        }
    }
}
