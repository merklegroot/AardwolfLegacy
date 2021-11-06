using coss_api_client_lib.Models;
using trade_model;

namespace coss_api_client_lib
{
    public interface ICossApiClient
    {
        string GetExchangeInfoRaw();

        string GetMarketSummariesRaw(string symbol, string baseSymbol);

        string GetOrderBookRaw(string symbol, string baseSymbol);

        string GetBalanceRaw(ApiKey apiKey);

        string GetOpenOrdersRaw(ApiKey apiKey, string symbol, string baseSymbol);
        CossApiGetOpenOrdersResponseMessage GetOpenOrders(ApiKey apiKey, string symbol, string baseSymbol);

        string CreateOrderRaw(ApiKey apiKey, string symbol, string baseSymbol, decimal price, decimal quantity, bool isBid);

        CreateApiOrderResponseMessage CreateOrder(ApiKey apiKey, string symbol, string baseSymbol, decimal price, decimal quantity, bool isBid);

        string CancelOrderRaw(ApiKey apiKey, string nativeSymbol, string nativeBaseSymbol, string orderId);

        string GetWebCoinsRaw();

        CossApiGetCompletedOrdersResponse GetCompletedOrders(ApiKey apiKey, string symbol, string baseSymbol, int? limit = null, int? page = null);
        string GetCompletedOrdersRaw(ApiKey apiKey, string symbol, string baseSymbol, int? limit = null, int? page = null);        

        string GetServerTimeRaw();

        void SynchronizeTime();
    }
}
