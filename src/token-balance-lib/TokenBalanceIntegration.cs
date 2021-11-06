using Newtonsoft.Json;
using System;
using web_util;
using token_balance_lib.Models;
using cache_lib.Models;
using mongo_lib;
using config_connection_string_lib;
using cache_lib;

namespace token_balance_lib
{
    public class TokenBalanceIntegration : ITokenBalanceIntegration
    {
        private const string DatabaseName = "token-balance";

        private readonly IGetConnectionString _getConnectionString;
        private readonly IWebUtil _webUtil;
        private readonly CacheUtil _cacheUtil;

        private static ThrottleContext ThrottleContext = new ThrottleContext
        {
            Locker = new object(),
            ThrottleThreshold = TimeSpan.FromSeconds(2.5)
        };

        private static readonly TimeSpan GetTokenBalanceThreshold = TimeSpan.FromMinutes(5);

        public TokenBalanceIntegration(IWebUtil webUtil, IGetConnectionString getConnectionString)
        {
            _webUtil = webUtil;
            _getConnectionString = getConnectionString;
            _cacheUtil = new CacheUtil();
        }

        public decimal GetTokenBalance(
            string walletAddress,
            string contract,
            CachePolicy cachePolicy)
        {
            if (string.IsNullOrWhiteSpace(walletAddress)) { throw new ArgumentNullException(nameof(walletAddress)); }
            if (string.IsNullOrWhiteSpace(contract)) { throw new ArgumentNullException(nameof(contract)); }

            var url = $"https://api.tokenbalance.com/token/{contract}/{walletAddress}";

            var retriever = new Func<string>(() => _webUtil.Get(url));
            var translator = new Func<string, TokenBalanceResponse>(responseText =>
                !string.IsNullOrWhiteSpace(responseText)
                    ? JsonConvert.DeserializeObject<TokenBalanceResponse>(responseText)
                    : null
            );

            var validator = new Func<string, bool>(text =>
            {
                if(string.IsNullOrWhiteSpace(text)) { return false; }
                var translated = translator(text);
                if (translated == null) { return false; }

                return true;
            });

            var collectionName = $"token-balance--{contract}--{walletAddress}";
            var collectionContext = new MongoCollectionContext(_getConnectionString.GetConnectionString(), DatabaseName, collectionName);

            var cacheResult = _cacheUtil.GetCacheableEx(ThrottleContext, retriever, collectionContext, GetTokenBalanceThreshold, cachePolicy, validator);

            var model = translator(cacheResult?.Contents);
            if (model == null) { throw new ApplicationException($"Failed to retrieve token balance for contract {contract} for wallet {walletAddress}."); }

            return model.Balance;
        }
    }
}
