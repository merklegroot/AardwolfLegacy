using cache_lib.Models;
using cache_model.Snapshots;
using mongo_lib;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Linq;

namespace trade_lib.Repo
{
    public interface IOpenOrdersSnapshotRepo
    {
        void AfterInsertOpenOrders(
            IMongoCollectionContext openOrdersContext,
            IMongoCollectionContext snapShotContext,
            CacheEventContainer cacheEventContainer);
    }

    public class OpenOrdersSnapshotRepo : IOpenOrdersSnapshotRepo
    {
        public void AfterInsertOpenOrders(
            IMongoCollectionContext openOrdersContext,
            IMongoCollectionContext snapShotContext,
            CacheEventContainer cacheEventContainer)
        {
            var snapShot = snapShotContext
                .GetLast<OpenOrdersSnapshot>();

            List<CacheEventContainer> itemsToApply = null;
            if (snapShot != null)
            {
                itemsToApply =
                    openOrdersContext.GetCollection<CacheEventContainer>()
                    .AsQueryable()
                    .Where(item => item.Id > snapShot.LastId)
                    .OrderBy(item => item.Id)
                    .ToList();
            }
            else
            {
                itemsToApply =
                    openOrdersContext.GetCollection<CacheEventContainer>()
                    .AsQueryable()
                    .OrderBy(item => item.Id)
                    .ToList();
            }

            if (snapShot == null) { snapShot = new OpenOrdersSnapshot(); }

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

    }
}
