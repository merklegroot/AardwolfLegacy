using client_lib.Models;
using System;
using trade_constants;
using trade_contracts.Messages;

namespace client_lib
{
    public interface IPingClient
    {
        PongInfo Ping(string serviceId);
    }

    public class PingClient : IPingClient
    {
        private readonly IRequestResponse _requestResponse;

        public PingClient(IRequestResponse requestResponse)
        {
            _requestResponse = requestResponse;
        }

        public PongInfo Ping(string serviceId)
        {
            var service = GetServiceById(serviceId);
            if (service == null) { throw new ApplicationException($"Unable to resolve service by id \"{serviceId}\"."); }
            var queueName = GetPingQueueNameForService(serviceId);
            if (string.IsNullOrWhiteSpace(queueName)) { throw new ApplicationException($"Queue name for service \"{serviceId}\" is null or whitespace."); }

            var req = new PingMessage();
            var response = _requestResponse.Execute<PingMessage, PongMessage>(req, queueName);

            return new PongInfo
            {
                ApplicationName = response?.ApplicationName,
                ApplicationVersion = response?.ApplicationVersion,
                MachineName = response?.MachineName,
                ProcessName = response?.ProcessName,
                BuildDate = response?.BuildDate
            };
        }

        private string GetPingQueueNameForService(string serviceId)
        {
            var serviceDef = GetServiceById(serviceId);

            if (serviceDef == null)
            {
                throw new ApplicationException($"Unable to resolve service by id \"{serviceId}\".");
            }

            return serviceDef.Queue;
        }

        private ServiceDef GetServiceById(string serviceId) => ServiceRes.Dictionary.ContainsKey(serviceId)
                ? ServiceRes.Dictionary[serviceId]
                : null;
    }
}
