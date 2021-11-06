//using cache_lib.Models;
//using MongoDB.Bson;
//using System.Collections.Generic;

//namespace kucoin_lib.Models.Snapshots
//{
//    public abstract class Snapshot<TSnapshotItem> : ISnapshot<TSnapshotItem>
//        where TSnapshotItem : ISnapshotItem
//    {
//        public ObjectId Id { get; set; }
//        public ObjectId LastId { get; set; }
//        public Dictionary<string, TSnapshotItem> SnapshotItems { get; set; }

//        public void ApplyEvent(CacheEventContainer cacheEventContainer)
//        {
//            var snapShotItem = ToSnapshotItem(cacheEventContainer);
//            if (snapShotItem == null) { return; }

//            (SnapshotItems ?? (SnapshotItems = new Dictionary<string, TSnapshotItem>()))
//                [cacheEventContainer.CacheKey] = snapShotItem;

//            LastId = cacheEventContainer.Id;
//        }

//        protected abstract TSnapshotItem ToSnapshotItem(CacheEventContainer cacheEventContainer);
//    }
//}
