using config_model;
using trade_model;

namespace config_lib
{
    public interface IBitzApiKeyRepo
    {
        ApiKey GetBitzApiKey();
        void SetBitzApiKey(ApiKey apiKey);
        string GetBitzTradePassword();
        void SetBitzTradePassword(string password);
        UsernameAndPassword GetBitzLoginCredentials();
        void SetBitzLoginCredentials(UsernameAndPassword credentials);
    }
}
