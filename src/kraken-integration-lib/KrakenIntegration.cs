using date_time_lib;
using kraken_integration_lib.Models;
using kraken_integration_lib.Models.kraken_lib.Models;
using mongo_lib;
using MongoDB.Driver;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using trade_lib;
using cache_lib.Models;
using trade_model;
using web_util;
using cache_lib;
using config_client_lib;
using log_lib;
using kraken_lib.res;

namespace kraken_integration_lib
{
    // https://www.kraken.com/help/api
    public class KrakenIntegration : IKrakenIntegration
    {
        private static TimeSpan KrakenLedgerThreshold = TimeSpan.FromMinutes(30);
        private const string DatabaseName = "kraken";

        private static ThrottleContext ThrottleContext = new ThrottleContext
        {
            Locker = new object(),
            ThrottleThreshold = TimeSpan.FromSeconds(1)
        };

        private static TimeSpan HistoryCacheThreshold = TimeSpan.FromMinutes(5);

        private static readonly Dictionary<string, string> KrakenSymbolDictionary = new Dictionary<string, string>
        {
            { "XBT", "BTC" },
            { "XXBT", "BTC" },
            { "XETH", "ETH" },
            { "XDG", "DOGE" },
            { "XXMR", "XMR" },
            { "XZEC", "ZEC" },
            { "XXRP", "XRP" },
            { "ZUSD", "USD" },
            { "EOS", "EOS" },
            { "BCH", "BCH" },
            { "XMLN", "MLN" },
        };

        private readonly IWebUtil _webUtil;
        private readonly IMongoCollection<WebRequestEventContainer> _getAssetPairsCollection;
        private readonly IMongoCollection<WebRequestEventContainer> _getAssetsCollection;
        private readonly IMongoCollection<WebRequestEventContainer> _getBalanceCollection;
        private readonly IMongoCollection<WebRequestEventContainer> _getDepthCollection;
        private readonly IConfigClient _configClient;
        private readonly KrakenMap _map = new KrakenMap();

        private readonly ICacheUtil _cacheUtil;
        private readonly ILogRepo _log;

        public KrakenIntegration(
            IWebUtil webUtil,
            IConfigClient configClient,
            ICacheUtil cacheUtil,
            ILogRepo log)
        {

            _webUtil = webUtil;
            _configClient = configClient;
            _log = log;

            _cacheUtil = cacheUtil;

            var tradingPairsCollectionContext = new MongoCollectionContext(DbContext, "kraken-get-asset-pairs");
            _getAssetPairsCollection = tradingPairsCollectionContext.GetCollection<WebRequestEventContainer>();

            var getAssetsContext = new MongoCollectionContext(DbContext, "kraken-get-assets");
            _getAssetsCollection = getAssetsContext.GetCollection<WebRequestEventContainer>();

            var getBalanceContext = new MongoCollectionContext(DbContext, "kraken-get-balance");
            _getBalanceCollection = getBalanceContext.GetCollection<WebRequestEventContainer>();

            var getDepthContext = new MongoCollectionContext(DbContext, "kraken-get-depth");
            _getDepthCollection = getDepthContext.GetCollection<WebRequestEventContainer>();
        }

        public List<CommodityForExchange> GetCommodities(CachePolicy cachePolicy = CachePolicy.AllowCache)
        {
            return new List<CommodityForExchange>();
        }

        public HoldingInfo GetHoldings(CachePolicy cachePolicy)
        {
            var contents = GetNativeHoldings();
            Console.WriteLine(contents);

            // {"error":[],"result":{"ZUSD":"38.6711","XXRP":"200.00000000","XETH":"0.8932454000","XZEC":"0.0000000000","XXMR":"0.2500000000","EOS":"0.0008978700","BCH":"0.1995060000"}}

            var response = JsonConvert.DeserializeObject(contents);
            var result = ((JObject)response)["result"];

            var holdinginfo = new HoldingInfo();
            holdinginfo.Holdings = new List<Holding>();
            holdinginfo.TimeStampUtc = DateTime.UtcNow;
            foreach (JProperty kid in result.Children())
            {
                var krakenSymbolName = kid.Name;
                var symbolName = KrakenSymbolDictionary.ContainsKey(krakenSymbolName)
                    ? KrakenSymbolDictionary[krakenSymbolName]
                    : krakenSymbolName;

                var quantity = kid.Value.Value<decimal>();

                var holding = new Holding();
                holding.Asset = symbolName;
                holding.Available = quantity;
                holding.Total = quantity;

                holdinginfo.Holdings.Add(holding);
            }

            return holdinginfo;
        }

