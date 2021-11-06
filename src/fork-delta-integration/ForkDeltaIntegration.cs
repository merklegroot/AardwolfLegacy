using fork_delta_integration.Models;
using mongo_lib;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using res_util_lib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using trade_lib;
using cache_lib.Models;
using trade_model;
using web_util;

namespace fork_delta_integration
{
    // https://github.com/forkdelta/backend-replacement/tree/master/docs/api
    // https://www.reddit.com/r/ForkDelta/comments/7pen41/api_for_forkdelta/
    // https://api.forkdelta.com/returnTicker
    public class ForkDeltaIntegration : ITradeIntegration
    {
        private readonly IWebUtil _webUtil;
        private readonly IMongoCollection<WebRequestEventContainer> _tickerCollection;

        public ForkDeltaIntegration(            
            IWebUtil webUtil,
            IMongoDatabaseContext dbContext)
        {
            
            _webUtil = webUtil;
            var tickerCollectionContext = new MongoCollectionContext(dbContext, "fork-delta-ticker");
            _tickerCollection = tickerCollectionContext.GetCollection<WebRequestEventContainer>();
        }

        public string Name => "ForkDelta";
        public Guid Id => new Guid("377C000B-C239-465E-BE4E-9FADA6FAA587");

        public List<string> GetCoins()
        {
            throw new NotImplementedException();
        }

        private string HttpGetOperation(           
            string url,
            IMongoCollection<WebRequestEventContainer> collection)
        {
            var startTimeUtc = DateTime.UtcNow;
            var requestResult = HttpGet(url);
            var endTimeUtc = DateTime.UtcNow;

            var ec = new WebRequestEventContainer
            {
                StartTimeUtc = requestResult.startTime,
                EndTimeUtc = requestResult.endTime,
                Raw = requestResult.contents,
                Context = new WebRequestContext
                {
                    Url = url,
                    Verb = "GET",
                    Payload = null,
                    Headers = null
                }
            };

            collection.InsertOne(ec);

            return requestResult.contents;
        }

        public List<CommodityForExchange> GetCommodities(CachePolicy cachePolicy = CachePolicy.AllowCache)
        {
            var nativeItems = GetNativeTickerItems();
            if (nativeItems == null) { return new List<CommodityForExchange>(); }

            return nativeItems
                .Where(item => item != null && !string.IsNullOrWhiteSpace(item.TokenAddress) && SymbolContractDictionary.ContainsKey(item.TokenAddress))
                .Select(item => new CommodityForExchange
            {
                Symbol = SymbolContractDictionary[item.TokenAddress],
                NativeSymbol = SymbolContractDictionary[item.TokenAddress],
                }).ToList();
        }

        private static Dictionary<string, string> _symbolContractDictionary;
        private static Dictionary<string, string> SymbolContractDictionary
        {
            get
            {
                var getter = new Func<Dictionary<string, string>>(() =>
                {
                    var simpleCommodities = ResUtil.Get<List<SimpleCommodity>>("eth-commodities.json", typeof(SimpleCommodity).Assembly);
                    var dict = new Dictionary<string, string>();
                    foreach (var commodity in simpleCommodities)
                    {
                        if (!string.IsNullOrWhiteSpace(commodity.ContractId))
                        {
                            dict[commodity.ContractId] = commodity.Symbol;
                        }
                    }

                    return dict;
                });

                return _symbolContractDictionary ?? (_symbolContractDictionary = getter());
            }
        }

        private static TimeSpan TickerLifespan = TimeSpan.FromMinutes(30);
        public List<ForkDeltaTickerItem> GetNativeTickerItems()
        {
            const string Url = "https://api.forkdelta.com/returnTicker";

            var ec = _tickerCollection.AsQueryable()
                .OrderByDescending(item => item.Id)
                .FirstOrDefault();

            string contents = null;
            if (ec != null && !string.IsNullOrWhiteSpace(ec.Raw) && (DateTime.UtcNow - ec.StartTimeUtc) < TickerLifespan)
            {
                contents = ec.Raw;
            }
            else
            {
                contents = HttpGetOperation(Url, _tickerCollection);
            }            

            if (string.IsNullOrWhiteSpace(contents)) { return new List<ForkDeltaTickerItem>();}

            var json = (JObject)JsonConvert.DeserializeObject(contents);
            var items = new List<ForkDeltaTickerItem>();
            foreach (JProperty kid in json.Children())
            {
                var name = kid.Name;
                var value = kid.Value;
                var valueJson = JsonConvert.SerializeObject(value);
                var tickerItem = JsonConvert.DeserializeObject<ForkDeltaTickerItem>(valueJson);

                items.Add(tickerItem);
            }

            return items;
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

        public List<HistoricalTrade> GetUserTradeHistory()
        {
            throw new NotImplementedException();
        }

        public OrderBook GetOrderBook(TradingPair tradingPair, CachePolicy cachePolicy)
        {
            const string Url = "https://api.forkdelta.com/getMarket";//?token=0x86fa049857e0209aa7d9e616f7eb3b3b78ecfdb0";

            var contents = _webUtil.Get(Url);
            Console.WriteLine(contents);

            return null;
            //throw new NotImplementedException();
        }

        public List<TradingPair> GetTradingPairs(CachePolicy cachePolicy = CachePolicy.AllowCache)
        {
            throw new NotImplementedException();
        }

        public decimal? GetWithdrawalFee(string symbol, CachePolicy cachePolicy)
        {
            return null;
        }

        public Dictionary<string, decimal> GetWithdrawalFees(CachePolicy cachePolicy)
        {
            return new Dictionary<string, decimal>();
        }

        public void SetDepositAddress(DepositAddress depositAddress)
        {
            throw new NotImplementedException();
        }

        private static object Locker = new object();
        private static DateTime? LastReadTime;
        // idex's rate limit is 100 per minute (or 600ms)
        // taking it up to 1000ms to be on the safe side.
        private static TimeSpan ThrottleThreshold = TimeSpan.FromMilliseconds(1000);

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

        private (DateTime startTime, string contents, DateTime endTime) HttpPost(string url, string data = null)
        {
            return Throttle(() =>
            {
                var startTime = DateTime.UtcNow;
                var contents = _webUtil.Post(url, data);
                var endTime = DateTime.UtcNow;

                return (startTime, contents, endTime);
            });
        }

        private (DateTime startTime, string contents, DateTime endTime) HttpGet(string url)
        {
            return Throttle(() =>
            {
                var startTime = DateTime.UtcNow;
                var contents = _webUtil.Get(url);
                var endTime = DateTime.UtcNow;

                return (startTime, contents, endTime);
            });
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
    }
}
