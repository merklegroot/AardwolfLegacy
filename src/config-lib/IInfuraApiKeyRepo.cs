using trade_model;

namespace config_lib
{
    public interface IInfuraApiKeyRepo
    {
        ApiKey GetInfuraApiKey();
        void SetInfuraApiKey(ApiKey apiKey);
    }
}
