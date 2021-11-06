using System;
using cache_lib.Models;
using client_lib.Models;
using trade_contracts;
using trade_contracts.Messages;

namespace client_lib
{
    public abstract class ServiceClient : IServiceClient
    {
        private readonly IRequestResponse _requestResponse;

        public ServiceClient(IRequestResponse requestResponse)
        {
            _requestResponse = requestResponse;
        }

        protected virtual IRequestResponse RequestResponse => _requestResponse;

        protected abstract string QueueName { get; }

        protected virtual string VersionedQueue(int version) =>
            string.IsNullOrWhiteSpace(_overriddenQueue)
            ? QueueName + (version > 0 ? $"-v{version}" : string.Empty)
            : _overriddenQueue;

        private string _overriddenQueue = null;
        public virtual void OverrideQueue(string queue)
        {
            _overriddenQueue = queue;
        }

        public virtual void OverrideTimeout(TimeSpan timeout)
        {
            _requestResponse.OverrideTimeout(timeout);
        }

        protected CachePolicyContract ToContract(CachePolicy cachePolicy)
        {
            return (CachePolicyContract)cachePolicy;
        }

        public PongInfo Ping()
        {
            var req = new PingMessage();
            var response = _requestResponse.Execute<PingMessage, PongMessage>(req, VersionedQueue(1));

            return new PongInfo
            {
                ApplicationName = response?.ApplicationName,
                ApplicationVersion = response?.ApplicationVersion,
                MachineName = response?.MachineName,
                ProcessName = response?.ProcessName,
                BuildDate = response?.BuildDate
            };
        }

        public void UseDefaultConfigKey()
        {
            _requestResponse.UseDefaultConfigKey();
        }

        public void OverrideConfigKey(string configKey)
        {
            _requestResponse.OverrideConfigKey(configKey);
        }
    }
}
