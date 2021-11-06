using cache_lib.Models;

namespace coss_agent_lib.Strategy
{
    public interface ICossAutoOpenBid
    {
        void Execute(CachePolicy cachePolicy = CachePolicy.ForceRefresh);
    }
}
