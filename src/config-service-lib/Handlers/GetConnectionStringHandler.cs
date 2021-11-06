using config_lib;
using console_lib;
using service_lib.Handlers;
using trade_contracts.Messages.Config;

namespace config_service_lib.Handlers
{
    public interface IGetConnectionStringHandler : IRequestResponseHandler<GetConnectionStringRequestMessage, GetConnectionStringResponseMessage> { }
    public class GetConnectionStringHandler : IGetConnectionStringHandler
    {
        private readonly IConfigRepo _configRepo;

        public GetConnectionStringHandler(IConfigRepo configRepo)
        {
            _configRepo = configRepo;
        }

        public GetConnectionStringResponseMessage Handle(GetConnectionStringRequestMessage message)
        {
            var conn = _configRepo.GetConnectionString();

            return new GetConnectionStringResponseMessage
            {
                ConnectionString = conn // _configRepo.GetConnectionString()
            };
        }
    }
}
