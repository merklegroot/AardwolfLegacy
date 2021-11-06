//using iridium_lib.Models;
//using Newtonsoft.Json;
//using System.Collections.Generic;
//using trade_contracts;
//using web_util;

//namespace iridium_lib
//{
//    public class IridiumIntegration : IIridiumIntegration
//    {
//        private readonly IServiceInvoker _serviceInvoker;

//        public IridiumIntegration(IServiceInvoker serviceInvoker)
//        {
//            _serviceInvoker = serviceInvoker;
//        }

//        public DepositAddressContract GetDepositAddress(string exchange, string symbol)
//        {
//            var payload = new { exchange = exchange, symbol = symbol, forceRefresh = false };
//            const string ApiMethod = "get-deposit-address";
//            return _serviceInvoker.CallApi<DepositAddressContract>(ApiMethod, payload);
//        }

//        public List<HistoryItemContract> GetExchangeHistory(string exchange)
//        {
//            const string ApiMethod = "get-history-for-exchange";
//            var payload = new ExchangeServiceModel { Exchange = exchange };

//            return _serviceInvoker.CallApi<List<HistoryItemContract>>(ApiMethod, payload);
//        }

//        public List<ExchangeCommodityContract> GetCommoditiesForExchange(string exchange)
//        {
//            const string ApiMethod = "get-commodities-for-exchange";
//            var payload = new ExchangeServiceModel { Exchange = exchange };

//            return _serviceInvoker.CallApi<List<ExchangeCommodityContract>>(ApiMethod, payload);
//        }

//        public ExchangeCommodityContract GetCommoditiyForExchange(string exchange, string symbol)
//        {
//            const string ApiMethod = "get-commodity-for-exchange";
//            var payload = new { exchange = exchange, nativeSymbol = symbol };
            
//            return _serviceInvoker.CallApi<ExchangeCommodityContract>(ApiMethod, payload);
//        }
//    }
//}
