using cache_lib;
using cache_lib.Models;
using config_client_lib;
using log_lib;
using mongo_lib;
using Newtonsoft.Json;
using oex_lib.Client;
using oex_lib.Models;
using qryptos_lib.Res;
using System;
using System.Collections.Generic;
using System.Linq;
using trade_constants;
using trade_lib;
using trade_model;

namespace oex_lib
{
    public interface IOexExchange : ITradeIntegration
    {
    }

    public class OexExchange : IOexExchange
    {
        private static readonly ThrottleContext ThrottleContext = new ThrottleContext
        {
            Locker = new object(),
            ThrottleThreshold = TimeSpan.FromSeconds(1)
        };

        private static readonly TimeSpan TradingPairsThreshold = TimeSpan.FromMinutes(45);
        private static readonly TimeSpan OrderBookThreshold = TimeSpan.FromMinutes(20);

        public Guid Id => new Guid("A4B3DD54-F12A-4718-B517-CE28DADCD326");
        public string Name => IntegrationNameRes.Oex;

        private const string DatabaseName = "oex";

        private readonly IConfigClient _configClient;
        private readonly IOexClient _oexClient;
        private readonly ICacheUtil _cacheUtil;
        private readonly ILogRepo _log;

        private readonly OexMap _oexMap = new OexMap();

        public OexExchange(
            IConfigClient configClient,
            IOexClient oexClient,
            ICacheUtil cacheUtil,
            ILogRepo log)
        {
            _configClient = configClient;
            _oexClient = oexClient;
            _cacheUtil = cacheUtil;
            _log = log;
        }

        public List<CommodityForExchange> GetCommodities(CachePolicy cachePolicy)
        {
            var nativePairsResponse = GetNativeTradingPairs(cachePolicy);
            if (nativePairsResponse?.Data == null) { return new List<CommodityForExchange>(); }

            var allSymbols = new List<string>();
            allSymbols.AddRange(nativePairsResponse.Data.Select(tp => tp.Symbol));
            allSymbols.AddRange(nativePairsResponse.Data.Select(tp => tp.BaseSymbol));

            var distinctSymbols = allSymbols.Distinct().ToList();

            return distinctSymbols.Select(nativeSymbol =>
            {
                var canon = _oexMap.GetCanon(nativeSymbol);

                return new CommodityForExchange
                {
                    CanonicalId = canon?.Id,
                    Symbol = !string.IsNullOrWhiteSpace(canon?.Symbol) ? canon.Symbol : nativeSymbol,
                    Name = nativeSymbol,

                    NativeSymbol = nativeSymbol,
                    NativeName = !string.IsNullOrWhiteSpace(canon?.Name) ? canon.Name : nativeSymbol,
                };
            }).ToList();
        }

        public DepositAddress GetDepositAddress(string symbol, CachePolicy cachePolicy)
        {
            return null;
        }

        public List<DepositAddressWithSymbol> GetDepositAddresses(CachePolicy cachePolicy)
        {
            return new List<DepositAddressWithSymbol>();
        }

        public HoldingInfo GetHoldings(CachePolicy cachePolicy)
        {
            return new HoldingInfo
            {
                TimeStampUtc = DateTime.UtcNow
            };
        }

        public OrderBook GetOrderBook(TradingPair tradingPair, CachePolicy cachePolicy)
        {
            var tradingPairId = GetTradingPairId(tradingPair.Symbol, tradingPair.BaseSymbol, cachePolicy);
            if (!tradingPairId.HasValue) { throw new ApplicationException($"Could not determine the Oex trading pair id for trading pair {tradingPair.Symbol}-{tradingPair.BaseSymbol}"); }

            var native = GetNativeOrderBook(tradingPairId.Value, cachePolicy);

            return new OrderBook
            {
                AsOf = native?.AsOfUtc,
                Asks = (native?.Data?.Data?.Sells ?? new List<OexOrder>()).Select(queryOrder => new Order
                {
                    Price = queryOrder.Price,
                    Quantity = queryOrder.Amount
                }).ToList(),
                Bids = (native?.Data?.Data?.Buys ?? new List<OexOrder>()).Select(queryOrder => new Order
                {
                    Price = queryOrder.Price,
                    Quantity = queryOrder.Amount
                }).ToList()
            };
        }

