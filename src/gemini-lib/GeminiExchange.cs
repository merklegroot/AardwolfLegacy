using System;
using System.Collections.Generic;
using System.Linq;
using cache_lib;
using cache_lib.Models;
using config_client_lib;
using gemini_lib.Client;
using gemini_lib.Map;
using gemini_lib.Models;
using log_lib;
using mongo_lib;
using Newtonsoft.Json;
using trade_lib;
using trade_model;

namespace gemini_lib
{
    public interface IGeminiExchange : ITradeIntegration { }

    public class GeminiExchange : IGeminiExchange
    {
        public Guid Id => new Guid("6AFD65B8-7E59-40AA-B1EB-8AA090ED6EC1");
        public string Name => "Gemini";
        private const string DatabaseName = "gemini";
        private static TimeSpan TradingPairsThreshold = TimeSpan.FromMinutes(20);

        private static readonly GeminiMap _geminiMap = new GeminiMap();

        private static readonly ThrottleContext ThrottleContext = new ThrottleContext
        {
            Locker = new object(),
            ThrottleThreshold = TimeSpan.FromSeconds(1)
        };

        private readonly IConfigClient _configClient;
        private readonly IGeminiClient _geminiClient;
        private readonly ICacheUtil _cacheUtil;
        private readonly ILogRepo _log;

        public GeminiExchange(
            IConfigClient configClient,
            IGeminiClient geminiClient,
            ICacheUtil cacheUtil,
            ILogRepo log)
        {
            _configClient = configClient;
            _geminiClient = geminiClient;
            _cacheUtil = cacheUtil;
            _log = log;
        }

        public List<CommodityForExchange> GetCommodities(CachePolicy cachePolicy)
        {
            var nativeTradingPairs = GetNativeTradingPairs(cachePolicy);
            var nativeSymbols = new List<string>();

            foreach (var combo in (nativeTradingPairs?.Data ?? new List<string>()))
            {
                if (string.IsNullOrWhiteSpace(combo)) { continue; }
                var trimmedCombo = combo.Trim();
                if (trimmedCombo.Length != 6) { continue; }

                var nativeSymbol = trimmedCombo.Substring(0, 3);
                var nativeBaseSymbol = trimmedCombo.Substring(nativeSymbol.Length, trimmedCombo.Length - nativeSymbol.Length);

                nativeSymbols.Add(nativeSymbol.ToUpper());
                nativeSymbols.Add(nativeBaseSymbol.ToUpper());
            }

            nativeSymbols = nativeSymbols.Distinct().ToList();

            return nativeSymbols
                .Select(nativeSymbol =>
                {                    
                    var canon = _geminiMap.GetCanon(nativeSymbol);

                    return new CommodityForExchange
                    {
                        Symbol = !string.IsNullOrWhiteSpace(canon?.Symbol) ? canon.Symbol : nativeSymbol,
                        NativeSymbol = nativeSymbol,
                        Name = !string.IsNullOrWhiteSpace(canon?.Name) ? canon.Name : nativeSymbol,
                        NativeName = nativeSymbol
                    };
                })
                .Where(item => item != null)
                .ToList();
        }

        private AsOfWrapper<List<string>> GetNativeTradingPairs(CachePolicy cachePolicy)
        {
            var translator = new Func<string, List<string>>(text =>
                !string.IsNullOrWhiteSpace(text)
                    ? JsonConvert.DeserializeObject<List<string>>(text)
                    : null);

            var validator = new Func<string, bool>(text =>
            {
                if (string.IsNullOrWhiteSpace(text)) { throw new ApplicationException($"Received a null or whitespace response from {Name} when requesting symbols list."); }
                translator(text);

                return true;
            });

            var retriever = new Func<string>(() =>
            {
                try
                {
                    var responseText = _geminiClient.GetSymbols();
                    if (!validator(responseText)) { throw new ApplicationException($"Commodities list response from {Name} failed validation."); }

                    return responseText;
                }
                catch (Exception exception)
                {
                    _log.Error($"And exception was thrown when requesting commodities from {Name}.{Environment.NewLine}{exception.Message}");
                    _log.Error(exception);
                    throw;
                }
            });

            var collectionContext = new MongoCollectionContext(DbContext, "gemini--get-symbols");
            var cacheResult = _cacheUtil.GetCacheableEx(ThrottleContext, retriever, collectionContext, TradingPairsThreshold, cachePolicy, validator);

            return new AsOfWrapper<List<string>>
            {
                AsOfUtc = cacheResult?.AsOf,
                Data = translator(cacheResult?.Contents)
            };
        }

