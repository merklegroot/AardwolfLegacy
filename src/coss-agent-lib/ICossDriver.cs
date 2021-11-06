using cache_lib.Models;
using coss_agent_lib.Models;
using coss_data_model;
using OpenQA.Selenium;
using sel_lib;
using System;
using System.Collections.Generic;
using trade_browser_lib.Models;
using trade_model;

namespace coss_agent_lib
{
    public interface ICossDriver
    {
        void Init(IRemoteWebDriver driver);

        bool CheckSession();
        void Login();

        void LoginIfNecessary();
        bool CheckWallet();

        IWebElement WaitForElement(Func<IWebElement> method);
        bool SetInputTextAndVerify(IWebElement input, string text);
        bool SetInputNumberAndVerify(IWebElement input, decimal num);
        string GetInputText(IWebElement input);
        bool Attempt(Func<bool> method, int maxAttempts);
        void Sleep(TimeSpan timeSpan);
        void Sleep(int milliseconds);
        
        void Navigate(string url);
        void NavigateAndVerify(string url, Func<bool> verifier);
        void NavigateToExchange(TradingPair tradingPair);
        void NavigateToExchange(string symbol, string baseSymbol);
        void NavigateToDashboard();

        void CancelAllForTradingPair(TradingPair tradingPair, OrderType? orderType = null);
        // void CancelOrderOnCurrentPage(decimal price, decimal quantity);
        void CancelOrder(string orderId, string symbol, string baseSymbol);
        List<OpenOrderEx> RefreshOpenOrders(TradingPair tradingPair);
        List<OpenOrderEx> GetOpenOrders();
        List<OpenOrderForTradingPair> GetOpenOrdersForTradingPair(string symbol, string baseSymbol);

        bool ClickPerformBuyButton();
        bool ClickSellToggleButton();

        bool PlaceOrder(
            TradingPair tradingPair,
            OrderType orderType,
            QuantityAndPrice quantityAndPrice,
            bool alreadyOnPage = false);

        void ExecuteScript(string script);

        string PerformRequest(string url, string method = "GET");

        CossSessionState SessionState { get; }
    }
}
