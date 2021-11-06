using cache_lib.Models;
using cache_lib.Models.Snapshots;

namespace cache_model.Snapshots
{
    public class OpenOrdersSnapshot : Snapshot<OpenOrderSnapshotItem>
    {
        protected override OpenOrderSnapshotItem ToSnapshotItem(CacheEventContainer cacheEventContainer)
        {
            if (cacheEventContainer == null) { return null; }

            var pieces = cacheEventContainer.CacheKey.Split('-');
            if (pieces == null || pieces.Length != 2
                || string.IsNullOrWhiteSpace(pieces[0]) || string.IsNullOrWhiteSpace(pieces[1]))
            { return null; }

            var symbol = pieces[0].Trim().ToUpper();
            var baseSymbol = pieces[1].Trim().ToUpper();

            return new OpenOrderSnapshotItem
            {
                Id = cacheEventContainer.Id,
                AsOfUtc = cacheEventContainer.EndTimeUtc,
                Symbol = symbol,
                BaseSymbol = baseSymbol,
                Raw = cacheEventContainer.Raw,
                CacheKey = cacheEventContainer.CacheKey
            };
        }
    }
}
