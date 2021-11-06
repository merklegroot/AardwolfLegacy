//using cache_lib.Models;
//using MongoDB.Bson;
//using System.Collections.Generic;

//namespace kucoin_lib.Models.Snapshots
//{
//    public interface ISnapshot<TSnapshotItem>
//        where TSnapshotItem : ISnapshotItem
//    {
//        ObjectId Id { get; set; }
//        ObjectId LastId { get; set; }

//        Dictionary<string, TSnapshotItem> SnapshotItems { get; set; }
        
//        void ApplyEvent(CacheEventContainer cacheEventContainer);
//    }
//}
