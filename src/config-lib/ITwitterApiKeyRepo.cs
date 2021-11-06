using trade_model;

namespace config_lib
{
    public interface ITwitterApiKeyRepo
    {
        ApiKey GetTwitterApiKey();
        void SetTwitterApiKey(ApiKey apiKey);
    }
}
