using cache_lib.Models;
using cache_lib.Models.Snapshots;
using Newtonsoft.Json;

namespace coss_lib.Models
{
    public class CossNativeUserTradeHistorySnapshot : Snapshot<CossNativeUserTradeHistorySnapshotItem>
    {
        protected override CossNativeUserTradeHistorySnapshotItem ToSnapshotItem(CacheEventContainer cacheEventContainer)
        {
            var json = JsonConvert.SerializeObject(cacheEventContainer);

            var pieces = cacheEventContainer.CacheKey.Split('-');
            var symbol = pieces[0];
            var baseSymbol = pieces[1];
            
            var snapshotItem = new CossNativeUserTradeHistorySnapshotItem
            {
                AsOfUtc = cacheEventContainer.StartTimeUtc,
                CacheKey = cacheEventContainer.CacheKey,
                Id = cacheEventContainer.Id,
                Raw = cacheEventContainer.Raw,
                Symbol = symbol,
                BaseSymbol = baseSymbol
            };

            return snapshotItem;
        }
    }
}
