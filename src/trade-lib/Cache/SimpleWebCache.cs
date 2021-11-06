using mongo_lib;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using web_util;

namespace trade_lib.Cache
{
    [Obsolete]
    public class SimpleWebCache : ISimpleWebCache
    {
        private static readonly TimeSpan _defaultThreshold = TimeSpan.FromMinutes(30);
        private static readonly Random _random = new Random();

        private static readonly TimeSpan ThrottleTimeSpan = TimeSpan.FromSeconds(1);

        private class DateTimeContainer
        {
            public DateTime? DateTimeUtc { get; set; }
        }

        private static readonly object ThrottleDictionaryLocker = new object();
        private static readonly Dictionary<string, object> _throttleDictionary = new Dictionary<string, object>();
        private static readonly Dictionary<string, DateTimeContainer> _callTimeDictionary = new Dictionary<string, DateTimeContainer>();
        private static readonly Dictionary<string, bool> _hasCheckedIndexDictionary = new Dictionary<string, bool>();
        private static readonly Dictionary<string, DateTimeContainer> _lastCullingDictionary = new Dictionary<string, DateTimeContainer>();

        private readonly IWebUtil _webUtil;
        private readonly IMongoCollectionContext _store;
        private readonly string _throttleKey;        
        private readonly object _locker;
        private readonly DateTimeContainer _callTimeContainer;
        private readonly DateTimeContainer _lastCullingTimeContainer;

        public SimpleWebCache(
            IWebUtil webUtil,
            IMongoCollectionContext store,
            string throttleKey)
        {
            _webUtil = webUtil;
            _store = store;
            _throttleKey = throttleKey;

            lock (ThrottleDictionaryLocker)
            {
                _locker = _throttleDictionary.ContainsKey(throttleKey)
                    ? _throttleDictionary[throttleKey]
                    : (_throttleDictionary[throttleKey] = new object());

                _callTimeContainer = _callTimeDictionary.ContainsKey(throttleKey)
                    ? _callTimeDictionary[throttleKey]
                    : (_callTimeDictionary[throttleKey] = new DateTimeContainer());

                _lastCullingTimeContainer = _lastCullingDictionary.ContainsKey(throttleKey)
                    ? _lastCullingDictionary[throttleKey]
                    : (_lastCullingDictionary[throttleKey] = new DateTimeContainer());
            }
        }

        private void CreateIndexIfNecessary()
        {
            if (_hasCheckedIndexDictionary.ContainsKey(_throttleKey) && _hasCheckedIndexDictionary[_throttleKey])
            {
                return;
            }

            lock (_locker)
            {
                if (_hasCheckedIndexDictionary.ContainsKey(_throttleKey) && _hasCheckedIndexDictionary[_throttleKey])
                {
                    return;
                }

                var collection = GetCollection();

                collection
                    .Indexes.CreateOne(Builders<SimpleWebCacheEntity>
                    .IndexKeys.Ascending(_ => _.Url));

                collection
                    .Indexes.CreateOne(Builders<SimpleWebCacheEntity>
                    .IndexKeys.Ascending(_ => _.Group));

                _hasCheckedIndexDictionary[_throttleKey] = true;
            }
        }

        public void RefreshIfCloseToExpiring(string url, TimeSpan? lifeSpan = null)
        {
            var effectiveThreshold =
                lifeSpan.HasValue && lifeSpan.Value > TimeSpan.Zero
                ? lifeSpan.Value
                : _defaultThreshold;

            var closeLowerThreshold = GetCloseLowerThreshold(effectiveThreshold);
            var closeUpperThreshold = GetCloseUpperThreshold(effectiveThreshold);

            var collection = GetCollection();

            var mostRecent = collection.AsQueryable()
                .OrderByDescending(item => item.Id)
                .Where(item => item.Url == url)
                .FirstOrDefault();

            if (mostRecent != null)
            {
                var timeSince = DateTime.UtcNow - mostRecent.RequestTimeUtc;
                if (timeSince < closeLowerThreshold) { return; }

                // throw in a bit of randomness to ensure that everything doesn't need to refresh at the same time.
                var range = closeUpperThreshold - closeLowerThreshold;
                var medianThreshold = TimeSpan.FromTicks((long)(closeLowerThreshold.Ticks + range.Ticks * _random.NextDouble()));
                if(timeSince < medianThreshold) { return; }
            }

            Get(url, true);
        }

        public string Get(string url, bool forceRefresh = false)
        {
            return Get(url, (contents) => true, forceRefresh);
        }