        private CacheResult GetOrderBookContents(TradingPair tradingPair, CachePolicy cachePolicy)
        {
            const string Url = "https://api.kraken.com/0/public/Depth";

            var toKrakenSymbol = new Func<string, string>(symbol =>
            {
                if (string.Equals(symbol, "BTC")) { return "XBT"; }
                if (string.Equals(symbol, "XDG")) { return "DOGE"; }

                return symbol;
            });

            var pairName = $"{toKrakenSymbol(tradingPair.Symbol.ToUpper())}{toKrakenSymbol(tradingPair.BaseSymbol.ToUpper())}";
            var payload = $"{{ \"pair\": \"{pairName}\" }}";

            var retriever = new Func<string>(() => _webUtil.Post(Url, payload));
            var collectionContext = new MongoCollectionContext(DbContext, $"kraken--depth-{tradingPair.Symbol}-{tradingPair.BaseSymbol}");
            var validator = new Func<string, bool>(text => !string.IsNullOrWhiteSpace(text));

            return _cacheUtil.GetCacheableEx(ThrottleContext, retriever, collectionContext, GetTradingPairsThreshold, cachePolicy, validator);
        }

        public OrderBook GetOrderBook(TradingPair tradingPair, CachePolicy cachePolicy)
        {
            var cacheResult = GetOrderBookContents(tradingPair, cachePolicy);

            var response = JsonConvert.DeserializeObject(cacheResult.Contents);
            var result = ((JObject)response)["result"];

            var orderBook = new OrderBook { Asks = new List<Order>(), Bids = new List<Order>() };
            var krakenOrderToOrder = new Func<JArray, Order>(krakenOrder =>
            {
                var krakenOrderPieces = krakenOrder.Children().ToList();
                var price = krakenOrderPieces[0].Value<decimal>();
                var quantity = krakenOrderPieces[1].Value<decimal>();
                var time = krakenOrderPieces[2];

                return new Order { Price = price, Quantity = quantity };
            });

            foreach (JProperty kid in result.Children())
            {
                var responsePairName = kid.Name;
                var asks = kid.Children()["asks"].FirstOrDefault() as JToken;
                if (asks != null)
                {
                    foreach (JArray ask in asks.Children())
                    {
                        var order = krakenOrderToOrder(ask);
                        orderBook.Asks.Add(order);
                    }
                }

                var bids = kid.Children()["bids"].FirstOrDefault() as JToken;
                if (bids != null)
                {
                    foreach (JArray bid in bids.Children())
                    {
                        var order = krakenOrderToOrder(bid);
                        orderBook.Bids.Add(order);
                    }
                }
            }

            /*
            var krakenAssets = new List<KrakenAsset>();
            foreach (JProperty kid in result.Children())
            {
                var symbol = kid.Name;
                var assetClass = kid.Children()["aclass"].FirstOrDefault()?.Value<string>();
                var altName = kid.Children()["altname"].FirstOrDefault()?.Value<string>();
                var decimals = kid.Children()["decimals"].FirstOrDefault()?.Value<int?>();
                var displayDecimals = kid.Children()["display_decimals"].FirstOrDefault()?.Value<int?>();
            */

            return orderBook;
        }

        private static TimeSpan GetAssetsThreshold = TimeSpan.FromMinutes(10);

        // https://api.kraken.com/0/public/Assets
        public List<KrakenAsset> GetNativeAssets()
        {
            const string Url = "https://api.kraken.com/0/public/Assets";

            var context = new WebRequestContext
            {
                Url = Url,
                Verb = "POST",
                Payload = null
            };

            var contents = GetCacheableWebRequest(_getAssetsCollection, context);

            var response = JsonConvert.DeserializeObject(contents);
            var result = ((JObject)response)["result"];

            var krakenAssets = new List<KrakenAsset>();
            foreach (JProperty kid in result.Children())
            {
                var symbol = kid.Name;
                var assetClass = kid.Children()["aclass"].FirstOrDefault()?.Value<string>();
                var altName = kid.Children()["altname"].FirstOrDefault()?.Value<string>();
                var decimals = kid.Children()["decimals"].FirstOrDefault()?.Value<int?>();
                var displayDecimals = kid.Children()["display_decimals"].FirstOrDefault()?.Value<int?>();

                var krakenAsset = new KrakenAsset
                {
                    Symbol = symbol,
                    AssetClass = assetClass,
                    AltName = altName,
                    Decimals = decimals,
                    DisplayDecimals = displayDecimals
                };

                krakenAssets.Add(krakenAsset);
            }

            return krakenAssets;
        }

