using trade_model;

namespace config_lib
{
    public interface IBinanceApiKeyRepoDirect
    {
        ApiKey GetBinanceApiKey();
        void SetBinanceApiKey(ApiKey apiKey);
    }
}
