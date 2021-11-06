using config_connection_string_lib;
using mongo_lib;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using cache_lib.Models;
using trade_model;
using web_util;
using cache_lib;

namespace cryptocompare_lib
{
    public class CryptoCompareIntegration : ICryptoCompareIntegration
    {
        private const string DatabaseName = "cryptocompare";

        private static ThrottleContext ThrottleContext = new ThrottleContext
        {
            Locker = new object(),
            ThrottleThreshold = TimeSpan.FromSeconds(1)
        };

        private static TimeSpan CacheThreshold = TimeSpan.FromMinutes(10);

        private readonly IWebUtil _webUtil;

        private readonly CacheUtil _cacheUtil;

        private readonly IGetConnectionString _getConnectionString;

        public CryptoCompareIntegration(
            IWebUtil webUtil,
            IGetConnectionString getConnectionString)
        {
             _webUtil = webUtil;
            _getConnectionString = getConnectionString;

            _cacheUtil = new CacheUtil();
        }

        private static object GetPricesLocker = new object();

        public Dictionary<string, decimal> GetPrices(string symbol, CachePolicy cachePolicy)
        {
            return GetPricesWithAsOf(symbol, cachePolicy).Prices;
        }

        private (Dictionary<string, decimal> Prices, DateTime? AsOfUtc) GetPricesWithAsOf(string symbol, CachePolicy cachePolicy)
        {
            lock (GetPricesLocker)
            {
                if (string.IsNullOrWhiteSpace(symbol)) { throw new ArgumentNullException(nameof(symbol)); }
                var effectiveSymbol = symbol.Trim().ToUpper();

                var symbolsThatArentOnCryptoCompare = new List<string> { "TIG", "BTH" };
                if (symbolsThatArentOnCryptoCompare.Any(item => string.Equals(symbol, item, StringComparison.InvariantCultureIgnoreCase)))
                {
                    return (new Dictionary<string, decimal>(), DateTime.UtcNow);
                }

                var baseSymbols = new List<string> { "USD", "USDT", "ETH", "BTC" };
                var commaSeparatedBaseSymbols = string.Join(",", baseSymbols);

                // https://min-api.cryptocompare.com/data/price?fsym=ETH&tsyms=BTC,USD,EUR
                var url = $"https://min-api.cryptocompare.com/data/price?fsym={effectiveSymbol}&tsyms={commaSeparatedBaseSymbols}";

                var retriever = new Func<string>(() => _webUtil.Get(url));
                var translator = new Func<string, CryptoComparePriceResponse>(text =>
                {
                    if (string.IsNullOrWhiteSpace(text)) { return new CryptoComparePriceResponse(); }

                    var response = new CryptoComparePriceResponse
                    {
                        Prices = new Dictionary<string, decimal>()
                    };

                    var json = (JObject)JsonConvert.DeserializeObject(text);
                    foreach (var item in json.Children())
                    {
                        if (item is JProperty itemProp)
                        {
                            var key = itemProp.Name;
                            if (string.Equals(key, "Response"))
                            {
                                response.Response = itemProp.Value.ToString();
                            }
                            else if (string.Equals(key, "Message"))
                            {
                                response.Message = itemProp.Value.ToString();
                            }
                            else if (string.Equals(key, "Type"))
                            {
                                if (int.TryParse(itemProp.Value.ToString(), out int typeNum))
                                {
                                    response.Type = typeNum;
                                }
                            }
                            else if (string.Equals(key, "Aggregated"))
                            {
                                if (bool.TryParse(itemProp.Value.ToString(), out bool valueBool))
                                {
                                    response.Aggregated = valueBool;
                                }
                            }
                            else if (string.Equals(key, "Data"))
                            {
                                // On error, this has an empty array.
                                // Otherwise, the property doesn't exist.
                                // I'm not sure what to do with this.
                            }
                            else if (decimal.TryParse(itemProp.Value.ToString(), NumberStyles.Float, CultureInfo.CurrentCulture, out decimal valueNum))
                            {
                                response.Prices[key] = valueNum;
                            }
                        }
                    }

                    return response;
                });

                var validator = new Func<string, bool>(text =>
                {
                    if (string.IsNullOrWhiteSpace(text)) { return false; }
                    var response = translator(text);

                    if (response.IsError) { return false; }

                    return true;
                });

                var collectionContext = new MongoCollectionContext(DbContext, $"cryptocompare-get-prices-{effectiveSymbol}");

                var cacheResult = _cacheUtil.GetCacheableEx(ThrottleContext, retriever, collectionContext, CacheThreshold, cachePolicy, validator);

                var resp = translator(cacheResult.Contents);

                if (resp == null) { throw new ApplicationException("Failed to parse CryptoCompare response."); }
                if (resp.IsError)
                {
                    var errorBuilder = new StringBuilder()
                        .AppendLine($"CryptoCompare response indicated failure when attempting to get prices for symbol \"{symbol}\".");

                    if (!string.IsNullOrWhiteSpace(resp.Message))
                    {
                        errorBuilder.AppendLine(resp.Message.Trim());
                    }

                    throw new ApplicationException(errorBuilder.ToString());
                }

                return (resp?.Prices, cacheResult?.AsOf);
            }
        }

