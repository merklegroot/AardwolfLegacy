using trade_model;

namespace config_lib
{
    public interface IHitBtcApiKeyRepo
    {
        ApiKey GetHitBtcApiKey();
        void SetHitBtcApiKey(ApiKey apiKey);        
    }
}
