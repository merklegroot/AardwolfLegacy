using cache_lib.Models;
using MongoDB.Bson;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace coss_data_model
{
    public class CossUserExchangeHistorySnapshot
    {
        public ObjectId Id { get; set; }
        public ObjectId LastId { get; set; }
        public DateTime TimeStampUtc { get; set; }
        public DateTime LastEventTimeStampUtc { get; set; }

        public List<CossExchangeHistoryItem> Items { get; set; } = new List<CossExchangeHistoryItem>();

        // This isn't thread safe, but it would be unusual
        // to have multiple threads altering the in-memory collection.
        public void Merge(CacheEventContainer container)
        {
            if (container == null) { return; }
            if (string.IsNullOrWhiteSpace(container.Raw)) { return; }
            if (container.Id < LastId) { return; }

            var response = JsonConvert.DeserializeObject<CossUserExchangeHistoryResponse>(container.Raw);
            if (response?.payload?.actions?.items == null
                || !response.successful) { return; }

            var clonedExistingItems = 
                (Items ?? new List<CossExchangeHistoryItem>())
                .Select(item => item.Clone()).ToList();

            var cloneDictionary = new Dictionary<string, CossExchangeHistoryItem>(StringComparer.InvariantCultureIgnoreCase);
            foreach(var item in (Items ?? new List<CossExchangeHistoryItem>()))
            {
                if (string.IsNullOrWhiteSpace(item?.id)) { continue; }
                cloneDictionary[item.id.Trim().ToUpper()] = item;
            }

            var props = typeof(CossExchangeHistoryItem).GetProperties(BindingFlags.Public | BindingFlags.Instance).ToList();

            foreach (var item in response?.payload?.actions?.items)
            {
                if (string.IsNullOrWhiteSpace(item?.id)) { continue; }
                var trimmedUpperId = item.id.Trim().ToUpper();
                var match = cloneDictionary.ContainsKey(trimmedUpperId)
                    ? cloneDictionary[trimmedUpperId]
                    : null;

                if (match != null)
                {
                    // just in case they decided to change history...
                    var itemType = item.GetType();
                    foreach (var prop in props)
                    {
                        prop.SetValue(match, prop.GetValue(item));
                    }

                    continue;
                }

                clonedExistingItems.Add(item);
            }

            Items = clonedExistingItems;
            LastId = container.Id;
        }
    }
}
