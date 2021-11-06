using MongoDB.Bson;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace trade_lib
{
    public class EventContainer
    {
        public ObjectId Id { get; set; }

        [JsonProperty("startTimeUtc")]
        public DateTime StartTimeUtc { get; set; }

        [JsonProperty("endTimeUtc")]
        public DateTime EndTimeUtc { get; set; }
    }

    public class EventContainer<TData> : EventContainer
    {        
        /// TODO: Consider constaining Data to be ICloneable.
        [JsonProperty("data")]
        public TData Data { get; set; }

        [JsonProperty("raw")]
        public string Raw { get; set; }

        /// <summary>
        /// This doesn't cover cloning Data unless TData is a value type.
        /// </summary>
        public virtual EventContainer<TData> CloneShallow()
        {
            var clone = (EventContainer<TData>)MemberwiseClone();
            return clone;
        }
    }

    public class WebRequestContext
    {
        private string _url;
        public string Url
        {
            get { return _url; }
            set { _url = value; UpdateSearchableContext(); }
        }

        private string _verb;
        public string Verb
        {
            get { return _verb; }
            set { _verb = value; UpdateSearchableContext(); }
        }

        private string _payload;
        public string Payload
        {
            get { return _payload; }
            set { _payload = value; UpdateSearchableContext(); }
        }

        private void UpdateSearchableContext()
        {
            var nullerator = new Func<string, string>(text => string.IsNullOrWhiteSpace(text) ? "(null)" : text.Trim());
            SearchableContext = $"{nullerator(Url)}_{nullerator(Verb)}_{nullerator(Payload)}";
        }

        public string SearchableContext { get; set; }

        public List<KeyValuePair<string, string>> Headers { get; set; } = new List<KeyValuePair<string, string>>();
    }

    public class WebRequestEventContainer : EventContainer
    {
        public WebRequestContext Context { get; set; }

        public string Raw { get; set; }
    }

    public class EventContainerWithContext<TContext, TData>
    {
        public ObjectId Id { get; set; }

        [JsonProperty("startTimeUtc")]
        public DateTime StartTimeUtc { get; set; }

        [JsonProperty("endTimeUtc")]
        public DateTime EndTimeUtc { get; set; }

        [JsonProperty("context")]
        public TContext Context { get; set; }

        [JsonProperty("data")]
        public TData Data { get; set; }

        [JsonProperty("raw")]
        public string Raw { get; set; }
    }
}
