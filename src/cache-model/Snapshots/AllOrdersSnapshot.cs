namespace cache_lib.Models.Snapshots
{
    public class AllOrdersSnapshot : Snapshot<OrderBookSnapshotItem>
    {
        protected override OrderBookSnapshotItem ToSnapshotItem(CacheEventContainer cacheEventContainer)
        {
            if (cacheEventContainer == null) { return null; }

            var pieces = cacheEventContainer.CacheKey.Split('-');
            if (pieces == null || pieces.Length != 2
                || string.IsNullOrWhiteSpace(pieces[0]) || string.IsNullOrWhiteSpace(pieces[1]))
            { return null; }

            var symbol = pieces[0].Trim().ToUpper();
            var baseSymbol = pieces[1].Trim().ToUpper();

            return new OrderBookSnapshotItem
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
