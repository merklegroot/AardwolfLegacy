using cache_lib.Models;
using trade_model;

namespace token_balance_lib
{
    public interface ITokenBalanceIntegration
    {
        decimal GetTokenBalance(string walletAddress, string contract, CachePolicy cachePolicy);
    }
}