        public decimal? GetPrice(string symbol, string baseSymbol, CachePolicy cachePolicy)
        {
            // TODO:
            // If GetPrices isn't setup to use our base symbol, we'd end up getting back null even if that base symbol is valid.
            // Enhance this so that it actually calls crypto compare with the passed in base symbol.
            var prices = GetPrices(symbol, cachePolicy);

            return prices != null && prices.ContainsKey(baseSymbol)
                ? prices[baseSymbol]
                : (decimal?)null;
        }

        internal class CryptoComparePriceResponse
        {
            public bool IsError
            {
                get
                {
                    return Response != null && string.Equals(Response.Trim(), "Error", StringComparison.InvariantCultureIgnoreCase);
                }
            }

            public string Response { get; set; }
            public string Message { get; set; }
            public int? Type { get; set; }
            public bool? Aggregated { get; set; }

            public Dictionary<string, decimal> Prices { get; set; }
        }

        public decimal? GetUsdValue(string symbol, CachePolicy cachePolicy)
        {
            if (string.IsNullOrWhiteSpace(symbol)) { throw new ArgumentNullException(nameof(symbol)); }
            var effectiveSymbol = symbol.Trim().ToUpper();

            if (string.Equals(effectiveSymbol, "USD", StringComparison.InvariantCultureIgnoreCase)
                || string.Equals(effectiveSymbol, "TUSD", StringComparison.InvariantCultureIgnoreCase))
                //|| string.Equals(effectiveSymbol, "USDT", StringComparison.InvariantCultureIgnoreCase))
            {
                return 1;
            }

            if (string.Equals(effectiveSymbol, "BTC", StringComparison.InvariantCultureIgnoreCase)
                || string.Equals(effectiveSymbol, "BTC", StringComparison.InvariantCultureIgnoreCase))
            {
                var btcPrices = GetPrices("BTC", cachePolicy);
                if (btcPrices.ContainsKey("USD")) { return btcPrices["USD"]; }
                return null;
            }

            if (string.Equals(effectiveSymbol, "ETH", StringComparison.InvariantCultureIgnoreCase)
                || string.Equals(effectiveSymbol, "ETH", StringComparison.InvariantCultureIgnoreCase))
            {
                var btcPrices = GetPrices("ETH", cachePolicy);
                if (btcPrices.ContainsKey("USD")) { return btcPrices["USD"]; }
                return null;
            }

            var dict = GetPrices(effectiveSymbol, cachePolicy);
            if (dict == null || !dict.Keys.Any()) { return null; }

            if (dict.ContainsKey("USD")) { return dict["USD"]; }
            if (dict.ContainsKey("USDT")) { return dict["USDT"]; }
            if (dict.ContainsKey("BTC")) {
                var itemToBtc = dict["BTC"];
                var btcToUsd = GetUsdValue("BTC", cachePolicy);
                if (btcToUsd.HasValue)
                {
                    return itemToBtc * btcToUsd.Value;
                }
            }

            if (dict.ContainsKey("ETH"))
            {
                var itemToBtc = dict["ETH"];
                var ethToUsd = GetUsdValue("ETH", cachePolicy);
                if (ethToUsd.HasValue)
                {
                    return itemToBtc * ethToUsd.Value;
                }
            }

            return null;
        }

