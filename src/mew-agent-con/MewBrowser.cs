using binance_lib;
using browser_lib;
using config_lib;
using coss_lib;
using hitbtc_lib;
using kucoin_lib;
using livecoin_lib;
using log_lib;
using mew_agent_con.Models;
using OpenQA.Selenium;
using OpenQA.Selenium.Remote;
using qryptos_lib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using trade_lib;
using cache_lib.Models;
using trade_model;
using trade_res;
using wait_for_it_lib;

namespace mew_agent_con
{
    public class MewBrowser : IMewBrowser
    {
        private readonly IWaitForIt _waitForIt;
        private readonly IBrowserUtil _browserUtil;
        private readonly IConfigRepo _configRepo;

        private readonly IBinanceIntegration _binance;
        private readonly ICossIntegration _coss;
        private readonly IHitBtcIntegration _hitBtc;
        private readonly IQryptosIntegration _qryptos;
        private readonly IKucoinIntegration _kucoin;
        private readonly ILivecoinIntegration _livecoin;

        private readonly IDepositAddressValidator _depositAddressValidator;
        private readonly ILogRepo _log;

        private static Dictionary<string, decimal> MinimumGasDictionary = new Dictionary<string, decimal>(StringComparer.InvariantCultureIgnoreCase)
        {
            { "ENJ", 35000 },
            { "PPT", 50000 },
            { "ETH", 20000 }
        };

        private RemoteWebDriver Driver { get { return _browserUtil.Driver; } }

        private static readonly List<Commodity> StandardCommodities = new List<Commodity>
        {
            CommodityRes.Bnb,
            CommodityRes.RequestNetwork
        };

        private List<ITradeIntegration> Exchanges
        {
            get
            {
                return new List<ITradeIntegration>
                {
                    _binance,
                    _coss,
                    _hitBtc,
                    _qryptos,
                    _kucoin,
                    _livecoin
                };
            }
        }

        public MewBrowser(
            IWaitForIt waitForIt,
            IBrowserUtil browserUtil,
            IConfigRepo configRepo,
            IBinanceIntegration binance,
            ICossIntegration coss,
            IHitBtcIntegration hitBtc,
            IQryptosIntegration qryptos,
            IKucoinIntegration kucoin,
            ILivecoinIntegration livecoin,
            IDepositAddressValidator depositAddressValidator,
            ILogRepo log)
        {
            _waitForIt = waitForIt;
            _browserUtil = browserUtil;
            _configRepo = configRepo;
            _binance = binance;
            _coss = coss;
            _hitBtc = hitBtc;
            _qryptos = qryptos;
            _kucoin = kucoin;
            _livecoin = livecoin;
            _depositAddressValidator = depositAddressValidator;
            _log = log;
        }

        public void Run()
        {
            Login();
        }

        public void Send(Commodity commodity, QuantityToSend quantity, string exchangeName)
        {
            if (commodity == null) { throw new ArgumentNullException(nameof(commodity)); }
            if (commodity.Id == default(Guid)) { throw new ArgumentNullException(nameof(commodity.Id)); }

            var exchange = Exchanges.SingleOrDefault(queryExchange => string.Equals(queryExchange.Name, exchangeName, StringComparison.InvariantCultureIgnoreCase));
            if (exchange == null) { throw new ApplicationException($"Unrecognized exchange \"{exchangeName}\"."); }

            // Some tokens don't like to be custom. Not sure why...
            var mustUseStandard = new List<Commodity>
            {
                CommodityRes.Eth,
                CommodityRes.Agrello,
                CommodityRes.District0x,
                CommodityRes.Utrust,
                CommodityRes.Augur,
                CommodityRes.Poe
            };

            if (mustUseStandard.Any(queryStandard => queryStandard.Id == commodity.Id))
            {
                SendStandardToken(exchange, commodity, quantity);
                return;
            }

            var depositAddress = exchange.GetDepositAddress(commodity.Symbol, CachePolicy.ForceRefresh);
            if (depositAddress == null) { throw new ApplicationException($"Couldn't find a deposit address for \"{commodity.Symbol}\" on \"{exchange.Name}\"."); }

            _depositAddressValidator.ValidateEthOrEthTokenAddress(depositAddress.Address);
            
            SendCustomToken(depositAddress.Address, commodity, quantity);
        }

