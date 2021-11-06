using trade_model;

namespace trade_node_integration
{
    public interface ITradeNodeUtil
    {
        string Ping();
        bool IsOnline();

        string FetchCurrencies(string exchangeName);
        string FetchMarkets(string exchangeName);

        string FetchOrderBook(string exchangeName, TradingPair tradingPair);
        string FetchBalance(string exchangeName);

        string GetNativeOpenOrders(string exchangeName);
        string GetNativeOpenOrders(string exchangeName, TradingPair tradingPair);

        string GetUserTradeHistory(string exchangeName);
        string GetWithdrawalHistory(string exchangeName);
        // fetch-kucoin-deposit-and-withdrawal-history-for-symbol
        string GetKucoinDepositAndWithdrawalHistoryForSymbol(string symbol);

        string GetDepositAddress(string exchangeName, string symbol);

        string CancelOrder(string exchangeName, string orderId);
        string CancelAllOpenOrdersForTradingPair(string exchangeName, TradingPair tradingPair);

        string BuyLimit(string exchange, TradingPair tradingPair, decimal quantity, decimal price);
        string SellLimit(string exchange, TradingPair tradingPair, decimal quantity, decimal price);

        string Withdraw(string exchange, string symbol, decimal quantity, DepositAddress address);

        string FetchQryptosCryptoAccounts();
        string FetchQryptosIndividualAccount(string symbol);
    }
}
