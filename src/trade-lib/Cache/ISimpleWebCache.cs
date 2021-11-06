using System;

namespace trade_lib.Cache
{
    public interface ISimpleWebCache
    {
        string Post(string url, string data, bool forceRefresh = false);
        string Get(string url, bool forceRefresh = false);
        string Get(string url, Func<string, bool> validator, bool forceRefresh = false);
        T Get<T>(Func<T> getter, string key, Func<T, bool> validator, bool forceRefresh = false, TimeSpan? lifeSpan = null);
        T GetEx<T>(Func<T> getter, string key, Func<T, bool> validator, bool forceRefresh, TimeSpan? lifeSpan, bool shouldAlwaysUseCache);
        void RefreshIfCloseToExpiring(string url, TimeSpan? lifeSpan = null);
    }
}
