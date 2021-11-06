using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;

namespace cache_lib.Models.Snapshots
{
    public abstract class Snapshot<TSnapshotItem> : ISnapshot<TSnapshotItem>
        where TSnapshotItem : ISnapshotItem
    {
        public ObjectId Id { get; set; }
        public ObjectId LastId { get; set; }
        public Dictionary<string, TSnapshotItem> SnapshotItems { get; set; }

        public void ApplyEvent(CacheEventContainer cacheEventContainer)
        {
            var snapShotItem = ToSnapshotItem(cacheEventContainer);
            if (snapShotItem == null) { return; }

            (SnapshotItems ?? (SnapshotItems = new Dictionary<string, TSnapshotItem>()))
                [cacheEventContainer.CacheKey] = snapShotItem;

            // Remove any bad items that may have weaseled their way in.
            var keys = SnapshotItems.Keys.ToList();
            foreach (var key in keys)
            {
                if (!string.Equals(key, key.ToUpper(), StringComparison.Ordinal))
                {
                    SnapshotItems.Remove(key);
                }
            }

            LastId = cacheEventContainer.Id;
        }

        protected abstract TSnapshotItem ToSnapshotItem(CacheEventContainer cacheEventContainer);
    }
}