        private string GetNativeHoldings()
        {
            return QueryPrivate("Balance");
        }

        public string GetCacheableWebRequest(
            IMongoCollection<WebRequestEventContainer> collection,
            WebRequestContext context)
        {            
            var mostRecent = collection.AsQueryable()
                .OrderByDescending(item => item.Id)
                .Where(item => item.Context.SearchableContext == context.SearchableContext)
                .FirstOrDefault();

            var currentTime = DateTime.UtcNow;
            if (mostRecent != null
                && (currentTime - mostRecent.StartTimeUtc) < GetAssetsThreshold
                && !string.IsNullOrWhiteSpace(mostRecent.Raw))
            {
                return mostRecent.Raw;
            }

            var (startTime, contents, endTime) = HttpPost(context.Url, context.Payload);

            var ec = new WebRequestEventContainer
            {
                StartTimeUtc = startTime,
                EndTimeUtc = endTime,
                Raw = contents,
                Context = context
            };

            _getAssetsCollection.InsertOne(ec);

            return contents;
        }

        private static TimeSpan GetTradingPairsThreshold = TimeSpan.FromMinutes(10);

        public List<TradingPair> GetTradingPairs(CachePolicy cachePolicy = CachePolicy.AllowCache)
        {
            var native = GetNativeAssetPairs();
            return native.Select(item => item.ToTradingPair()).ToList();
        }

        public List<KrakenAssetPair> GetNativeAssetPairs()
        {
            const string Url = "https://api.kraken.com/0/public/AssetPairs";
            var raw = GetCacheableWebRequest(_getAssetPairsCollection, new WebRequestContext { Url = Url, Verb = "POST" });

            var json = (JToken)JsonConvert.DeserializeObject(raw);
            var result = json["result"];

            var pairs = new List<KrakenAssetPair>();
            foreach (JProperty kid in result.Children())
            {
                var pairName = kid.Name;
                var kidContents = JsonConvert.SerializeObject(kid.Value);
                var krakenAssetPair = JsonConvert.DeserializeObject<KrakenAssetPair>(kidContents);
                krakenAssetPair.Name = pairName;

                pairs.Add(krakenAssetPair);
            }

            return pairs;
        }

        public decimal? GetWithdrawalFee(string symbol, CachePolicy cachePolicy)
        {
            var fees = GetWithdrawalFees(cachePolicy);
            if (fees.ContainsKey(symbol)) { return fees[symbol]; }

            return null;
        }

        public Dictionary<string, decimal> GetWithdrawalFees(CachePolicy cachePolicy)
        {
            // https://support.kraken.com/hc/en-us/articles/201893608-What-are-the-withdrawal-fees-

            return new Dictionary<string, decimal>
            {

                //Bitcoin (XBT) - ฿0.0005
                { "BTC", 0.0005m },
                
                //Ether (ETH) - Ξ0.005
                { "ETH", 0.005m },
                
                //Ripple (XRP) - Ʀ0.02
                { "XRP", 0.02m },
                
                //Stellar (XLM) - *0.00002
                { "XLM", 0.00002m },
                
                //Litecoin (LTC) - Ł0.001
                { "LTC", 0.001m },
                
                //Dogecoin (XDG) - Ð2.00
                { "DOGE", 2.00m },
                
                //Zcash (ZEC) - ⓩ0.00010
                { "ZEC", 0.00010m },
                
                //Iconomi (ICN) - ICN 0.2
                { "ICN", 0.2m },
                
                //Augur (REP) - Ɍ0.01
                { "REP", 0.01m },
                
                //Ether Classic (ETC) - ξ0.005
                { "ETC", 0.005m },
                
                //Melonport (MLN) - M0.003
                { "MLN", 0.003m },
                
                //Monero (XMR) - ɱ0.05
                { "XRM", 0.05m },
                
                //Dash (DASH) - Đ0.005
                { "DASH", 0.005m },
                
                //Dash Instant - Đ0.01
                { "Dash Instant", 0.01m },
                
                //Gnosis (GNO) - Ğ0.01
                { "GNO", 0.01m },
                
                //Tether (USDT) - USD₮ 5
                { "USDT", 5.0m },
                
                //EOS (EOS) - Ȅ0.50000
                { "EOS", 0.50000m },
                
                //Bitcoin Cash (BCH) - ฿0.0001
                { "BCH", 0.0001m },
            };
        }