        private bool SendSomeVztToCoss()
        {
            return SendCustomToken(_coss, CommodityRes.Vezt, new QuantityToSend(2000));
        }

        private bool SendAllReqToCoss()
        {
            return SendStandardToken(_coss, CommodityRes.RequestNetwork, QuantityToSend.All);
        }

        public void SendAllNcashToBinance()
        {
            SendCustomToken(_binance, CommodityRes.NCash, QuantityToSend.All);
        }

        public void SendAllOmisegoToBinance()
        {
            SendStandardToken(_binance, CommodityRes.Omisego, QuantityToSend.All);
        }

        public void SendAllLendToBinance()
        {
            SendCustomToken(_binance, CommodityRes.Lend, QuantityToSend.All);
        }

        public void SendAllIotxToBinance()
        {
            SendCustomToken(_binance, CommodityRes.IoTX, QuantityToSend.All);
        }

        public void SendAllPoeToBinance()
        {
            SendStandardToken(_binance, CommodityRes.Poe, QuantityToSend.All);
        }

        public void SendAllSubToBinance()
        {
            SendStandardToken(_binance, CommodityRes.Substratum, QuantityToSend.All);
        }

        public void SendAllMonacoToBinance()
        {
            SendStandardToken(_binance, CommodityRes.Monaco, QuantityToSend.All);
        }

        public void SendAllArnToBinance()
        {
            SendCustomToken(_binance, CommodityRes.Aeron, QuantityToSend.All);
        }

        public void SendAllSonmToBinance()
        {
            SendCustomToken(_binance, CommodityRes.Sonm, QuantityToSend.All);
        }

        public void SendAllToBinance(Commodity commodity)
        {
            if (StandardCommodities.Any(queryCommodity => queryCommodity.Id == commodity.Id))
            {
                SendStandardToken(_binance, commodity, QuantityToSend.All);
            }
            else
            {
                SendCustomToken(_binance, commodity, QuantityToSend.All);
            }
        }

        private bool SendCustomToken(ITradeIntegration exchange, Commodity commodity, QuantityToSend quantity)
        {
            var depositAddress = exchange.GetDepositAddress(commodity.Symbol, CachePolicy.AllowCache);
            if (depositAddress == null) { throw new ApplicationException($"Failed to retreive deposit address from exchange {exchange.Name} for commodity {commodity.Name}."); }

            return SendCustomToken(depositAddress.Address, commodity, quantity);
        }

        public void SendEthToBinance(QuantityToSend quantity)
        {
            var commodity = CommodityRes.Eth;
            var exchange = _binance;

            var depositAddress = exchange.GetDepositAddress(commodity.Symbol, CachePolicy.ForceRefresh);
            if (depositAddress == null) { throw new ApplicationException($"Failed to retreive deposit address from exchange {exchange.Name} for commodity {commodity.Name}."); }

            SendEth(depositAddress.Address, quantity);
        }

