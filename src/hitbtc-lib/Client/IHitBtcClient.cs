using hitbtc_lib.Client.ClientModels;
using System.Collections.Generic;
using trade_model;

namespace hitbtc_lib.Client
{
    public interface IHitBtcClient
    {
        string GetSymbols();
        string GetCurrenciesRaw();

        string GetDepositAddress(ApiKey apiKey, string nativeSymbol);
        string GetOpenOrdersRaw(ApiKey apiKey);
        string CancelOrderRaw(ApiKey apiKey, string orderId);
        string BuyLimitRaw(ApiKey apiKey, string tradingPairSymbol, decimal quantity, decimal price);
        string SellLimitRaw(ApiKey apiKey, string tradingPairSymbol, decimal quantity, decimal price);

        string GetTradeHistoryRaw(ApiKey apiKey);
        List<HitBtcApiTradeHistoryItem> GetTradeHistory(ApiKey apiKey);

        string GetTransactionsHistoryRaw(ApiKey apiKey);

        List<HitBtcClientTransactionItem> GetTransactionsHistory(ApiKey apiKey);

        string AuthenticatedRequest(ApiKey apiKey, string url, string verb = "GET", string payload = null);
    }
}
