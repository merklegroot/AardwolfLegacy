namespace cache_lib.Models
{
    public enum CachePolicy
    {
        Unknown = 0,
        AllowCache = 1,
        OnlyUseCache = 2,
        ForceRefresh = 3,
        OnlyUseCacheUnlessEmpty = 4,
        PreemptCache = 5
    }
}
