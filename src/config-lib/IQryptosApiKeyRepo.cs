using trade_model;

namespace config_lib
{
    public interface IQryptosApiKeyRepo
    {
        ApiKey GetQryptosApiKey();
        void SetQryptosApiKey(ApiKey apiKey);
    }
}
