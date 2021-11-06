using cache_lib;
using cache_lib.Models;
using cache_lib.Models.Snapshots;
using commodity_map;
using log_lib;
using mongo_lib;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using trade_model;

namespace trade_lib
{
    public abstract class OrderBookIntegration : ITradeGetCachedOrderBooks
    {
        private readonly ICacheUtil _cacheUtil = new CacheUtil();

        protected abstract ILogRepo Log { get; }

        public virtual List<OrderBookAndTradingPair> GetCachedOrderBooks()
        {
            var context = GetAllOrderBooksCollectionContext();
            var collection = context.GetCollection<AllOrdersSnapshot>();

            var snapshot = context.GetLast<AllOrdersSnapshot>();
            if (snapshot?.SnapshotItems == null) { return new List<OrderBookAndTradingPair>(); }

            var orders = new List<OrderBookAndTradingPair>();
            foreach (var key in snapshot.SnapshotItems.Keys)
            {
                var cachedOrderBook = snapshot.SnapshotItems[key];
                var orderBookAndTradingPair = new OrderBookAndTradingPair
                {
                    Symbol = cachedOrderBook.Symbol,
                    BaseSymbol = cachedOrderBook.BaseSymbol,
                };

                var orderBook = ToOrderBook(cachedOrderBook.Raw, cachedOrderBook.AsOfUtc);
                orderBookAndTradingPair.Asks = orderBook.Asks;
                orderBookAndTradingPair.Bids = orderBook.Bids;
                orderBookAndTradingPair.AsOf = orderBook.AsOf;

                orders.Add(orderBookAndTradingPair);
            }

            return orders;
        }

        public virtual OrderBook GetOrderBook(TradingPair tradingPair, CachePolicy cachePolicy)
        {
            try
            {
                var nativeSymbol = Map.ToNativeSymbol(tradingPair.Symbol.ToUpper());
                var nativeBaseSymbol = Map.ToNativeSymbol(tradingPair.BaseSymbol.ToUpper());

                var validator = new Func<string, bool>(text => !string.IsNullOrWhiteSpace(text));

                var retriever = new Func<string>(() =>
                {
                    try
                    {
                        var response = GetNativeOrderBookContents(nativeSymbol, nativeBaseSymbol);
                        if (!validator(response))
                        {
                            throw new ApplicationException($"Get order book for {tradingPair.Symbol}-{tradingPair.BaseSymbol} failed validation.");
                        }

                        return response;
                    }
                    catch (Exception exception)
                    {
                        Log.Error(exception);
                        throw;
                    }
                });

                var key = $"{tradingPair.Symbol.ToUpper()}-{tradingPair.BaseSymbol.ToUpper()}";
                var collectionContext = GetOrderBookContext();

                var cacheResult = _cacheUtil.GetCacheableEx(ThrottleContext, retriever, collectionContext, OrderBookCacheThreshold, cachePolicy, validator, AfterOrderBookInsert, key);

                if (string.IsNullOrWhiteSpace(cacheResult?.Contents)) { return null; }

                return ToOrderBook(cacheResult?.Contents, cacheResult?.AsOf);
            }
            catch (Exception exception)
            {
                var error = new StringBuilder()
                    .AppendLine($"BinanceIntegration -- Failed to get order book for {tradingPair} with cache policy {cachePolicy}.");

                Log.Error(exception);
                throw;
            }
        }

        protected abstract string GetNativeOrderBookContents(string nativeSymbol, string nativeBaseSymbol);

        protected abstract string CollectionPrefix { get; }

        protected abstract OrderBook ToOrderBook(string text, DateTime? asOf);

        protected abstract IMongoDatabaseContext DbContext { get;  }

        protected void AfterOrderBookInsert(CacheEventContainer cacheEventContainer)
        {
            var itemContext = GetOrderBookContext();
            var snapShotContext = GetAllOrderBooksCollectionContext();
            var snapShot = snapShotContext
                .GetLast<AllOrdersSnapshot>();

            List<CacheEventContainer> itemsToApply = null;
            if (snapShot != null)
            {
                itemsToApply =
                    itemContext.GetCollection<CacheEventContainer>()
                    .AsQueryable()
                    .Where(item => item.Id > snapShot.LastId)
                    .OrderBy(item => item.Id)
                    .ToList();
            }
            else
            {
                itemsToApply =
                    itemContext.GetCollection<CacheEventContainer>()
                    .AsQueryable()
                    .OrderBy(item => item.Id)
                    .ToList();
            }

            if (snapShot == null) { snapShot = new AllOrdersSnapshot(); }

            if (itemsToApply == null || !itemsToApply.Any()) { return; }

            foreach (var item in itemsToApply)
            {
                snapShot.ApplyEvent(item);
            }

            snapShot.Id = default(ObjectId);
            snapShotContext.Insert(snapShot);

            var collection = snapShotContext.GetCollection<BsonDocument>();
            var filter = Builders<BsonDocument>.Filter.Lt("_id", snapShot.Id);

            collection.DeleteMany(filter);
        }

        protected abstract CommodityMap Map { get; }

        protected abstract ThrottleContext ThrottleContext { get; }

        protected virtual TimeSpan OrderBookCacheThreshold { get { return TimeSpan.FromMinutes(10); } }

        protected MongoCollectionContext GetOrderBookContext()
        {
            return new MongoCollectionContext(DbContext, $"{CollectionPrefix}--order-book");
        }

        private IMongoCollectionContext GetAllOrderBooksCollectionContext()
        {
            return new MongoCollectionContext(DbContext, $"{CollectionPrefix}--all-order-books");
        }
    }
}
