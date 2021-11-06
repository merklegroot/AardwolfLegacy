using System;
using config_lib;
using rabbit_lib;
using service_lib.Handlers;
using trade_contracts.Messages.Config;

namespace config_service_lib.Handlers
{
    public interface ISetMewWalletAddressHandler : IMessageHandler<SetMewWalletAddressRequestMessage> { }
    public class SetMewWalletAddressHandler : ISetMewWalletAddressHandler
    {
        private readonly IConfigRepo _configRepo;

        public SetMewWalletAddressHandler(IConfigRepo configRepo)
        {
            _configRepo = configRepo;
        }

        public void Handle(IRabbitConnection rabbit, SetMewWalletAddressRequestMessage message)
        {
            _configRepo.SetEthAddress(message.Address);
            var response = new SetMewWalletAddressResponseMessage
            {
                CorrelationId = message.CorrelationId,
                TimeStampUtc = DateTime.UtcNow,
                WasSuccessful = true
            };

            rabbit.PublishContract(message.ResponseQueue, response, TimeSpan.FromMinutes(5));
        }
    }
}