        private (DateTime startTime, string contents, DateTime endTime) HttpPost(string url, string data = null, List<KeyValuePair<string, string>> headers = null)
        {
            return Throttle(() =>
            {
                var startTime = DateTime.UtcNow;
                var contents = _webUtil.Post(url, data, headers);
                var endTime = DateTime.UtcNow;

                return (startTime, contents, endTime);
            });
        }

        private static object Locker = new object();
        private static DateTime? LastReadTime;
        private static TimeSpan ThrottleThreshold = TimeSpan.FromMilliseconds(1500);

        private static T Throttle<T>(Func<T> getter)
        {
            lock (Locker)
            {
                if (LastReadTime.HasValue)
                {
                    var remainigTime = ThrottleThreshold - (DateTime.UtcNow - LastReadTime.Value);
                    if (remainigTime > TimeSpan.Zero)
                    {
                        Thread.Sleep(remainigTime);
                    }
                }

                LastReadTime = DateTime.UtcNow;
                return getter();
            }
        }

        public List<DepositAddressWithSymbol> GetDepositAddresses(CachePolicy cachePolicy)
        {
            throw new NotImplementedException();
        }

        public DepositAddress GetDepositAddress(string symbol, CachePolicy cachePolicy)
        {
            throw new NotImplementedException();
        }

        public void SetDepositAddress(DepositAddress depositAddress)
        {
            throw new NotImplementedException();
        }

        private TradingPair KrakenPairToTradingPair(string krakenPair)
        {
            if (string.IsNullOrWhiteSpace(krakenPair)) { return null; }
            var nativeSymbol = krakenPair.Substring(0, krakenPair.Length / 2);
            var nativeBaseSymbol = krakenPair.Substring(nativeSymbol.Length);

            var canon = _map.GetCanon(nativeSymbol);
            var baseCanon = _map.GetCanon(nativeBaseSymbol);

            return new TradingPair
            {
                CanonicalCommodityId = canon?.Id,
                Symbol = !string.IsNullOrWhiteSpace(canon?.Symbol)
                    ? canon?.Symbol
                    : nativeSymbol,
                NativeSymbol = nativeSymbol,
                NativeCommodityName = nativeSymbol,
                CommodityName = !string.IsNullOrWhiteSpace(canon?.Name)
                    ? canon?.Name
                    : nativeSymbol,

                CanonicalBaseCommodityId = baseCanon?.Id,
                BaseSymbol = !string.IsNullOrWhiteSpace(baseCanon?.Symbol)
                    ? baseCanon?.Symbol
                    : nativeBaseSymbol,
                NativeBaseSymbol = nativeBaseSymbol,
                BaseCommodityName = !string.IsNullOrWhiteSpace(baseCanon?.Name)
                    ? baseCanon?.Name
                    : nativeBaseSymbol,
                NativeBaseCommodityName = nativeBaseSymbol
            };
        }

        private DateTime KrakenTimeToDateTimeUtc(decimal krakenTime)
        {
            return DateTimeUtil.UnixTimeStampToDateTime((double)krakenTime)
                ?? DateTime.MinValue;
        }

        private TradeTypeEnum KrakenBuyOrSellToTradeType(string buyOrSell)
        {
            var effective = (buyOrSell ?? string.Empty).Trim();
            var dict = new Dictionary<string, TradeTypeEnum>(StringComparer.InvariantCultureIgnoreCase)
            {
                { "Buy", TradeTypeEnum.Buy },
                { "Sell", TradeTypeEnum.Sell }
            };

            return dict.ContainsKey(effective)
                ? dict[effective]
                : TradeTypeEnum.Unknown;
        }

        public List<HistoricalTrade> GetUserTradeHistory(CachePolicy cachePolicy)
        {
            return GetUserTradeHistoryV2(cachePolicy)?.History;
        }

        private const int KrakenApiVersion = 0;
        private const string BaseApiUrl = "https://api.kraken.com";

        public string Name => "Kraken";
        public Guid Id => new Guid("73F86A5C-F998-498B-A4E2-3C9E120F5963");

