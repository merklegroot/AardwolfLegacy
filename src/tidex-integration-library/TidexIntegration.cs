using config_connection_string_lib;
using log_lib;
using mongo_lib;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using res_util_lib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using tidex_integration_library.Models;
using cache_lib.Models;
using trade_model;
using trade_res;
using web_util;
using trade_lib.Cache;
using cache_lib;

namespace tidex_integration_library
{
    // http://list.wiki/Tidex
    // https://tidex.com/exchange/public-api
    // https://api.tidex.com/api/3/info
    // https://tidex.com/exchange/assets-spec
    public class TidexIntegration : ITidexIntegration
    {
        private static ThrottleContext ThrottleContext = new ThrottleContext
        {
            Locker = new object(),
            ThrottleThreshold = TimeSpan.FromSeconds(10)
        };

        private const string DatabaseName = "tidex";

        private readonly ISimpleWebCache _webCache;
        private readonly IWebUtil _webUtil;
        private readonly IGetConnectionString _getConnectionString;
        private readonly ILogRepo _log;
        private readonly CacheUtil _cacheUtil = new CacheUtil();

        public string Name => "Tidex";
        public Guid Id => new Guid("1F3FC435-05CF-4ABC-B468-FCE4358DB4D6");

        public TidexIntegration(
            IWebUtil webUtil,
            IGetConnectionString getConnectionString,
            ILogRepo log)
        {
            _webUtil = webUtil;
            _getConnectionString = getConnectionString;
            _log = log;

            var collectionContext = new MongoCollectionContext(getConnectionString.GetConnectionString(), DatabaseName, "tidex-web-cache");
            _webCache = new SimpleWebCache(webUtil, collectionContext, "tidex");
        }

        public List<string> GetCoins(CachePolicy cachePolicy = CachePolicy.AllowCache)
        {
            var tradingPairs = GetTradingPairs(cachePolicy);
            var allSymbols = tradingPairs.Select(item => item.Symbol).ToList();
            var allBaseSymbols = tradingPairs.Select(item => item.BaseSymbol).ToList();

            return allSymbols.Union(allBaseSymbols).Distinct().ToList();
        }

        public List<CommodityForExchange> GetCommodities(CachePolicy cachePolicy = CachePolicy.AllowCache)
        {
            var mappedCommodities = ResUtil.Get("standard-commodities.txt", GetType().Assembly)
                .Split('\r')
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Select(item => item.Trim())
                .ToList();

            return GetCoins().Select(symbol =>
            {
                Commodity commodity = null;
                if (mappedCommodities.Any(queryMappedCommoditiy => string.Equals(queryMappedCommoditiy, symbol, StringComparison.InvariantCultureIgnoreCase)))
                {
                    commodity = CommodityRes.BySymbol(symbol);
                }

                return new CommodityForExchange
                {
                    CanonicalId = commodity?.Id,
                    Symbol = symbol,
                    Name = !string.IsNullOrWhiteSpace(commodity?.Name) ? commodity.Name : symbol,
                    NativeName = symbol,
                    NativeSymbol = symbol
                };
            }).ToList();
        }

        private bool ValidateOrderBookContents(string contents)
        {
            var jObject = (JObject)JsonConvert.DeserializeObject(contents);
            var prop = (JProperty)jObject.First;
            var propertyName = prop.Name;
            if (string.Equals(propertyName, "success", StringComparison.InvariantCultureIgnoreCase))
            {
                var successValue = (long)prop.Value;
                if (successValue == 0)
                {
                    return false;
                }
            }

            return true;
        }

        public OrderBook GetOrderBook(TradingPair tradingPair, CachePolicy cachePolicy)
        {
            try
            {
                var orderBook = new OrderBook();

                var url = $"https://api.tidex.com/api/3/depth/{tradingPair.Symbol.Trim().ToLower()}_{tradingPair.BaseSymbol.Trim().ToLower()}";

                // {"waves_btc":{"asks":[[0.00060767,0.9515],[0.0006095,
                var contents = _webCache.Get(url, ValidateOrderBookContents, cachePolicy == CachePolicy.ForceRefresh);
                // {"success":0,"error":"Requests too often"}

                var jObject = (JObject)JsonConvert.DeserializeObject(contents);
                var prop = (JProperty)jObject.First;
                var propertyName = prop.Name;
                if (string.Equals(propertyName, "success", StringComparison.InvariantCultureIgnoreCase))
                {
                    var successValue = (long)prop.Value;
                    if (successValue == 0)
                    {
                        var errorText = new StringBuilder()
                            .AppendLine("Tidex response indicated failure.")
                            .AppendLine("Operation: GetOrderBook()")
                            .AppendLine($"TradingPair: {tradingPair}")
                            .AppendLine($"Url: {url}")
                            .AppendLine($"ResponseContents:")
                            .AppendLine(contents)
                            .ToString();

                        throw new ApplicationException(errorText);
                    }
                }

                var child = prop.First;

                var asks = child["asks"] != null
                    ? child["asks"]
                    : null;

                var bids = child["bids"] != null
                    ? child["bids"]
                    : null;

                orderBook.Asks = new List<Order>();
                if (asks != null)
                {
                    foreach (var orderJson in asks.Children())
                    {
                        var price = orderJson[0].ToObject<decimal>();
                        var quantity = orderJson[1].ToObject<decimal>();

                        orderBook.Asks.Add(new Order { Price = price, Quantity = quantity });
                    }
                }

                orderBook.Bids = new List<Order>();
                if (bids != null)
                {
                    foreach (var orderJson in bids.Children())
                    {
                        var price = orderJson[0].ToObject<decimal>();
                        var quantity = orderJson[1].ToObject<decimal>();

                        orderBook.Bids.Add(new Order { Price = price, Quantity = quantity });
                    }
                }

                return orderBook;
            }
            catch(Exception exception)
            {
                Console.WriteLine(exception);
                throw;
            }
        }

