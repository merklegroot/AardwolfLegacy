//using cache_lib.Models;

//namespace kucoin_lib.Models.Snapshots
//{
//    public class AllDepositAddressesSnapShot : Snapshot<DepositAddressSnapshotItem>
//    {
//        protected override DepositAddressSnapshotItem ToSnapshotItem(CacheEventContainer cacheEventContainer)
//        {
//            if (cacheEventContainer == null) { return null; }

//            return new DepositAddressSnapshotItem
//            {
//                Id = cacheEventContainer.Id,
//                AsOfUtc = cacheEventContainer.EndTimeUtc,
//                CacheKey = cacheEventContainer.CacheKey,
//                Raw = cacheEventContainer.Raw
//            };
//        }
//    }
//}
