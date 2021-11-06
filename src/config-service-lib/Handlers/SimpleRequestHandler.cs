using System;
using System.Collections.Generic;
using config_lib;
using rabbit_lib;
using service_lib.Handlers;
using trade_contracts.Messages.ConfigMessages;

namespace config_service_lib.Handlers
{
    public interface ISimpleRequestHandler : IMessageHandler<SimpleRequestMessage> { }
    public class SimpleRequestHandler : ISimpleRequestHandler
    {
        private readonly IConfigRepo _configRepo;

        public SimpleRequestHandler(IConfigRepo configRepo)
        {
            _configRepo = configRepo;
        }

        public void Handle(IRabbitConnection rabbit, SimpleRequestMessage message)
        {
            if (message == null) { throw new ArgumentNullException(nameof(message)); }
            if (string.IsNullOrWhiteSpace(message.Payload)) { throw new ArgumentNullException(nameof(message.Payload)); }

            var dict = new Dictionary<string, Func<string>>(StringComparer.InvariantCultureIgnoreCase)
            {
                { "get-connection-string", () => _configRepo.GetConnectionString() },
                { "get-mew-wallet-address", () => _configRepo.GetMewWalletAddress() }
            };

            if (dict.ContainsKey(message.Payload.Trim()))
            {
                var response = new SimpleResponseMessage
                {
                    CorrelationId = message.CorrelationId,
                    Payload = dict[message.Payload.Trim()](),
                    WasSuccessful = true
                };

                rabbit.PublishContract(message.ResponseQueue, response, TimeSpan.FromMinutes(5));
                return;
            }

            throw new ApplicationException($"Unexpected request {message.Payload.Trim()}.");
        }
    }
}
