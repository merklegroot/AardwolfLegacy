//using iridium_lib.Models;
//using System.Collections.Generic;
//using trade_contracts;

//namespace iridium_lib
//{
//    public class ExchangeClient : IExchangeClient
//    {
//        private readonly IServiceInvoker _serviceInvoker;

//        public ExchangeClient(IServiceInvoker serviceInvoker)
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

//        public List<TradingPairContract> GetTradingPairsForExchange(string exchange, bool forceRefresh)
//        {
//            var payload = new { exchange = exchange, forceRefresh = forceRefresh };
//            const string ApiMethod = "get-trading-pairs-for-exchange";

//            return _serviceInvoker.CallApi<List<TradingPairContract>>(ApiMethod, payload);
//        }

//        public List<ExchangeContract> GetExchanges()
//        {
//            const string ApiMethod = "get-exchanges";
//            return _serviceInvoker.CallApi<List<ExchangeContract>>(ApiMethod);
//        }

//        public List<string> GetCryptoCompareSymbols()
//        {
//            return _serviceInvoker.CallApi<List<string>>("get-cryptocompare-symbols");
//            // return ResUtil.Get<List<string>>("cryptocompare-symbols.json", typeof(TradeResDummy).Assembly).Distinct().ToList();
//        }
//    }
//}
