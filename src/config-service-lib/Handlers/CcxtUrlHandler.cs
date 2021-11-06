using config_lib;
using service_lib.Handlers;
using System;
using trade_contracts.Messages.Config;

namespace config_service_lib.Handlers
{
    public interface ICcxtUrlHandler : 
        IRequestResponseHandler<GetCcxtUrlRequestMessage, GetCcxtUrlResponseMessage>,
        IRequestResponseHandler<SetCcxtUrlRequestMessage, SetCcxtUrlResponseMessage>
    { }

    public class CcxtUrlHandler : ICcxtUrlHandler
    {
        private readonly IConfigRepo _configRepo;

        public CcxtUrlHandler(IConfigRepo configRepo)
        {
            _configRepo = configRepo;
        }

        public GetCcxtUrlResponseMessage Handle(GetCcxtUrlRequestMessage message)
        {
            return new GetCcxtUrlResponseMessage
            {
                Url = _configRepo.GetCcxtUrl()
            };
        }

        public SetCcxtUrlResponseMessage Handle(SetCcxtUrlRequestMessage message)
        {
            if (message == null) { throw new ArgumentNullException(nameof(message)); }
            if (message.Payload == null) { throw new ArgumentNullException(nameof(message.Payload)); }

            _configRepo.SetCcxtUrl(message.Payload.Url);

            return new SetCcxtUrlResponseMessage();
        }
    }
}