        public AsOfWrapper<OexGetOrderBookResponse> GetNativeOrderBook(int tradingPairId, CachePolicy cachePolicy)
        {
            var translator = new Func<string, OexGetOrderBookResponse>(text =>
                !string.IsNullOrWhiteSpace(text)
                    ? JsonConvert.DeserializeObject<OexGetOrderBookResponse>(text)
                    : null);

            var validator = new Func<string, bool>(text =>
            {
                if (string.IsNullOrWhiteSpace(text)) { throw new ApplicationException($"Received a null or whitespace response when requesting order book for {Name} trading pair with id {tradingPairId}."); }
                var translated = translator(text);

                const int ExpectedCode = 200;
                if (translated.Code != ExpectedCode)
                {
                    throw new ApplicationException($"Received a code of {translated.Code} when requesting order book for {Name} trading pair with id {tradingPairId} but expected code {ExpectedCode}.");
                }

                return true;
            });

            var retriever = new Func<string>(() =>
            {
                try
                {
                    var contents = _oexClient.GetOrderBookRaw(tradingPairId);
                    if (!validator(contents)) { throw new ApplicationException("Response failed validation when requesting order book for {Name} trading pair with id {tradingPairId}."); }                   

                    return contents;
                }
                catch(Exception exception)
                {
                    _log.Error($"An exception occurred when requesting order book for {Name} trading pair with id {tradingPairId}.{Environment.NewLine}{exception.Message}");
                    _log.Error(exception);
                    throw;
                }
            });

            var key = tradingPairId.ToString();
            var cacheResult = _cacheUtil.GetCacheableEx(ThrottleContext, retriever, OrderBookContext, OrderBookThreshold, cachePolicy, validator, null, key);

            return new AsOfWrapper<OexGetOrderBookResponse>
            {
                AsOfUtc = cacheResult?.AsOf,
                Data = translator(cacheResult.Contents)
            };
        }

        public List<TradingPair> GetTradingPairs(CachePolicy cachePolicy)
        {
            var oexTradingPairs = GetNativeTradingPairs(cachePolicy);
            if (oexTradingPairs?.Data == null) { return new List<TradingPair>(); }

            return oexTradingPairs.Data.Select(nativeTradingPair =>
            {
                var nativeSymbol = nativeTradingPair.Symbol;
                var canon = _oexMap.GetCanon(nativeSymbol);

                var nativeBaseSymbol = nativeTradingPair.BaseSymbol;                
                var baseCanon = _oexMap.GetCanon(nativeBaseSymbol);

                return new TradingPair
                {
                    Symbol = !string.IsNullOrWhiteSpace(canon?.Symbol)
                        ? canon.Symbol
                        : nativeSymbol,

                    NativeSymbol = nativeSymbol,

                    CommodityName = !string.IsNullOrWhiteSpace(canon?.Name)
                        ? canon.Name
                        : nativeSymbol,

                    NativeCommodityName = nativeSymbol,

                    BaseSymbol = !string.IsNullOrWhiteSpace(baseCanon?.Symbol)
                        ? baseCanon.Symbol
                        : nativeBaseSymbol,

                    NativeBaseSymbol = nativeBaseSymbol,

                    BaseCommodityName = !string.IsNullOrWhiteSpace(baseCanon?.Name)
                        ? baseCanon.Name
                        : nativeBaseSymbol,

                    NativeBaseCommodityName = nativeBaseSymbol
                };
            }).ToList();
        }

        private AsOfWrapper<List<OexTradingPair>> GetNativeTradingPairs(CachePolicy cachePolicy)
        {
            var translator = new Func<string, List<OexTradingPair>>(text =>
                !string.IsNullOrWhiteSpace(text)
                    ? ParseTradingPairs(text)
                    : new List<OexTradingPair>());

            var validator = new Func<string, bool>(text =>
            {
                if (string.IsNullOrWhiteSpace(text)) { throw new ApplicationException($"Received a null or whitespace response when requesting {Name} trading pairs."); }
                var translated = translator(text);
                if (translated == null || !translated.Any())
                {
                    throw new ApplicationException($"Failed to parse {Name} trading pairs.");
                }

                return true;
            });

            var retriever = new Func<string>(() =>
            {
                try
                {
                    var contents = _oexClient.GetTradeMarketSource();
                    if (!validator(contents))
                    {
                        throw new ApplicationException($"Response failed valiation when attempting to get {Name} trading pairs.");
                    }

                    return contents;
                }
                catch (Exception exception)
                {
                    _log.Error($"An exception occured when attempting to retrieve {Name} trading pairs.{Environment.NewLine}{exception.Message}");
                    _log.Error(exception);
                    throw;
                }
            });

            var cacheResult = _cacheUtil.GetCacheableEx(ThrottleContext, retriever, MarketSourceContext, TradingPairsThreshold, cachePolicy, validator);

            return new AsOfWrapper<List<OexTradingPair>>
            {
                AsOfUtc = cacheResult?.AsOf,
                Data = translator(cacheResult?.Contents)
            };
        }

        public decimal? GetWithdrawalFee(string symbol, CachePolicy cachePolicy)
        {
            return null;
        }

        public Dictionary<string, decimal> GetWithdrawalFees(CachePolicy cachePolicy)
        {
            return new Dictionary<string, decimal>(StringComparer.InvariantCultureIgnoreCase);
        }

