using cache_lib;
using cache_lib.Models;
using config_client_lib;
using currency_converter_lib.Models;
using log_lib;
using mongo_lib;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using trade_model;
using web_util;

namespace currency_converter_lib
{
    public interface ICurrencyConverterIntegration
    {
        AsOfWrapper<decimal> GetConversionRate(string symbol, CachePolicy cachePolicy);
    }

    public class CurrencyConverterIntegration : ICurrencyConverterIntegration
    {
        private static object ThrottleLock = new object();
        private ThrottleContext ThrottleContext = new ThrottleContext
        {
            ThrottleThreshold = TimeSpan.FromSeconds(0.25),
            Locker = ThrottleLock
        };

        private TimeSpan ValuationThreshold = TimeSpan.FromMinutes(15);

        private const string DatabaseName = "currency-converter";

        private readonly ICurrencyConverterClient _currencyConverterClient;
        private readonly IConfigClient _configClient;
        private readonly IWebUtil _webUtil;
        private readonly ICacheUtil _cacheUtil;
        private readonly ILogRepo _log;

        public CurrencyConverterIntegration(
            ICurrencyConverterClient currencyConverterClient,
            IConfigClient configClient,
            ICacheUtil cacheUtil,
            IWebUtil webUtil,
            ILogRepo log)
        {
            _currencyConverterClient = currencyConverterClient;
            _configClient = configClient;
            _cacheUtil = cacheUtil;
            _webUtil = webUtil;
            _log = log;
        }

        private static Dictionary<string, string> ForexMap = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase)
        {
            // Russian Ruble
            { "RUR", "RUB" }
        };

        public AsOfWrapper<decimal> GetConversionRate(string symbol, CachePolicy cachePolicy)
        {
            var effetiveCanonicalSymbol = symbol.Trim().ToUpper();
            var nativeSymbol = ForexMap.ContainsKey(effetiveCanonicalSymbol)
                ? ForexMap[effetiveCanonicalSymbol]
                : effetiveCanonicalSymbol;

            var key = $"{nativeSymbol.ToUpper()}_USD";

            var translator = new Func<string, decimal>(text =>
            {
                var model = JsonConvert.DeserializeObject<GetConversionRateResponse>(text);
                if (!model.ContainsKey(key))
                {
                    throw new ApplicationException("Failed to find expected key.");
                }

                var responseData = model[key];
                if (responseData?.Val == null)
                {
                    throw new ApplicationException("Val must not be null.");
                }

                return responseData.Val;
            });

            var validator = new Func<string, bool>(text =>
            {
                if (string.IsNullOrWhiteSpace(text)) { throw new ApplicationException($"Received a null or whitespace response when requesting {nativeSymbol.ToUpper()}-USD conversion rate."); }
                var rate = translator(text);
                if (rate <= 0) { throw new ApplicationException($"Received an invalid rate when requesting {nativeSymbol.ToUpper()}-USD conversion rate."); }

                return true;
            });

            var retriever = new Func<string>(() =>
            {
                try
                {
                    var text = _currencyConverterClient.GetRawForexRate(nativeSymbol);
                    if (!validator(text))
                    {
                        throw new ApplicationException($"Validation failed when requesting {nativeSymbol.ToUpper()}-USD conversion rate.");
                    }
                    return text;
                }
                catch (Exception exception)
                {
                    _log.Error($"An exception was thrown when requesting {nativeSymbol.ToUpper()}-USD conversion rate.{Environment.NewLine}{exception.Message}");
                    _log.Error(exception);
                    throw;
                }
            });
            
            var collectionContext = new MongoCollectionContext(DbContext, "get-conversion-rate");

            var cacheResult = _cacheUtil.GetCacheableEx(ThrottleContext, retriever, collectionContext, ValuationThreshold, cachePolicy, validator, null, key);

            return new AsOfWrapper<decimal>
            {
                AsOfUtc = cacheResult?.AsOf,
                Data = translator(cacheResult.Contents)
            };
        }

        private IMongoDatabaseContext DbContext => new MongoDatabaseContext(_configClient.GetConnectionString(), DatabaseName);
    }
}