        public void SendEthToCoss(QuantityToSend quantity)
        {
            var commodity = CommodityRes.Eth;
            var exchange = _coss;

            var depositAddress = exchange.GetDepositAddress(commodity.Symbol, CachePolicy.ForceRefresh);
            if (depositAddress == null) { throw new ApplicationException($"Failed to retreive deposit address from exchange {exchange.Name} for commodity {commodity.Name}."); }

            SendEth(depositAddress.Address, quantity);
        }
        private void SendEth(string depositAddress, QuantityToSend quantity)
        {
            if (string.IsNullOrWhiteSpace(depositAddress)) { throw new ArgumentNullException(nameof(depositAddress)); }
            if (quantity == null) { throw new ArgumentNullException(nameof(quantity)); }
            if (!quantity.SendAll && quantity.Value <= 0) { throw new ArgumentException($"{nameof(quantity)} - Must ether set SendAll or Value must be > 0."); }

            Login();

            if (quantity.SendAll)
            {
                ClickSendEntireBalance();
                _browserUtil.Sleep(TimeSpan.FromSeconds(2.5));
            }
            else
            {
                SetAmountToSend(quantity.Value);
            }

            SetDepositAddress(depositAddress);

            _browserUtil.Sleep(TimeSpan.FromSeconds(5));

            ClickGenerateTransaction();

            _browserUtil.Sleep(TimeSpan.FromSeconds(5));

            ClickSendTransaction();

            _browserUtil.Sleep(TimeSpan.FromSeconds(5));

            ClickConfirmTransaction();
            // sometimes the last part takes awhile.
            // todo: add a way to detect that it worked.
            _browserUtil.Sleep(TimeSpan.FromSeconds(120));
        }

        private bool SendCustomToken(string depositAddress, Commodity commodity, QuantityToSend quantity)
        {
            if (string.IsNullOrWhiteSpace(depositAddress)) { throw new ArgumentNullException(nameof(depositAddress)); }
            if (commodity == null) { throw new ArgumentNullException(nameof(commodity)); }
            if (!commodity.Decimals.HasValue || commodity.Decimals.Value <= 0) { throw new ArgumentOutOfRangeException($"{nameof(commodity.Decimals)} must be > 0."); }
            if (!commodity.IsEth && !(commodity.IsEthToken ?? false)) { throw new ArgumentOutOfRangeException($"{nameof(commodity)} must be Ethereum or an Ethereum token."); }
            if (string.IsNullOrWhiteSpace(commodity.ContractId)) { throw new ArgumentNullException(nameof(commodity.ContractId)); }
            if (quantity == null) { throw new ArgumentNullException(nameof(quantity)); }
            
            Login();
            ClickAddCustomToken();

            _browserUtil.Sleep(TimeSpan.FromSeconds(2.5));

            var customSymbol = $"CUSTOM_{commodity.Symbol}";

            SetCustomContractAddress(commodity.ContractId);
            SetCustomContractSymbol(customSymbol);
            SetCustomContractDecimals(commodity.Decimals.Value);

            _browserUtil.Sleep(TimeSpan.FromSeconds(2.5));

            SaveCustomToken();

            _browserUtil.Sleep(TimeSpan.FromSeconds(2.5));

            ClickEthDropdown();

            _browserUtil.Sleep(TimeSpan.FromSeconds(2.5));

            ClickSymbolDropdownOption(customSymbol);

            _browserUtil.Sleep(TimeSpan.FromSeconds(2.5));

            if (quantity.SendAll)
            {
                ClickSendEntireBalance();
                _browserUtil.Sleep(TimeSpan.FromSeconds(2.5));
            }
            else
            {
                SetAmountToSend(quantity.Value);
            }
            
            SetDepositAddress(depositAddress);

            _browserUtil.Sleep(TimeSpan.FromSeconds(5));

            ClickGenerateTransaction();

            _browserUtil.Sleep(TimeSpan.FromSeconds(5));

            ClickSendTransaction();

            _browserUtil.Sleep(TimeSpan.FromSeconds(5));

            ClickConfirmTransaction();
            // sometimes the last part takes awhile.
            // todo: add a way to detect that it worked.
            _browserUtil.Sleep(TimeSpan.FromSeconds(120));

            return true;
        }

        private void SaveCustomToken()
        {
            const string Script = "saveTokenToLocal()";
            AngularExecuteWalletBalanceController(Script);
        }

        private void SetCustomContractDecimals(int decimals)
        {
            var script = $"localToken.decimals = '{decimals}'";
            AngularExecuteWalletBalanceController(script);
        }