        private int? GetTradingPairId(string symbol, string baseSymbol, CachePolicy cachePolicy)
        {
            var nativeTradingPairs = GetNativeTradingPairs(CachePolicy.OnlyUseCache);
            var match = nativeTradingPairs.Data.SingleOrDefault(tp => string.Equals(tp.Symbol, symbol, StringComparison.InvariantCultureIgnoreCase)
                && string.Equals(tp.BaseSymbol, baseSymbol, StringComparison.InvariantCultureIgnoreCase));

            if (match == null && cachePolicy != CachePolicy.OnlyUseCache)
            {
                nativeTradingPairs = GetNativeTradingPairs(cachePolicy);
                match = nativeTradingPairs.Data.SingleOrDefault(tp => string.Equals(tp.Symbol, symbol, StringComparison.InvariantCultureIgnoreCase)
                    && string.Equals(tp.BaseSymbol, baseSymbol, StringComparison.InvariantCultureIgnoreCase));
            }

            return match?.Id;
        }

        private List<OexTradingPair> ParseTradingPairs(string contents)
        {
            var pairs = new List<OexTradingPair>();
            const int MaxIterations = 1000;

            var initialPos = 0;
            for (var i = 0; i < MaxIterations; i++)
            {
                var openTagStartPos = contents.IndexOf("<a ", initialPos);
                if (openTagStartPos < 0) { break; }
                var openTagEndPos = contents.IndexOf(">", openTagStartPos + 1);
                if (openTagEndPos < 0) { break; }
                var startTagContents = contents.Substring(openTagStartPos, openTagEndPos - openTagStartPos + 1);

                var closeTagStartPos = contents.IndexOf("</a", openTagEndPos + 1);
                if (closeTagStartPos < 0) { break; }
                var closeTagEndPos = contents.IndexOf(">", closeTagStartPos + 1);
                if (closeTagEndPos < 0) { break; }
                initialPos = closeTagEndPos + 1;

                var closeTagContents = contents.Substring(closeTagStartPos, closeTagEndPos - closeTagStartPos + 1);

                var tagGutsStartPos = openTagStartPos + startTagContents.Length;
                var tagGutsLength = closeTagStartPos - tagGutsStartPos;
                var tagGuts = contents.Substring(tagGutsStartPos, tagGutsLength);

                var href = GetBetweenMarkers(startTagContents, "href=\"", "\"");
                var tradingPairId = GetTradingPairId(href);

                if (!tradingPairId.HasValue) { continue; }

                string symbol = null;
                string baseSymbol = null;
                if (!string.IsNullOrWhiteSpace(tagGuts))
                {
                    var tagPieces = tagGuts.Split('/');
                    if (tagPieces.Length == 2)
                    {
                        if (!string.IsNullOrWhiteSpace(tagPieces[0])
                            && !string.IsNullOrWhiteSpace(tagPieces[1]))
                        {
                            symbol = tagPieces[0].Trim();
                            baseSymbol = tagPieces[1].Trim();
                        }
                    }
                }

                var pair = new OexTradingPair
                {
                    Id = tradingPairId.Value,
                    Symbol = symbol,
                    BaseSymbol = baseSymbol
                };

                pairs.Add(pair);
            }

            return pairs;
        }

        private int? GetTradingPairId(string href)
        {
            // /trademarket.html?symbol=21
            if (string.IsNullOrWhiteSpace(href)) { return null; }
            const string symbolEqualsMarker = "symbol=";
            var symbolEqualsPos = href.IndexOf(symbolEqualsMarker);

            if (symbolEqualsPos < 0) { return null; }
            var symbolIdText = href.Substring(symbolEqualsPos + symbolEqualsMarker.Length);
            if (string.IsNullOrWhiteSpace(symbolIdText)) { return null; }

            return int.TryParse(symbolIdText.Trim(), out int parsedId)
                ? parsedId
                : (int?)null;
        }

        private string GetBetweenMarkers(string contents, string openMarker, string closeMarker)
        {
            if (contents == null) { return null; }

            var openMarkerPos = contents.IndexOf(openMarker);
            if (openMarkerPos < 0) { return null; }

            var closeMarkerPos = contents.IndexOf(closeMarker, openMarkerPos + openMarker.Length);
            if (closeMarkerPos < 0) { return null; }

            var gutsStartPos = openMarkerPos + openMarker.Length;
            var gutsLength = closeMarkerPos - gutsStartPos;
            return contents.Substring(gutsStartPos, gutsLength);
        }

        private IMongoCollectionContext MarketSourceContext => new MongoCollectionContext(DbContext, "oex--get-market-source");
        private IMongoCollectionContext OrderBookContext => new MongoCollectionContext(DbContext, "oex--get-order-book");
        private IMongoDatabaseContext DbContext => new MongoDatabaseContext(_configClient.GetConnectionString(), DatabaseName);
    }
}
