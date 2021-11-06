using cache_lib.Models;
using mongo_lib;
using System;

namespace cache_lib
{
    public interface ICacheUtil
    {
        CacheResult GetCacheableEx(
            ThrottleContext throttleContext,
            Func<string> retriever,
            IMongoCollectionContext collectionContext,
            TimeSpan threshold,
            CachePolicy cachePolicy,
            Func<string, bool> validator = null,
            Action<CacheEventContainer> afterInsert = null,
            string key = null);
    }
}