        private string GetSignature(ApiKey apiKey, string path, string propertiesWithNonce)
        {
            var nonce = DateTime.Now.Ticks;

            byte[] base64DecodedSecred = Convert.FromBase64String(apiKey.Secret);

            //var np = nonce + Convert.ToChar(0) + propertiesWithNonce;

            var pathBytes = Encoding.UTF8.GetBytes(propertiesWithNonce);
            var hash256Bytes = sha256_hash(propertiesWithNonce);
            var z = new byte[pathBytes.Count() + hash256Bytes.Count()];
            pathBytes.CopyTo(z, 0);
            hash256Bytes.CopyTo(z, pathBytes.Count());

            var sigBytes = getHash(base64DecodedSecred, z);
            var sigText = Convert.ToBase64String(sigBytes);

            return sigText;
        }

        private string ManualPost(string url, string payload, string contentType, List<KeyValuePair<string, string>> headers)
        {
            var webRequest = (HttpWebRequest)WebRequest.Create(url);
            webRequest.Method = "POST";

            if (!string.IsNullOrWhiteSpace(contentType))
            {
                webRequest.ContentType = contentType;
            }           
            
            foreach (var header in headers ?? new List<KeyValuePair<string, string>>())
            {
                webRequest.Headers.Add(header.Key, header.Value);
            }

            if (!string.IsNullOrWhiteSpace(payload))
            {
                using (var writer = new StreamWriter(webRequest.GetRequestStream()))
                {
                    writer.Write(payload);
                }
            }

            return Throttle(() =>
            {
                using (WebResponse webResponse = webRequest.GetResponse())
                {
                    using (Stream str = webResponse.GetResponseStream())
                    {
                        using (StreamReader sr = new StreamReader(str))
                        {
                            return sr.ReadToEnd();
                        }
                    }
                }
            });
        }

        public string QueryPrivate(string krakenMethod, Dictionary<string, object> propDictionary = null)
        {
            var propText = propDictionary != null
                ? string.Join(string.Empty, propDictionary.Keys.ToList()
                    .Select(queryKey => $"&{queryKey}={propDictionary[queryKey]}")
                    .ToList())
                : string.Empty;

            var apiKey = _configClient.GetKrakenApiKey();

            // generate a 64 bit nonce using a timestamp at tick resolution
            Int64 nonce = DateTime.Now.Ticks;
            var effectiveProperties = "nonce=" + nonce + propText;

            string path = string.Format("/{0}/private/{1}", KrakenApiVersion, krakenMethod);
            string address = BaseApiUrl + path;
            var webRequest = (HttpWebRequest)WebRequest.Create(address);
            webRequest.ContentType = "application/x-www-form-urlencoded";
            webRequest.Method = "POST";
            webRequest.Headers.Add("API-Key", apiKey.Key);

            byte[] base64DecodedSecred = Convert.FromBase64String(apiKey.Secret);

            var np = nonce + Convert.ToChar(0) + effectiveProperties;

            var pathBytes = Encoding.UTF8.GetBytes(path);
            var hash256Bytes = sha256_hash(np);
            var z = new byte[pathBytes.Count() + hash256Bytes.Count()];
            pathBytes.CopyTo(z, 0);
            hash256Bytes.CopyTo(z, pathBytes.Count());

            var signature = getHash(base64DecodedSecred, z);

            webRequest.Headers.Add("API-Sign", Convert.ToBase64String(signature));

            if (effectiveProperties != null)
            {
                using (var writer = new StreamWriter(webRequest.GetRequestStream()))
                {
                    writer.Write(effectiveProperties);
                }
            }

            return Throttle(() =>
            {
                using (WebResponse webResponse = webRequest.GetResponse())
                {
                    using (Stream str = webResponse.GetResponseStream())
                    {
                        using (StreamReader sr = new StreamReader(str))
                        {
                            return sr.ReadToEnd();
                        }
                    }
                }
            });
        }

        private byte[] sha256_hash(String value)
        {
            using (SHA256 hash = SHA256Managed.Create())
            {
                Encoding enc = Encoding.UTF8;

                Byte[] result = hash.ComputeHash(enc.GetBytes(value));

                return result;
            }
        }

        private byte[] getHash(byte[] keyByte, byte[] messageBytes)
        {
            using (var hmacsha512 = new HMACSHA512(keyByte))
            {
                return hmacsha512.ComputeHash(messageBytes);
            }
        }