        private void SetCustomContractSymbol(string symbol)
        {
            var script = $"localToken.symbol = '{symbol}'";
            AngularExecuteWalletBalanceController(script);
        }

        private void SetCustomContractAddress(string address)
        {
            var script = $"addressDrtv.ensAddressField = '{address}'";
            AngularExecuteWalletBalanceController(script);
        }

        public void SendAllQuarkChainToBinance()
        {
            SendCustomToken(_binance, CommodityRes.QuarkChain, QuantityToSend.All);
        }

        private void ClickAddCustomToken()
        {
            AngularExecuteWalletBalanceController("customTokenField = !customTokenField");
        }

        public void SendAllPptToBinance()
        {
            SendStandardToken(_binance, CommodityRes.Populous, QuantityToSend.All);
        }

        public void SendAllEnjToBinance()
        {            
            SendStandardToken(_binance, CommodityRes.EnjinCoin, QuantityToSend.All);
        }

        public void SendAllIcnToBinance()
        {
            SendStandardToken(_binance, CommodityRes.Iconomi, QuantityToSend.All);
        }

        public void SendAllAmbrosousToBinance()
        {
            SendStandardToken(_binance, CommodityRes.Ambrosous, QuantityToSend.All);
        }

        public void SendAllAeronToBinance()
        {
            SendStandardToken(_binance, CommodityRes.Aeron, QuantityToSend.All);
        }

        public void SendAllPopulousToBinance()
        {
            SendStandardToken(_binance, CommodityRes.Populous, QuantityToSend.All);
        }

        public void SendAllBancorToBinance()
        {
            SendStandardToken(_binance, CommodityRes.BancorNetworkToken, QuantityToSend.All);
        }

        private bool SendStandardToken(ITradeIntegration integration, Commodity commodity, QuantityToSend quantityToSend)
        {
            var destinationAddressTask =
                Task.Run(() =>
                {
                    try
                    {
                        // TODO: Should also get the contract id from the integration
                        // TODO: and verify that they match the contract that we're Mew is using.

                        var address = integration.GetDepositAddress(commodity.Symbol, CachePolicy.ForceRefresh);
                        if (address == null || string.IsNullOrWhiteSpace(address.Address))
                        {
                            throw new ApplicationException($"Failed to retrieve {commodity} deposit address from {integration.Name}");
                        }

                        return address.Address.Trim();
                    }
                    catch (Exception exception)
                    {
                        _log.Error(exception);
                        throw;
                    }
                });

            if (!Login()) { return false; }
            WithdrawSimpleToken(commodity, destinationAddressTask.Result, quantityToSend);

            return true;
        }
        
        public bool Login()
        {
            NavigateToMainPage();
            CloseTheWarning();
            ClickSendEtherMenuItem();
            ClickKeystoreJsonFileRadio();
            EnterFileNameAndPassword();

            return true;
        }