        public (decimal? UsdValue, DateTime? AsOfUtc) GetUsdValueV2(string symbol, CachePolicy cachePolicy)
        {
            if (string.IsNullOrWhiteSpace(symbol)) { throw new ArgumentNullException(nameof(symbol)); }
            var effectiveSymbol = symbol.Trim().ToUpper();

            if (string.Equals(effectiveSymbol, "USD", StringComparison.InvariantCultureIgnoreCase)
                || string.Equals(effectiveSymbol, "TUSD", StringComparison.InvariantCultureIgnoreCase))
            {
                return (1, DateTime.UtcNow);
            }

            if (string.Equals(effectiveSymbol, "BTC", StringComparison.InvariantCultureIgnoreCase)
                || string.Equals(effectiveSymbol, "BTC", StringComparison.InvariantCultureIgnoreCase))
            {
                var wrappedBtcPrices = GetPricesWithAsOf("BTC", cachePolicy);
                var btcPrices = wrappedBtcPrices.Prices;
                if (btcPrices.ContainsKey("USD")) { return (btcPrices["USD"], wrappedBtcPrices.AsOfUtc); }
                return (null, wrappedBtcPrices.AsOfUtc);
            }

            if (string.Equals(effectiveSymbol, "ETH", StringComparison.InvariantCultureIgnoreCase)
                || string.Equals(effectiveSymbol, "ETH", StringComparison.InvariantCultureIgnoreCase))
            {
                var wrappedEthPrices = GetPricesWithAsOf("ETH", cachePolicy);
                var ethPrices = wrappedEthPrices.Prices;
                if (ethPrices.ContainsKey("USD")) { return (ethPrices["USD"], wrappedEthPrices.AsOfUtc); }
                return (null, wrappedEthPrices.AsOfUtc);
            }

            var wrappedPrices = GetPricesWithAsOf(effectiveSymbol, cachePolicy);
            if (wrappedPrices.Prices == null || !wrappedPrices.Prices.Keys.Any()) { return (null, wrappedPrices.AsOfUtc); }

            var dict = wrappedPrices.Prices;
            if (dict.ContainsKey("USD")) { return (dict["USD"], wrappedPrices.AsOfUtc); }
            if (dict.ContainsKey("USDT")) { return (dict["USDT"], wrappedPrices.AsOfUtc); }
            if (dict.ContainsKey("BTC"))
            {
                var itemToBtc = dict["BTC"];
                var btcToUsd = GetUsdValue("BTC", cachePolicy);
                if (btcToUsd.HasValue)
                {
                    return (itemToBtc * btcToUsd.Value, wrappedPrices.AsOfUtc);
                }
            }

            if (dict.ContainsKey("ETH"))
            {
                var itemToBtc = dict["ETH"];
                var ethToUsd = GetUsdValue("ETH", cachePolicy);
                if (ethToUsd.HasValue)
                {
                    return (itemToBtc * ethToUsd.Value, wrappedPrices.AsOfUtc);
                }
            }

            return (null, wrappedPrices.AsOfUtc);
        }

        public decimal GetEthToBtcRatio(CachePolicy cachePolicy)
        {
            var dictionary = GetPrices("ETH", cachePolicy);
            if (!dictionary.ContainsKey("BTC")) { throw new ApplicationException("Failed to get ETH to BTC ratio."); }
            return dictionary["BTC"];
        }

        private IMongoDatabaseContext DbContext
        {
            get { return new MongoDatabaseContext(_getConnectionString.GetConnectionString(), DatabaseName); }
        }
    }
}
