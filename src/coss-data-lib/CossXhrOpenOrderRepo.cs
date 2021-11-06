using cache_lib.Models;
using config_connection_string_lib;
using coss_data_model;
using mongo_lib;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using trade_model;

namespace coss_data_lib
{
    public interface ICossXhrOpenOrderRepo
    {
        IMongoCollectionContext CollectionContext { get; }
        List<OpenOrderForTradingPair> Get();
        OpenOrderForTradingPair XhrToOpenOrder(CossXhrOpenOrder queryXhr);
    }

    public class CossXhrOpenOrderRepo : ICossXhrOpenOrderRepo
    {
        private readonly IGetConnectionString _getConnectionString;

        public CossXhrOpenOrderRepo(IGetConnectionString getConnectionString)
        {
            _getConnectionString = getConnectionString;
        }

        public List<OpenOrderForTradingPair> Get()
        {
            var snapshot = SnapshotContext.GetLast<CossXhrOpenOrderSnapshot>()
                ?? new CossXhrOpenOrderSnapshot();

            var eventContainers = snapshot.LastProcessedId != default(ObjectId)
                ? CollectionContext.GetCollection<CacheEventContainer>()
                    .AsQueryable().Where(item => item.Id > snapshot.LastProcessedId)
                    .OrderBy(item => item.Id)
                    .ToList()
                : CollectionContext.GetAll<CacheEventContainer>();

            if (eventContainers != null && eventContainers.Any())
            {
                foreach (var eventContainer in eventContainers)
                {
                    ApplyToSnapshot(snapshot, eventContainer);
                }

                snapshot.Id = default(ObjectId);
                SnapshotContext.Insert(snapshot);
            }

            var allOpenOrders = new List<OpenOrderForTradingPair>();
            foreach (var key in snapshot.OpenOrdersByTradingPair.Keys)
            {
                var xhrs = snapshot.OpenOrdersByTradingPair[key];
                var orders = xhrs
                    .Select(item => XhrToOpenOrder(item))
                    .ToList();

                allOpenOrders.AddRange(orders);
            }

            return allOpenOrders;
        }

        private void ApplyToSnapshot(CossXhrOpenOrderSnapshot snapshot, CacheEventContainer eventContainer)
        {
            if (eventContainer.Id <= snapshot.LastProcessedId) { return; }
            var pieces = eventContainer.CacheKey.Split('-');
            if (pieces.Count() != 2) { return; }
            if (string.IsNullOrWhiteSpace(pieces[0]) || string.IsNullOrWhiteSpace(pieces[1])) { return; }

            var symbol = pieces[0].Trim().ToUpper();
            var baseSymbol = pieces[1].Trim().ToString();

            var xhrs = JsonConvert.DeserializeObject<List<CossXhrOpenOrder>>(eventContainer.Raw);
            var orders =
                xhrs.Select(queryXhr =>
                {
                    var orderTypeDictionary = new Dictionary<string, OrderType>(StringComparer.InvariantCultureIgnoreCase)
                    {
                        { "buy", OrderType.Bid },
                        { "sell", OrderType.Ask }
                    };

                    var nativeOrderTypeText = queryXhr.type != null ? queryXhr.type.Trim() : null;
                    var orderType = orderTypeDictionary.ContainsKey(nativeOrderTypeText)
                        ? orderTypeDictionary[nativeOrderTypeText]
                        : OrderType.Unknown;

                    return new OpenOrderForTradingPair
                    {
                        Symbol = symbol,
                        BaseSymbol = baseSymbol,
                        OrderId = queryXhr.order_guid,
                        Price = queryXhr.price ?? 0,
                        Quantity = queryXhr.amount ?? 0,
                        OrderType = orderType
                    };
                })
                .ToList();


            snapshot.OpenOrdersByTradingPair[eventContainer.CacheKey.ToUpper()] = xhrs;
            snapshot.LastProcessedId = eventContainer.Id;
        }

        public OpenOrderForTradingPair XhrToOpenOrder(CossXhrOpenOrder queryXhr)
        {
            var orderTypeDictionary = new Dictionary<string, OrderType>(StringComparer.InvariantCultureIgnoreCase)
                    {
                        { "buy", OrderType.Bid },
                        { "sell", OrderType.Ask }
                    };

            var nativeOrderTypeText = queryXhr.type != null ? queryXhr.type.Trim() : null;
            var orderType = orderTypeDictionary.ContainsKey(nativeOrderTypeText)
                ? orderTypeDictionary[nativeOrderTypeText]
                : OrderType.Unknown;

            var pieces = queryXhr.pair_id.Split('-');
            if (pieces.Count() != 2) { return null; }
            var symbol = pieces[0].ToUpper();
            var baseSymbol = pieces[1].ToUpper();

            return new OpenOrderForTradingPair
            {
                Symbol = symbol,
                BaseSymbol = baseSymbol,
                OrderId = queryXhr.order_guid,
                Price = queryXhr.price ?? 0,
                Quantity = queryXhr.amount ?? 0,
                OrderType = orderType
            };
        }

        public IMongoCollectionContext CollectionContext
        {
            get { return new MongoCollectionContext(DbContext, "coss--open-orders"); }
        }

        public IMongoCollectionContext SnapshotContext
        {
            get { return new MongoCollectionContext(DbContext, "coss--open-orders-snapshot"); }
        }
        
        private IMongoDatabaseContext DbContext => new MongoDatabaseContext(_getConnectionString.GetConnectionString(), "coss");
    }
}