        private void WithdrawSimpleToken(Commodity commodity, string destinationAddress, QuantityToSend quantity)
        {
            if (commodity == null) { throw new ArgumentNullException(nameof(Commodity)); }
            if (string.IsNullOrWhiteSpace(commodity.Symbol)) { throw new ArgumentNullException(nameof(commodity.Symbol)); }
            if (string.IsNullOrWhiteSpace(destinationAddress)) { throw new ArgumentNullException(destinationAddress); }
            if (quantity == null) { throw new ArgumentNullException(nameof(quantity)); }

            if (commodity.Id != CommodityRes.Eth.Id
                && !commodity.IsEth
                && !string.Equals(commodity.Symbol, CommodityRes.Eth.Symbol, StringComparison.InvariantCultureIgnoreCase))
            {

                ShowAllTokens();
                _browserUtil.Sleep(TimeSpan.FromSeconds(2.5));

                // More work needs to be done if it's not one of MEW's standard tokens.
                LoadSymbol(commodity.Symbol);
                _browserUtil.Sleep(TimeSpan.FromSeconds(2.5));

                ClickEthDropdown();
                _browserUtil.Sleep(TimeSpan.FromSeconds(2.5));

                ClickSymbolDropdownOption(commodity.Symbol);
                _browserUtil.Sleep(TimeSpan.FromSeconds(2.5));
            }

            if (quantity.SendAll)
            {
                ClickSendEntireBalance();
                _browserUtil.Sleep(TimeSpan.FromSeconds(2.5));
            }
            else
            {
                SetAmountToSend(quantity.Value);
            }

            _browserUtil.Sleep(TimeSpan.FromSeconds(2.5));

            SetDepositAddress(destinationAddress);

            _browserUtil.Sleep(TimeSpan.FromSeconds(5));

            ClickGenerateTransaction();

            _browserUtil.Sleep(TimeSpan.FromSeconds(5));

            ClickSendTransaction();

            _browserUtil.Sleep(TimeSpan.FromSeconds(5));

            ClickConfirmTransaction();

            // sometimes the last part takes awhile.
            // todo: add a way to detect that it worked.
            _browserUtil.Sleep(TimeSpan.FromMinutes(2));
        }       

        private void ClickGenerateTransaction()
        {
            RunAngular("generateTx()");
        }

        private void ClickSendTransactionOld()
        {
            RunAngular("parseSignedTx( signedTx )");
        }

        private void ClickSendTransaction()
        {
            // parseSignedTx( signedTx )
            const string Script =
@"var items = document.getElementsByTagName('a');
for (var i = 0; i < items.length; i++)
{
    var item = items[i];
    if (item.innerText && item.innerText.indexOf('Send Transaction') != -1)
    {
        item.click();
        break;
    }
}";

            ExecuteScript(Script);
        }

        private void ClickConfirmTransaction()
        {
            const string Script =
@"var items = document.getElementsByTagName('button');
for (var i = 0; i < items.length; i++)
{
    var item = items[i];
    if (item.innerText && item.innerText.indexOf('Yes, I am sure! Make transaction.') != -1)
    {
        item.click();
        break;
    }
}";

            ExecuteScript(Script);
        }

        private IWebElement GetGenerateTransactionButton()
        {
            return _browserUtil.WaitForElement(() => Driver.FindElementByPartialLinkText("Generate Transaction"));
        }

        private IWebElement GetGasLimitTextBox()
        {
            return _browserUtil.WaitForElement(() =>
            {
                var allDivs = Driver.FindElementsByTagName("div");
                foreach (var div in allDivs)
                {
                    var label = div.FindElement(By.TagName("label"));
                    if (label == null || label.Text == null || !string.Equals(label.Text.Trim(), "Gas Limit", StringComparison.InvariantCultureIgnoreCase)) { continue; }

                    var input = div.FindElement(By.TagName("input"));
                    if (input == null) { continue; }

                    var ngModel = div.GetAttribute("ng-model");
                    if (ngModel != null && string.Equals(ngModel, "tx.gasLimit", StringComparison.InvariantCultureIgnoreCase))
                    {
                        return input;
                    }
                }

                return null;
            });
        }

        private IWebElement GetAddressTextBox()
        {
            return _browserUtil.WaitForElement(() =>
            {
                var allDivs = Driver.FindElementsByTagName("div");
                foreach (var div in allDivs)
                {
                    try
                    {
                        var label = div.FindElement(By.TagName("label"));
                        if (label == null || label.Text == null || !string.Equals(label.Text.Trim(), "To Address", StringComparison.InvariantCultureIgnoreCase)) { continue; }
                        var input = div.FindElement(By.TagName("input"));
                        if (input == null) { continue; }
                        var ngModel = div.GetAttribute("ng-model");

                        if (ngModel != null && string.Equals(ngModel, "addressDrtv.ensAddressField", StringComparison.InvariantCultureIgnoreCase))
                        {
                            return input;
                        }
                    }
                    catch
                    {
                        continue;
                    }
                }

                return null;
            });
        }

