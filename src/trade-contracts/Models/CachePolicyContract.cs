namespace trade_contracts
{
    public enum CachePolicyContract
    {
        Unknown = 0,
        AllowCache = 1,
        OnlyUseCache = 2,
        ForceRefresh = 3,
        OnlyUseCacheUnlessEmpty = 4,
        // PreemptCache = 5
    }
}
