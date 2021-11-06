using trade_model;

namespace config_lib
{
    public interface ICryptopiaApiKeyRepo
    {
        ApiKey GetCryptopiaApiKey();
        void SetCryptopiaApiKey(ApiKey apiKey);        
    }
}
