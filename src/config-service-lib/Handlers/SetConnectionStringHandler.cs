using config_lib;
using log_lib;
using service_lib.Handlers;
using System;
using trade_contracts.Messages.Config;

namespace config_service_lib
{
    public interface ISetConnectionStringHandler : IRequestResponseHandler<SetConnectionStringRequestMessage, SetConnectionStringResponseMessage> { }
    public class SetConnectionStringHandler : ISetConnectionStringHandler
    {
        private readonly ILogRepo _logRepo;
        private readonly IConfigRepo _configRepo;

        public SetConnectionStringHandler(            
            IConfigRepo configRepo,
            ILogRepo logRepo)
        {
            _configRepo = configRepo;
            _logRepo = logRepo;
        }

        public SetConnectionStringResponseMessage Handle(SetConnectionStringRequestMessage message)
        {
            try
            {
                _configRepo.SetConnectionString(message.ConnectionString);
                return new SetConnectionStringResponseMessage { WasSuccessful = true };
            }
            catch (Exception exception)
            {
                _logRepo.Error(exception);
                return new SetConnectionStringResponseMessage { WasSuccessful = false, FailureReason = exception.Message };
            }
        }
    }
}