        private void ClickSendEntireBalance()
        {
            const string Script =
@"(function () {
  var anchors = document.getElementsByTagName('a');
  for (var i = 0; i < anchors.length; i++) {
    var anchor = anchors[i];
    var ngClick = anchor.getAttribute('ng-click');
    if (ngClick === 'transferAllBalance()') {
      anchor.click();
      return;
    }
  }
})();
";
            ExecuteScript(Script);
        }

        private IWebElement GetSendEntireBalanceLink()
        {
            return _browserUtil.WaitForElement(() =>
                Driver.FindElementsByTagName("a")
                .SingleOrDefault(item =>
                {
                    var click = item.GetAttribute("ng-click");
                    return click != null && string.Equals(click, "transferAllBalance()", StringComparison.InvariantCultureIgnoreCase);
                })
            );
        }

        private IWebElement GetEthDropdown()
        {
            return _browserUtil.WaitForElement(() =>
                Driver.FindElementsByTagName("a")
                .SingleOrDefault(item =>
                {
                    return item.FindElements(By.TagName("strong"))
                    .SingleOrDefault(queryStrong =>
                    {
                        var strongText = queryStrong.Text;
                        return strongText != null &&
                            string.Equals(strongText.Replace("\"", string.Empty).Trim(), "ETH", StringComparison.InvariantCultureIgnoreCase);
                    }) != null;                    
                })
            );
        }

        private void ClickEthDropdown()
        {
            const string Script =
@"(function () {
  var anchors = document.getElementsByTagName('a');
  for (var i = 0; i < anchors.length; i++) {
    var anchor = anchors[i];
    var strongs = anchor.getElementsByTagName('strong');
    for (var j = 0; j < strongs.length; j++) {
      var strong = strongs[j];
      var strongText = strong.innerText.trim();
      if (strongText === 'ETH') {
        strong.click();
        return;
      }
    }
 }
})();";

            ExecuteScript(Script);
        }

        private void SetAmountToSend(decimal amountToSend)
        {
            var amountToSendText = amountToSend.ToString();

            RunAngular($"tx.value = {amountToSend}");
        }

        private void SetDepositAddress(string depositAddress)
        {
            RunAngular($"addressDrtv.ensAddressField = '{depositAddress}'");
        }

        private void RunAngular(string angular)
        {
            var sanitizedAngular = angular.Replace("'", "\\'");

            var script = "angular.element(document.getElementsByTagName('main')[0]).scope().$apply('" + sanitizedAngular + @"');";
            var js = (IJavaScriptExecutor)Driver;

            js.ExecuteScript(script);
        }

        private void ExecuteScript(string script)
        {
            ((IJavaScriptExecutor)Driver).ExecuteScript(script);
        }

        private void ClickSymbolDropdownOption(string symbol)
        {
            var script =
@"(function () {
  var listItems = document.getElementsByTagName('li');
  for (var i = 0; i < listItems.length; i++) {
    var listItem = listItems[i];
    var anchors = listItem.getElementsByTagName('a');
    for (var j = 0; j < anchors.length; j++) {
      var anchor = anchors[j];
      var anchorText = anchor.innerText.trim();
      if (anchorText === '" + symbol.ToUpper().Trim() + @"') {
        anchor.click();
        return;
      }
    }
  }

  // something went wrong.
  window.location = 'about:blank';
})();";

            ExecuteScript(script);
        }

        private IWebElement GetSymbolDropdownOption(string symbol)
        {
            return _browserUtil.WaitForElement(() =>
                    Driver.FindElementsByTagName("a")
                    .SingleOrDefault(item =>
                    {
                        var cssClass = item.GetAttribute("class");
                        if (cssClass == null || !string.Equals(cssClass, "ng-binding", StringComparison.InvariantCultureIgnoreCase)) { return false; }
                        var itemText = item.Text;
                        return itemText != null && string.Equals(itemText.Trim(), symbol, StringComparison.InvariantCultureIgnoreCase);
                    })
                );
        }

        private bool EnterFileNameAndPassword()
        {
            var fileInput = GetFileInput();
            if (fileInput == null) { return false; }

            var walletFileName = _configRepo.GetMewWalletFileName();
            if (string.IsNullOrWhiteSpace(walletFileName))
            {
                _log.Error("MEW Wallet File Name is not configured.");
                return false;
            }

            fileInput.SendKeys(walletFileName);

            var passwordInput = GetPasswordInput();
            if (passwordInput == null) { return false; }

            var password = _configRepo.GetMewPassword();
            if (string.IsNullOrWhiteSpace(password))
            {
                _log.Error("MEW Password is not configured.");
                return false;
            }

            passwordInput.SendKeys(password);

            ClickUnlockWallet();

            return true;
        }

        private void ClickUnlockWallet()
        {
            const string Script =
@"var paragraphs = document.getElementsByTagName('p');
for(var i = 0; i < paragraphs.length; i++){
  var paragraph = paragraphs[i];
  if (angular.element(paragraph).scope().decryptWallet != null) {
    angular.element(paragraph).scope().$apply('decryptWallet()');
	break;
  }
}";

            ExecuteScript(Script);
        }

        private void LoadSymbol(string symbol)
        {
            var script =
"var symbol = '" + symbol + @"';
var rows = document.getElementsByTagName('tr');
for (var i = 0; i < rows.length; i++) {
  var row = rows[i];
  var cells = row.getElementsByTagName('td');
  for (var j = 0; j < cells.length; j++) {
    var cell = cells[j];
    var cellText = cell.innerText.trim();
    if (cellText === symbol) {
      var otherCell = cells[0];
      var spans = otherCell.getElementsByTagName('span');
      if (spans) { spans[0].click(); }
    }
  }
}";

            ExecuteScript(script);
        }

        private bool LoadSymbolOld(Commodity symbol)
        {
            var accountInfoTable = GetAccountInfoTable();
            if (accountInfoTable == null) { return false; }
            var tableBody = accountInfoTable.FindElement(By.TagName("tbody"));
            if (tableBody == null) { return false; }
            var rows = tableBody.FindElements(By.TagName("tr"));
            if (rows == null) { return false; }

            foreach (var row in rows)
            {
                var cells = row.FindElements(By.TagName("td")).ToList();
                if (cells == null || cells.Count != 2) { return false; }
                var removeTokenButton = cells[0].FindElement(By.TagName("img"));
                var clickToLoadButton = cells[0].FindElement(By.TagName("span"));
                var cellSymbol = cells[1].Text;

                if (cellSymbol != null && string.Equals(cellSymbol.Trim(), symbol.Symbol, StringComparison.InvariantCultureIgnoreCase))
                {
                    if (clickToLoadButton != null)
                    {
                        clickToLoadButton.Click();
                        return true;
                    }

                    return false;
                }
            }

            return false;
        }

        private IWebElement GetAccountInfoTable()
        {
            return _browserUtil.WaitForElement(() =>
                Driver.FindElementsByTagName("table")
                .SingleOrDefault(item =>
                {
                    var cssClass = item.GetAttribute("class");
                    return cssClass != null && string.Equals(cssClass.Trim(), "account-info", StringComparison.InvariantCultureIgnoreCase);
                })
            );
        }

        private void ShowAllTokens()
        {
            AngularExecuteWalletBalanceController("showAllTokens = true");
        }
        
        private void AngularExecuteWalletBalanceController(string angular)
        {
            var sanitizedAngular = angular.Replace("'", "\\'");
            var script = "angular.element(document.getElementsByTagName('aside')[0]).scope().$apply('" + sanitizedAngular + "');";
            ExecuteScript(script);
        }

        private IWebElement GetShowAllTokensButton()
        {
            return _browserUtil.WaitForElement(() =>
                Driver.FindElementsByTagName("a")
                .SingleOrDefault(item =>
                item.Text != null && string.Equals(item.Text.Trim(), "Show All Tokens", StringComparison.InvariantCultureIgnoreCase))
            );
        }

        private IWebElement GetUnlockButton()
        {
            return _browserUtil.WaitForElement(() =>
                Driver.FindElementsByTagName("div")
                .Where(item =>
                {
                    var cssClass = item.GetAttribute("class");
                    return cssClass != null && string.Equals(cssClass, "form-group");
                })
                .SingleOrDefault(item =>
                {
                    return item.FindElements(By.TagName("a"))
                    .SingleOrDefault(queryAnchor =>
                    {
                        var anchorText = queryAnchor.Text;
                        return !string.IsNullOrWhiteSpace(anchorText) && string.Equals(anchorText, "Unlock", StringComparison.InvariantCultureIgnoreCase);
                    }) != null;
                }));
        }

        private IWebElement GetFileInput()
        {
            // <input style="display:none;" type="file" on-read-file="showContent($fileContent)" id="fselector">
            return _browserUtil.WaitForElement(() =>
                Driver.FindElementsByTagName("input")
                .SingleOrDefault(item =>
                {
                    var itemType = item.GetAttribute("type");
                    return itemType != null && string.Equals(itemType, "file");
                })
            );
        }

        private IWebElement GetPasswordInput()
        {
            return _browserUtil.WaitForElement(() =>
                Driver.FindElementsByTagName("input")
                .SingleOrDefault(item =>
                {
                    //var itemType = item.GetAttribute("type");
                    //return !string.IsNullOrWhiteSpace(itemType) && string.Equals(itemType, "password", StringComparison.InvariantCultureIgnoreCase);

                    var ngModel = item.GetAttribute("ng-model");
                    // ng-model="$parent.$parent.filePassword"
                    return !string.IsNullOrWhiteSpace(ngModel) && string.Equals(ngModel, "$parent.$parent.filePassword");
                })
            );
        }

        private bool ClickKeystoreJsonFileRadio()
        {
            // <input aria-flowto="aria6" aria-label="Keystore JSON file" type="radio" ng-model="walletType" value="fileupload" class="ng-pristine ng-untouched ng-valid ng-empty" name="295">

            var radio = _browserUtil.WaitForElement(() =>
                Driver.FindElementsByTagName("input")
                .SingleOrDefault(item =>
                {
                    var itemType = item.GetAttribute("type");
                    if (!string.Equals(itemType, "radio", StringComparison.InvariantCultureIgnoreCase)) { return false; }

                    var ariaLabel = item.GetAttribute("aria-label");
                    return ariaLabel != null && string.Equals(ariaLabel, "Keystore JSON file", StringComparison.InvariantCultureIgnoreCase);
                })
            );

            if (radio == null) { return false; }

            radio.SendKeys(Keys.Space);

            return true;
        }

        private bool NavigateToMainPage()
        {
            const string Url = "http://localhost/mew";
            Driver.Navigate().GoToUrl(Url);

            return true;
        }

        private bool ClickSendEtherMenuItem()
        {
            const string Script =
@"var items = document.getElementsByTagName('a');
for (var i = 0; i < items.length; i++)
{
    var item = items[i];
    if (item.innerText && item.innerText.indexOf('Send Ether & Tokens') != -1)
    {
        item.click();
        break;
    }
}";

            ExecuteScript(Script);

            return true;
        }

        private bool CloseTheWarning()
        {
            const string Script =
@"var items = document.getElementsByTagName('img');
for (var i = 0; i < items.length; i++)
{
    var item = items[i];
    var ngClick = item.getAttribute('ng-click');
    if (ngClick && ngClick.toString() == 'onboardModal.close()')
    {
        console.log('here');
        item.click();
        break;
    }
}";

            ExecuteScript(Script);

            return true;
        }

        public void Dispose()
        {
            if (Driver != null) { Driver.Dispose(); }
        }
    }
}