        public List<TradingPair> GetTradingPairs(CachePolicy cachePolicy)
        {
            var nativeInfo = GetTidexNativeInfo(cachePolicy);
            if (nativeInfo?.Pairs == null) { return new List<TradingPair>(); }
            return nativeInfo.Pairs
                .Where(item => !string.IsNullOrWhiteSpace(item.Symbol) && !string.IsNullOrWhiteSpace(item.BaseSymbol))
                .Select(item => new TradingPair(item.Symbol, item.BaseSymbol))                
                .ToList();
        }

        private TidexInfo GetTidexNativeInfo(CachePolicy cachePolicy)
        {
            const string Url = "https://api.tidex.com/api/3/info";

            var translator = new Func<string, TidexInfo>(text =>
            {
                if (string.IsNullOrWhiteSpace(text)) { return null; }
                var info = JsonConvert.DeserializeObject<TidexInfo>(text);
                info.Pairs = new List<TixedPairInfo>();
                if (info.PairsDictionary != null)
                {
                    foreach (var key in info.PairsDictionary.Keys)
                    {
                        if (string.IsNullOrWhiteSpace(key)) { continue; }
                        var pieces = key.Trim().Split('_');
                        string symbol = null;
                        string baseSymbol = null;
                        if (pieces.Count() == 2)
                        {
                            if (!string.IsNullOrWhiteSpace(pieces[0])) { symbol = pieces[0].Trim().ToUpper(); }
                            if (!string.IsNullOrWhiteSpace(pieces[1])) { baseSymbol = pieces[1].Trim().ToUpper(); }
                        }

                        var value = info.PairsDictionary[key];
                        if (value == null) { continue; }
                        var valueText = value.ToString();
                        var pairInfo = JsonConvert.DeserializeObject<TixedPairInfo>(valueText);
                        pairInfo.Symbol = symbol;
                        pairInfo.BaseSymbol = baseSymbol;

                        info.Pairs.Add(pairInfo);
                    }
                }

                return info;
            });

            var validatorWithReason = new Func<string, (bool Result, string Reason)>(text =>
            {
                if (string.IsNullOrWhiteSpace(text))
                {
                    return (false, $"Empty response from \"{Url}\".");
                }

                var translated = translator(text);
                if (translated == null)
                {
                    return (false, $"Failed to parse response from \"{Url}\".");
                }

                if (translated.Success.HasValue && translated.Success.Value == 0)
                {
                    var errorBuilder = new StringBuilder()
                        .AppendLine($"Response from \"{Url}\" indicated failure.");

                    if (!string.IsNullOrWhiteSpace(translated.Error))
                    {
                        errorBuilder.AppendLine(translated.Error.Trim());
                    }

                    return (false, errorBuilder.ToString());
                }

                return (true, null);
            });

            var validator = new Func<string, bool>(text => validatorWithReason(text).Result);

            var retriever = new Func<string>(() =>
            {
                try
                {
                    var text = _webUtil.Get(Url);
                    var validationResult = validatorWithReason(text);
                    if (!validationResult.Result)
                    {
                        var errorBuilder = new StringBuilder().AppendLine($"The response from {Url} failed validation.");

                        if (string.IsNullOrWhiteSpace(validationResult.Reason))
                        {
                            errorBuilder.AppendLine(validationResult.Reason);
                        }

                        throw new ApplicationException(errorBuilder.ToString());
                    }

                    return text;
                }
                catch(Exception exception)
                {
                    _log.Error(exception);
                    throw;
                }
            });

            var collectionContext = new MongoCollectionContext(_getConnectionString.GetConnectionString(), DatabaseName, "tidex--get-info");

            var cacheResult = _cacheUtil.GetCacheableEx(ThrottleContext, retriever, collectionContext, TimeSpan.FromMinutes(5), cachePolicy, validator);
            return translator(cacheResult?.Contents);
        }

        public decimal? GetWithdrawalFee(string symbol, CachePolicy cachePolicy)
        {
            var fees = GetWithdrawalFees(cachePolicy);
            return fees.ContainsKey(symbol)
                ? fees[symbol]
                : (decimal?)null;
        }

        public Dictionary<string, decimal> GetWithdrawalFees(CachePolicy cachePolicy)
        {
            var feeDictionary = new Dictionary<string, decimal>();

            var tradeData = GetTradeData();
            foreach (var currency in tradeData.Currency)
            {
                feeDictionary[currency.Symbol] = currency.WithdrawFee;
            }

            return feeDictionary;
        }

        private TidexTradeData GetTradeData()
        {
            const string Url = "https://web.tidex.com/api/trade-data/";
            var contents = _webCache.Get(Url);
            var tradeData = JsonConvert.DeserializeObject<TidexTradeData>(contents);

            return tradeData;
        }

        public List<DepositAddressWithSymbol> GetDepositAddresses(CachePolicy cachePolicy)
        {
            return new List<DepositAddressWithSymbol>();
        }

        public DepositAddress GetDepositAddress(string symbol, CachePolicy cachePolicy)
        {
            return null;
        }

        public HoldingInfo GetHoldings(CachePolicy cachePolicy)
        {
            return null;
        }
    }
}