        public DepositAddress GetDepositAddress(string symbol, CachePolicy cachePolicy)
        {
            throw new NotImplementedException();
        }

        public List<DepositAddressWithSymbol> GetDepositAddresses(CachePolicy cachePolicy)
        {
            throw new NotImplementedException();
        }

        public HoldingInfo GetHoldings(CachePolicy cachePolicy)
        {
            throw new NotImplementedException();
        }

        public OrderBook GetOrderBook(TradingPair tradingPair, CachePolicy cachePolicy)
        {
            
            throw new NotImplementedException();
        }

        private AsOfWrapper<GeminiOrderBook> GetNativeOrderBook(string nativeSymbol, string nativeBaseSymbol, CachePolicy cachePolicy)
        {
            var contents = _geminiClient.GetOrderBook(nativeSymbol, nativeBaseSymbol);
            throw new NotImplementedException();
        }

        public List<TradingPair> GetTradingPairs(CachePolicy cachePolicy)
        {
            var nativeTradingPairs = GetNativeTradingPairs(cachePolicy);
            var nativeSymbols = new List<string>();

            var tradingPairs = new List<TradingPair>();
            foreach (var combo in (nativeTradingPairs?.Data ?? new List<string>()))
            {
                if (string.IsNullOrWhiteSpace(combo)) { return null; }
                var trimmedCombo = combo.Trim();
                if (trimmedCombo.Length != 6) { return null; }

                var nativeSymbol = trimmedCombo.Substring(0, 3);
                var nativeBaseSymbol = trimmedCombo.Substring(nativeSymbol.Length, trimmedCombo.Length - nativeSymbol.Length);

                // nativeSymbols.Add(nativeSymbol);
                //nativeSymbols.Add(nativeBaseSymbol);

                var tradingPair = new TradingPair
                {
                    Symbol = nativeSymbol.ToUpper(),
                    NativeSymbol = nativeSymbol.ToUpper(),
                    BaseSymbol = nativeBaseSymbol.ToUpper(),
                    NativeBaseSymbol = nativeBaseSymbol.ToUpper()
                };

                tradingPairs.Add(tradingPair);
            }

            return tradingPairs;

            //return (nativeTradingPairs?.Data ?? new List<string>())
            //    .Select(combo =>
            //    {
            //        if (string.IsNullOrWhiteSpace(combo)) { return null; }
            //        var trimmedCombo = combo.Trim();
            //        if (trimmedCombo.Length != 6) { return null; }

            //        var nativeSymbol = trimmedCombo.Substring(0, 3);
            //        var nativeBaseSymbol = trimmedCombo.Substring(nativeSymbol.Length, trimmedCombo.Length - nativeSymbol.Length);

            //        var canon = _geminiMap.GetCanon(nativeSymbol);
            //        var baseCanon = _geminiMap.GetCanon(nativeBaseSymbol);

            //        return new CommodityForExchange
            //        {
            //            Symbol = !string.IsNullOrWhiteSpace(canon?.Symbol) ? canon.Symbol : nativeSymbol,
            //            NativeSymbol = nativeSymbol,
            //            Name = !string.IsNullOrWhiteSpace(canon.Name) ? canon.Name : nativeSymbol,
            //            NativeName = nativeSymbol
            //        };
            //    })
            //    .Where(item => item != null)
            //    .ToList();

            // throw new NotImplementedException();
        }

        public decimal? GetWithdrawalFee(string symbol, CachePolicy cachePolicy)
        {
            throw new NotImplementedException();
        }

        public Dictionary<string, decimal> GetWithdrawalFees(CachePolicy cachePolicy)
        {
            throw new NotImplementedException();
        }

        private IMongoDatabaseContext DbContext
        {
            get { return new MongoDatabaseContext(_configClient.GetConnectionString(), DatabaseName); }
        }
    }
}
