using trade_model;

namespace config_lib
{
    public interface ICossApiKeyRepo
    {
        ApiKey GetCossApiKey();
        void SetCossApiKey(ApiKey apiKey);        
    }
}
