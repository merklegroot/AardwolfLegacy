using cache_lib.Models;
using mongo_lib;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace cache_lib
{
    public class CacheUtil : ICacheUtil
    {
        public CacheResult GetCacheableEx(
            ThrottleContext throttleContext,
            Func<string> retriever,
            IMongoCollectionContext collectionContext,
            TimeSpan threshold,
            CachePolicy cachePolicy,
            Func<string, bool> validator = null,
            Action<CacheEventContainer> afterInsert = null,
            string originalKey = null)
        {
            var effectiveThreshold = cachePolicy == CachePolicy.PreemptCache
                ? TimeSpan.FromTicks(threshold.Ticks / 2L)
                : threshold;

            var key = originalKey != null
                ? originalKey.ToUpper()
                : originalKey;

            var currentTime = DateTime.UtcNow;

            var bsonCollection = collectionContext.GetCollection<BsonDocument>();
            var cacheCollection = collectionContext.GetCollection<CacheEventContainer>();

            var bsonResult =
                cachePolicy != CachePolicy.ForceRefresh
                ? ((!string.IsNullOrWhiteSpace(key))
                    ? bsonCollection.Find(item => item["cacheKey"] == key).SortByDescending(item => item["_id"]).FirstOrDefault()
                    : bsonCollection.Find(item => true).SortByDescending(item => item["_id"]).FirstOrDefault())
                : null;
            
            CacheEventContainer databaseResult = null;
            try
            {
                if (bsonResult != null) { databaseResult = BsonSerializer.Deserialize<CacheEventContainer>(bsonResult); }
            }
            catch
            {
                databaseResult = null;
            }

            if (cachePolicy == CachePolicy.OnlyUseCache ||
                (cachePolicy == CachePolicy.OnlyUseCacheUnlessEmpty && bsonResult != null))
            {
                return new CacheResult
                {
                    Id = databaseResult?.Id,
                    Contents = databaseResult?.Raw,
                    AsOf = databaseResult?.EndTimeUtc,
                    CacheAge = databaseResult != null ? currentTime - databaseResult.StartTimeUtc : (TimeSpan?)null,
                    WasFromCache = true
                };
            }

            TimeSpan? cacheAge = null;
            if (databaseResult != null && !string.IsNullOrWhiteSpace(databaseResult.Raw))
            {
                bool isValid;

                try
                {
                    isValid = validator?.Invoke(databaseResult.Raw) ?? true;
                }
                catch
                {
                    isValid = false;
                }

                if (isValid)
                {
                    cacheAge = currentTime - databaseResult.StartTimeUtc;
                    if (cacheAge <= effectiveThreshold)
                    {
                        return new CacheResult
                        {
                            Id = databaseResult.Id,
                            AsOf = databaseResult.EndTimeUtc,
                            Contents = databaseResult.Raw,
                            CacheAge = cacheAge,
                            WasFromCache = true
                        };
                    }
                }
            }

            var response = Throttle(throttleContext, retriever);

            bool passedValidation = false;
            try
            {
                passedValidation = validator != null
                    ? validator(response.Data)
                    : !string.IsNullOrWhiteSpace(response.Data);
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception.Message);
                passedValidation = false;
            }

            CacheEventContainer ec = null;
            if (passedValidation)
            {
                ec = new CacheEventContainer
                {
                    StartTimeUtc = response.StartTime,
                    EndTimeUtc = response.EndTime,
                    Raw = response.Data,
                    CacheKey = key?.ToUpper()
                };

                cacheCollection.InsertOne(ec);
                afterInsert?.Invoke(ec);
            }

            return new CacheResult
            {
                Id = ec?.Id,
                AsOf = response.EndTime,
                Contents = response.Data,
                CacheAge = cacheAge,
                WasFromCache = false
            };
        }

        private GetterResult<T> Throttle<T>(ThrottleContext throttleContext, Func<T> getter)
        {
            Task<GetterResult<T>> getterTask;
            lock (throttleContext.Locker)
            {
                if (throttleContext.LastReadTime.HasValue)
                {
                    var remainigTime = throttleContext.ThrottleThreshold - (DateTime.UtcNow - throttleContext.LastReadTime.Value);
                    if (remainigTime > TimeSpan.Zero)
                    {
                        Thread.Sleep(remainigTime);
                    }
                }

                throttleContext.LastReadTime = DateTime.UtcNow;

                getterTask = new Task<GetterResult<T>>(() =>
                {
                    var startTime = DateTime.UtcNow;
                    var data = getter();
                    var endTime = DateTime.UtcNow;

                    return new GetterResult<T>
                    {
                        StartTime = startTime,
                        Data = data,
                        EndTime = endTime
                    };
                }, TaskCreationOptions.LongRunning);
                getterTask.Start();

                getterTask.Wait(TimeSpan.FromSeconds(2.5));
            }

            return getterTask.Result;
        }       
    }
}