        public string LedgerStuff()
        {
            // string reqs = string.Format("pair={0}", pair);

            // if (count.HasValue)
            // {
            //     reqs += string.Format("&count={0}", count.Value.ToString());
            // }

            /*
                aclass = asset class (optional):
                    currency (default)
                asset = comma delimited list of assets to restrict output to (optional.  default = all)
                type = type of ledger to retrieve (optional):
                    all (default)
                    deposit
                    withdrawal
                    trade
                    margin
                start = starting unix timestamp or ledger id of results (optional.  exclusive)
                end = ending unix timestamp or ledger id of results (optional.  inclusive)
                ofs = result offset
            */

            var props = new Dictionary<string, object>();
            props["end"] = 1524969994.0983;

            var result = QueryPrivate("Ledgers", props);

            return result;
        }

        public List<KrakenLedgerItemAndKey> GetAllLedgers()
        {
            var aggregateItems = new List<KrakenLedgerItemAndKey>();
            var getNextLedger = new Func<KrakenLedger>(() =>
            {
                if (!aggregateItems.Any()) { return LedgerWithProps(null); }
                var earliestTime = aggregateItems.Min(item => item.LedgerItem.Time);

                return LedgerWithProps(new Dictionary<string, object> { { "end", earliestTime } });
            });

            var iterations = 0;
            List<string> newKeys = null;
            do
            {
                var nextLedger = getNextLedger();
                var nextLedgerKeys = nextLedger.Result.Ledger.Keys.ToList();

                var currentKeys = aggregateItems.Select(item => item.Key).ToList();
                newKeys = nextLedgerKeys.Where(queryNextKey => !currentKeys.Any(queryCurrentKey => string.Equals(queryNextKey, queryCurrentKey, StringComparison.InvariantCultureIgnoreCase)))
                    .ToList();

                newKeys.ForEach(queryNewKey =>
                {
                    aggregateItems.Add(
                    new KrakenLedgerItemAndKey
                    {
                        Key = queryNewKey,
                        LedgerItem = nextLedger.Result.Ledger[queryNewKey]
                    });
                });
                iterations++;
            } while (newKeys.Any() && iterations < 10);

            // var isThereAnythingNew = nextLedgerKeys.Any(queryNextKey => !currentKeys.Any(queryCurrentKey => string.Equals(queryNextKey, queryCurrentKey, StringComparison.InvariantCultureIgnoreCase)));

            return aggregateItems;
        }

        public KrakenLedger LedgerWithProps(Dictionary<string, object> props)
        {
            var responseText = QueryPrivate("Ledgers", props);

            return JsonConvert.DeserializeObject<KrakenLedger>(responseText);
        }

        public string LedgerStuff_Individual()
        {
            var props = new Dictionary<string, object>();
            // props["start"] = "LE7EDD-UQ6H4-LAKZFD";
            props["end"] = "LE7EDD-UQ6H4-LAKZFD";

            var result = QueryPrivate("Ledgers", props);

            return result;
        }

        public (List<KrakenLedgerItemAndKey> LedgerItems, DateTime? AsOfUtc) GetNatveLedgerWithAsOf(CachePolicy cachePolicy)
        {            
            var translator = new Func<string, List<KrakenLedgerItemAndKey>>(text =>
                !string.IsNullOrWhiteSpace(text)
                    ? JsonConvert.DeserializeObject<List<KrakenLedgerItemAndKey>>(text)
                    : null);

            var validator = new Func<string, bool>(text =>
            {
                if (string.IsNullOrWhiteSpace(text)) { throw new ApplicationException("Kraken ledger response must not be null or whitespace."); }
                if (translator(text) == null) { throw new ApplicationException("Failed to parse kraken ledger response."); }

                return true;
            });

            var retriever = new Func<string>(() =>
            {
                try
                {
                    var aggregate = GetAllLedgers();
                    if (aggregate == null) { throw new ApplicationException("Kraken - retrieved null ledgers"); }
                    return JsonConvert.SerializeObject(aggregate);
                }
                catch (Exception exception)
                {
                    _log.Error("Failed to retrieve Kraken ledger.");
                    _log.Error(exception);
                    throw;
                }
            });

            var collectionContext = new MongoCollectionContext(DbContext, "kraken--ledger");
            var cacheResult = _cacheUtil.GetCacheableEx(ThrottleContext, retriever, collectionContext, KrakenLedgerThreshold, cachePolicy, validator);

            var native = translator(cacheResult?.Contents);

            return (native, cacheResult?.AsOf);
        }

