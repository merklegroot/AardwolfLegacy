using System;
using System.Collections.Generic;

namespace cache_lib.Models
{
    public class MemCacheDictionary<T> : Dictionary<string, MemCacheItem<T>>
            where T : class
    {
        private static TimeSpan DefaultThreshold = TimeSpan.FromMinutes(10);

        private readonly TimeSpan _threshold;

        private readonly Func<T, bool> _validator;

        public MemCacheDictionary() : this(DefaultThreshold) { }

        public MemCacheDictionary(TimeSpan threshold) : this(null, threshold) { }

        public MemCacheDictionary(Func<T, bool> validator) : this(validator, DefaultThreshold) { }

        public MemCacheDictionary(Func<T, bool> validator, TimeSpan threshold)
             : base(StringComparer.InvariantCultureIgnoreCase)
        {
            _validator = validator;
            _threshold = threshold;
        }

        public T Get(string key, CachePolicy cachePolicy)
        {
            if (string.IsNullOrWhiteSpace(key)) { throw new ArgumentNullException(nameof(key)); }
            if (!ContainsKey(key)) { return default(T); }

            var item = this[key];
            if (item.Value == null) { return default(T); }
            if (!item.TimeStampUtc.HasValue) { return default(T); }

            var isValid = _validator != null ? _validator(item.Value) : item.Value != null;
            if (!isValid) { return default(T); }

            var timeSince = DateTime.UtcNow - item.TimeStampUtc.Value;
            if (timeSince <= _threshold)
            {
                return item.Value;
            }

            return default(T);
        }

        public void Set(string key, T value)
        {
            this[key] = new MemCacheItem<T>
            {
                TimeStampUtc = DateTime.UtcNow,
                Value = value
            };
        }
    }
}
