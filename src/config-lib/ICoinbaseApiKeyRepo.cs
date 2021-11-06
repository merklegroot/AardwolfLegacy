
using trade_model;

namespace config_lib
{
    public interface ICoinbaseApiKeyRepo
    {
        ApiKey GetCoinbaseApiKey();
        void SetCoinbaseApiKey(ApiKey apiKey);
    }
}
