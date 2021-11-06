using trade_model;

namespace config_lib
{
    public interface IKrakenApiKeyRepo
    {
        ApiKey GetKrakenApiKey();
        void SetKrakenApiKey(ApiKey apiKey);        
    }
}