        public (KrakenLedger Ledger, DateTime? AsOfUtc) GetNatveLedgerWithAsOfOld(CachePolicy cachePolicy)
        {
            var translator = new Func<string, KrakenLedger>(text => 
                !string.IsNullOrWhiteSpace(text)
                    ? JsonConvert.DeserializeObject<KrakenLedger>(text)
                    : null);

            var validator = new Func<string, bool>(text =>
            {
                if (string.IsNullOrWhiteSpace(text)) { throw new ApplicationException("Kraken ledger response must not be null or whitespace."); }
                if (translator(text) == null) { throw new ApplicationException("Failed to parse kraken ledger response."); }

                return true;
            });

            var retriever = new Func<string>(() =>
            {
                try
                {
                    var text = QueryPrivate("Ledgers");
                    if (!validator(text)) { throw new ApplicationException("Kraken ledger failed validation."); }
                    return text;
                }
                catch (Exception exception)
                {
                    _log.Error("Failed to retrieve Kraken ledger.");
                    _log.Error(exception);
                    throw;
                }
            });

            var collectionContext = new MongoCollectionContext(DbContext, "kraken--get-ledger");            
            var cacheResult = _cacheUtil.GetCacheableEx(ThrottleContext, retriever, collectionContext, KrakenLedgerThreshold, cachePolicy, validator);

            var native = translator(cacheResult?.Contents);

            return (native, cacheResult?.AsOf);
        }

        private AsOfWrapper<KrakenGetTradesHistoryResponse> GetMyNativeTradeHistoryWithAsOf(CachePolicy cachePolicy)
        {

            var translator = new Func<string, KrakenGetTradesHistoryResponse>(text =>
            {
                return !string.IsNullOrWhiteSpace(text)
                    ? JsonConvert.DeserializeObject<KrakenGetTradesHistoryResponse>(text)
                    : null;
            });

            var validator = new Func<string, bool>(text =>
            {
                if (string.IsNullOrWhiteSpace(text)) { throw new ApplicationException("Received a null or whitespace response when requesting Kraken native history."); }
                translator(text);

                return true;
            });

            var retriever = new Func<string>(() =>
            {
                try
                {
                    var text = QueryPrivate("TradesHistory");
                    if (!validator(text)) { throw new ApplicationException("Validation failed when requesting Kraken trade history."); }
                    return text;
                }
                catch(Exception exception)
                {
                    _log.Error($"Encountered an exception when requesting Kraken trade history.{Environment.NewLine}{exception.Message}");
                    _log.Error(exception);
                    throw;
                }
            });

            var collectionContext = new MongoCollectionContext(DbContext, "kraken--trade-history");

            var cacheResult = _cacheUtil.GetCacheableEx(ThrottleContext, retriever, collectionContext, HistoryCacheThreshold, cachePolicy);

            // var history = translator(cacheResult?.Contents);

            return new AsOfWrapper<KrakenGetTradesHistoryResponse>
            {
                AsOfUtc = cacheResult?.AsOf,
                Data = translator(cacheResult?.Contents)
            };
        }

        private string ParseKrakenErrors(string contents)
        {
            var json = (JObject)JsonConvert.DeserializeObject(contents);
            var error = json["error"];
            if (error == null) { return null; }

            var errors = new List<string>();
            if (error is JArray jarrayError)
            {
                foreach (var err in jarrayError)
                {
                    var errorText = err.ToString();
                    errors.Add(errorText);
                }
            }

            if (!errors.Any()) { return null; }
            return string.Join(Environment.NewLine, errors);
        }

        public Holding GetHolding(string symbol)
        {
            throw new NotImplementedException();
        }

        public bool BuyMarket(TradingPair tradingPair, decimal quantity)
        {
            throw new NotImplementedException();
        }

        public bool SellMarket(TradingPair tradingPair, decimal quantity)
        {
            throw new NotImplementedException();
        }

