using cache_lib.Models;
using System;
using System.Linq;

namespace trade_api.Utils
{
    public class CachePolicyParser
    {
        public static CachePolicy? ParseCachePolicy(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) { return null; }

            var enumType = typeof(CachePolicy);
            var cachePolicyText = text.Trim();
            var names = Enum.GetNames(enumType);
            var matchingName = names.SingleOrDefault(queryName => string.Equals(queryName, cachePolicyText, StringComparison.InvariantCultureIgnoreCase));

            return Enum.TryParse(matchingName, out CachePolicy parsedValue)
                ? parsedValue
                : (CachePolicy?)null;
        }

        public static CachePolicy ParseCachePolicy(string text, CachePolicy defaultCachePolicy)
        {
            return ParseCachePolicy(text, false, defaultCachePolicy);
        }

        public static CachePolicy ParseCachePolicy(string text, bool forceRefresh, CachePolicy defaultCachePolicy)
        {
            var parsedValue = ParseCachePolicy(text);
            if (parsedValue.HasValue) { return parsedValue.Value; }
            if (forceRefresh) { return CachePolicy.ForceRefresh; }

            return defaultCachePolicy;
        }
    }
}