        public T GetEx<T>(Func<T> getter, string key, Func<T, bool> validator, bool forceRefresh,
            TimeSpan? maxLifeSpan, bool shouldAlwaysUseCache)
        {
            var effectiveThreshold =
                maxLifeSpan.HasValue && maxLifeSpan.Value > TimeSpan.Zero
                ? maxLifeSpan.Value
                : _defaultThreshold;

            var effectiveCloseUpperThreshold = TimeSpan.FromTicks((long)(0.8d * effectiveThreshold.Ticks));
            var effectiveCloseLowerThreshold = TimeSpan.FromTicks((long)(0.65d * effectiveThreshold.Ticks));

            var effectiveValidator = validator ?? new Func<T, bool>(data => true);

            CreateIndexIfNecessary();

            var collection = GetCollection();

            var mostRecent = collection.AsQueryable()
                .OrderByDescending(item => item.Id)
                .Where(item => item.Url == key)
                .FirstOrDefault();

            T value = default(T);
            bool shouldReturnValue = false;
            if (mostRecent != null && !forceRefresh)
            {
                var timeSince = DateTime.UtcNow - mostRecent.RequestTimeUtc;
                var isWithinThreshold = timeSince < effectiveThreshold;
                if (timeSince < effectiveThreshold || shouldAlwaysUseCache)
                {
                    try
                    {
                        value = !string.IsNullOrWhiteSpace(mostRecent.Contents)
                            ? JsonConvert.DeserializeObject<T>(mostRecent.Contents)
                            : default(T);

                        if (effectiveValidator(value))
                        {
                            if (isWithinThreshold)
                            {
                                return value;
                            }

                            shouldReturnValue = true;
                        }
                    }
                    catch (Exception exception)
                    {
                        Console.WriteLine(exception);
                        shouldReturnValue = false;
                    }
                }
            }

            var task = Task.Run(() =>
            {
                var requestTime = DateTime.UtcNow;
                var result = Throttle(() => getter(), effectiveValidator);
                if (!effectiveValidator(result))
                {
                    throw new InvalidResponseException(
                        new StringBuilder()
                        .AppendLine("Validator indicated that result was invalid.")
                        .AppendLine($"Url: {key}")
                        .AppendLine("Contents:")
                        .AppendLine(result != null && !(result is string && string.IsNullOrWhiteSpace(result as string))
                            ? JsonConvert.SerializeObject(result, Formatting.Indented)
                            : "(null)")
                        .ToString());
                }

                var responseTime = DateTime.UtcNow;

                var serializedContents = result != null
                    ? JsonConvert.SerializeObject(result, Formatting.Indented)
                    : null;

                var entity = new SimpleWebCacheEntity
                {
                    RequestTimeUtc = requestTime,
                    ResponseTimeUtc = responseTime,
                    Url = key,
                    UpperUrl = key?.ToUpper(),
                    Contents = serializedContents,
                    Group = _throttleKey
                };

                collection.InsertOne(entity);
                Culling(collection, effectiveThreshold);

                return result;
            });

            if (shouldReturnValue) { return value; }

            return task.Result;
        }

        public string Post(string url, string data, bool forceRefresh = false)
        {
            var key = url +"_" + (data ?? string.Empty);
            var getter = new Func<string>(() => _webUtil.Post(url, data));
            var validator = new Func<string, bool>((contents) => true);
            return Get(getter, key, validator, forceRefresh);
        }

        public string Get(string url, Func<string, bool> validator, bool forceRefresh = false)
        {
            var getter = new Func<string>(() => _webUtil.Get(url));
            return Get(getter, url, validator, forceRefresh);
        }

        private void Culling(IMongoCollection<SimpleWebCacheEntity> collection, TimeSpan threshold)
        {
            var cullingThreshold = TimeSpan.FromTicks(2L * threshold.Ticks);

            if (_lastCullingTimeContainer.DateTimeUtc.HasValue)
            {
                var timeSince = DateTime.UtcNow - _lastCullingTimeContainer.DateTimeUtc;
                if (timeSince < cullingThreshold) { return; }
            }

            lock (_locker)
            {
                if (_lastCullingTimeContainer.DateTimeUtc.HasValue)
                {
                    var timeSince = DateTime.UtcNow - _lastCullingTimeContainer.DateTimeUtc;
                    if (timeSince < cullingThreshold) { return; }
                }

                collection.DeleteMany(Builders<SimpleWebCacheEntity>.Filter.Lt("RequestTimeUtc", DateTime.UtcNow.AddDays(-1)));

                _lastCullingTimeContainer.DateTimeUtc = DateTime.UtcNow;
            }
        }

        private T Throttle<T>(Func<T> method, Func<T, bool> validator)
        {
            lock (_locker)
            {
                if (_callTimeContainer.DateTimeUtc.HasValue)
                {
                    var timeSince = DateTime.UtcNow - _callTimeContainer.DateTimeUtc.Value;
                    var remainingTime = ThrottleTimeSpan - timeSince;
                    if (remainingTime > TimeSpan.Zero)
                    {
                        Thread.Sleep(remainingTime);
                    }
                }

                _callTimeContainer.DateTimeUtc = DateTime.UtcNow;
                var result = method();
                var isValid = validator(result);
                if (isValid) { return result; }

                var extraSleep = TimeSpan.FromTicks(4L * ThrottleTimeSpan.Ticks);
                Thread.Sleep(extraSleep);

                return method();
            }
        }

        private IMongoCollection<SimpleWebCacheEntity> GetCollection()
        {
            return _store.GetCollection<SimpleWebCacheEntity>();
        }
        
        private TimeSpan GetCloseUpperThreshold(TimeSpan threshold)
        {
            return TimeSpan.FromTicks((long)(0.8d * threshold.Ticks));
        }

        private TimeSpan GetCloseLowerThreshold(TimeSpan threshold)
        {
            return TimeSpan.FromTicks((long)(0.65d * threshold.Ticks));
        }

        public T Get<T>(Func<T> getter, string key, Func<T, bool> validator, bool forceRefresh = false, TimeSpan? lifeSpan = null)
        {
            return GetEx(getter, key, validator, forceRefresh, lifeSpan, false);
        }
    }
}