        public HistoryContainer GetUserTradeHistoryV2(CachePolicy cachePolicy)
        {
            var nativeLedgerWithAsOf = GetNatveLedgerWithAsOf(cachePolicy);
            var nativeTradeHistoryWithAsOf = GetMyNativeTradeHistoryWithAsOf(cachePolicy);            

            var nativeTradeHistoryAsOf = nativeTradeHistoryWithAsOf.AsOfUtc;
            var nativeLedgerHistoryAsOf = nativeTradeHistoryWithAsOf.AsOfUtc;

            DateTime? effectiveAsOfUtc = null;

            if (!nativeTradeHistoryAsOf.HasValue && !nativeLedgerHistoryAsOf.HasValue)
            { effectiveAsOfUtc = null; }
            else if (nativeTradeHistoryAsOf.HasValue && nativeLedgerHistoryAsOf.HasValue)
            { effectiveAsOfUtc = new List<DateTime> { nativeTradeHistoryAsOf.Value, nativeLedgerHistoryAsOf.Value }.Min(item => item); }
            else if (nativeTradeHistoryAsOf.HasValue)
            { effectiveAsOfUtc = nativeTradeHistoryAsOf.Value; }
            else if (nativeLedgerHistoryAsOf.HasValue)
            { effectiveAsOfUtc = nativeLedgerHistoryAsOf.Value; }
            else
            { effectiveAsOfUtc = null; }

            var nativeTradeHistory = nativeTradeHistoryWithAsOf.Data;
            var nativeLedgerHistory = nativeLedgerWithAsOf.LedgerItems;

            var tradeHistory = nativeTradeHistory?.Result?.Trades.Keys
                .Select(key =>
                {
                    var item = nativeTradeHistory?.Result.Trades[key];
                    var tradingPair = KrakenPairToTradingPair(item.Pair);
                    var translated = new HistoricalTrade
                    {
                        NativeId = key,
                        Comments = $"OrderTxId: {item.OrderTxId}",
                        TradingPair = tradingPair,
                        Symbol = tradingPair?.Symbol,
                        BaseSymbol = tradingPair?.BaseSymbol,
                        Price = item.Price,
                        Quantity = item.Vol,
                        TimeStampUtc = KrakenTimeToDateTimeUtc(item.Time),
                        TradeType = KrakenBuyOrSellToTradeType(item.Type),
                        FeeQuantity = item.Fee
                    };

                    if (translated.TradeType == TradeTypeEnum.Buy || translated.TradeType == TradeTypeEnum.Sell)
                    {
                        translated.FeeCommodity = translated.BaseSymbol;
                    }

                    return translated;
                })
                .ToList();

            foreach (var ledgerItemWithKey in nativeLedgerHistory)
            {
                var key = ledgerItemWithKey?.Key;
                var ledgerItem = ledgerItemWithKey?.LedgerItem;
                if (ledgerItem == null || string.Equals(ledgerItem.Type,"trade", StringComparison.InvariantCultureIgnoreCase))
                {
                    continue;
                }

                var ledgerDateTimeUtc = DateTimeUtil.UnixTimeStampToDateTime(ledgerItem.Time);

                var nativeSymbol = ledgerItem.Asset;

                // it also has "trade"
                // need to figure out
                // hot to map this to buy/sell
                var ledgerTypeDictionary = new Dictionary<string, TradeTypeEnum>
                {
                    { "withdrawal", TradeTypeEnum.Withdraw },
                    { "deposit", TradeTypeEnum.Deposit },
                };

                var tradeType = ledgerTypeDictionary.ContainsKey(ledgerItem.Type)
                    ? ledgerTypeDictionary[ledgerItem.Type]
                    : TradeTypeEnum.Unknown;

                var canon = _map.GetCanon(ledgerItem.Asset);
                var symbol = !string.IsNullOrWhiteSpace(canon?.Symbol)
                    ? canon.Symbol
                    : ledgerItem.Asset;

                var historicalTrade = new HistoricalTrade
                {
                    NativeId = key,
                    TimeStampUtc = ledgerDateTimeUtc ?? default(DateTime),
                    Quantity = Math.Abs(ledgerItem.Amount),
                    Price = default(decimal),
                    TradeType = tradeType,
                    FeeQuantity = ledgerItem.Fee,
                    Symbol = symbol
                };

                tradeHistory.Add(historicalTrade);
            }

            var orderedHistory = tradeHistory != null
                ? tradeHistory.OrderBy(item => item.TimeStampUtc).ToList()
                : null;

            return new HistoryContainer
            {
                History = orderedHistory,
                AsOfUtc = effectiveAsOfUtc
            };
        }

        private IMongoDatabaseContext DbContext
        {
            get { return new MongoDatabaseContext(_configClient.GetConnectionString(), DatabaseName); }
        }
    }
}
