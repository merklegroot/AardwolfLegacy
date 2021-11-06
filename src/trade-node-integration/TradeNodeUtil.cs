using config_client_lib;
using log_lib;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using trade_model;
using web_util;

namespace trade_node_integration
{
    public class TradeNodeUtil : ITradeNodeUtil
    {       
        private readonly IWebUtil _webUtil;
        private readonly IConfigClient _configClient;
        private readonly ILogRepo _log;

        public TradeNodeUtil(
            IConfigClient configClient,
            IWebUtil webUtil,            
            ILogRepo log)
        {
            _webUtil = webUtil;
            _configClient = configClient;
            _log = log;
        }

        public string FetchBalance(string exchangeName)
        {
            var url = $"{UrlBase}get-balance";
            return _webUtil.Post(url, JsonConvert.SerializeObject(new { exchange = exchangeName }));
        }

        public string GetNativeOpenOrders(string exchangeName, TradingPair tradingPair)
        {
            var Url = $"{UrlBase}get-open-orders-for-trading-pair";
            return _webUtil.Post(Url, JsonConvert.SerializeObject(new { exchange = exchangeName, symbol = tradingPair.Symbol, baseSymbol = tradingPair.BaseSymbol }));
        }

        public string GetNativeOpenOrders(string exchangeName)
        {
            var Url = $"{UrlBase}get-all-open-orders";
            return _webUtil.Post(Url, JsonConvert.SerializeObject(new { exchange = exchangeName }));
        }

        public string FetchOrderBook(string exchangeName, TradingPair tradingPair)
        {
            const string Method = "fetch-order-book";
            return CallMethod(Method, exchangeName, new Dictionary<string, object>
            {
                { "symbol", tradingPair.Symbol },
                { "baseSymbol", tradingPair.BaseSymbol },
            });
        }

        public string CancelAllOpenOrdersForTradingPair(string exchangeName, TradingPair tradingPair)
        {
            try
            {
                var url = $"{UrlBase}cancel-all-open-orders-for-trading-pair";

                return _webUtil.Post(url, JsonConvert.SerializeObject(new { exchange = exchangeName, symbol = tradingPair.Symbol, baseSymbol = tradingPair.BaseSymbol }));
            }
            catch (Exception exception)
            {
                _log.Error(exception);
                // throw;
            }

            return null;
        }

        public string BuyLimit(string exchangeName, TradingPair tradingPair, decimal quantity, decimal price)
        {
            return PlaceLimitOrder(true, exchangeName, tradingPair, quantity, price);
        }

        public string SellLimit(string exchangeName, TradingPair tradingPair, decimal quantity, decimal price)
        {
            return PlaceLimitOrder(false, exchangeName, tradingPair, quantity, price);
        }

        public string GetDepositAddress(string exchangeName, string symbol)
        {
            return CallMethod("get-deposit-address", exchangeName, new Dictionary<string, object> { { "symbol", symbol } });
        }

        private string PlaceLimitOrder(bool isBuy, string exchangeName, TradingPair tradingPair, decimal quantity, decimal price)
        {
            var buyOrSellText = isBuy ? "buy" : "sell";
            var url = $"{UrlBase}{buyOrSellText}-limit";
            var data = new
            {
                exchange = exchangeName,
                symbol = tradingPair.Symbol,
                baseSymbol = tradingPair.BaseSymbol,
                quantity = quantity,
                price = price
            };

            var payloadContents = JsonConvert.SerializeObject(data);

            return _webUtil.Post(url, payloadContents);
        }

        public string GetUserTradeHistory(string exchangeName)
        {
            const string ApiMethod = "get-user-trade-history";
            return CallMethod(ApiMethod, exchangeName);
        }

        public string GetWithdrawalHistory(string exchangeName)
        {
            const string ApiMethod = "get-withdrawal-history";
            return CallMethod(ApiMethod, exchangeName);
        }

        public string FetchCurrencies(string exchangeName)
        {
            const string ApiMethod = "fetch-currencies";
            return CallMethod(ApiMethod, exchangeName);
        }

        public string CallMethod(string apiMethod, string exchangeName = null, Dictionary<string, object> additionalData = null)
        {
            var data = new Dictionary<string, object>();
            if (!string.IsNullOrWhiteSpace(exchangeName)) { data["exchange"] = exchangeName; }

            foreach (var key in (additionalData ?? new Dictionary<string, object>()).Keys)
            {
                data[key] = additionalData[key];
            }

            return NodeUtilPost(apiMethod, data);
        }

        private string NodeUtilPost<T>(string apiMethod, T payload)
        {
            var Url = $"{UrlBase}{apiMethod}";
            var payloadContents = JsonConvert.SerializeObject(payload);

            return _webUtil.Post(Url, payloadContents);
        }

        private string NodeUtilGet(string apiMethod)
        {
            var Url = $"{UrlBase}{apiMethod}";
            return _webUtil.Get(Url);
        }

        public string Ping()
        {
            return NodeUtilGet("ping");
        }

        public bool IsOnline()
        {
            try { return string.Equals(Ping(), "pong"); }
            catch (Exception exception)
            {
                _log.Error(exception);
                return false;
            }
        }

        public string Withdraw(string exchange, string symbol, decimal quantity, DepositAddress address)
        {
            var data = new Dictionary<string, object>
            {
                { "symbol",  symbol },
                { "quantity", quantity },
                { "destinationAddress",
                    new Dictionary<string, object>
                    {
                        { "address", address.Address },
                        { "memo", address.Memo }
                    }
                }
            };

            var result = CallMethod("withdraw", exchange, data);

            return result;
        }

        public string FetchMarkets(string exchangeName)
        {
            const string ApiMethod = "fetch-markets";
            return CallMethod(ApiMethod, exchangeName);
        }

        public string FetchQryptosCryptoAccounts() => CallMethod("fetch-qryptos-crypto-accounts");

        public string GetKucoinDepositAndWithdrawalHistoryForSymbol(string symbol) 
            => CallMethod("get-kucoin-deposit-and-withdrawal-history-for-symbol", null, new Dictionary<string, object> { { "symbol", symbol } });

        public string CancelOrder(string exchangeName, string orderId)
        {
            return CallMethod("cancel-order", exchangeName, new Dictionary<string, object> { { "orderId", orderId } });
        }

        public string FetchQryptosIndividualAccount(string symbol)
            => CallMethod("fetch-qryptos-individual-account", null, new Dictionary<string, object> { { "symbol", symbol } });

        private string UrlBase
        {
            get
            {
                // this will normally be "http://localhost:3010/api/";
                var root = (_configClient.GetCcxtUrl() ?? string.Empty).Trim();
                return new Uri(new Uri(root), "api").AbsoluteUri + "/";
            }
        }
    }
}
