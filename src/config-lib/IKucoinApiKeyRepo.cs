using config_model;
using trade_model;

namespace config_lib
{
    public interface IKucoinApiRepo
    {
        ApiKey GetKucoinApiKey();
        void SetKucoinApiKey(ApiKey apiKey);

        string GetKucoinApiPassphrase();
        void SetKucoinApiPassphrase(string passphrase);

        UsernameAndPassword GetKucoinEmailCredentials();
        void SetKucoinEmailCredentials(UsernameAndPassword credentials);

        string GetKucoinTradePassword();
        void SetKucoinTradePassword(string tradingPassword);
    }
}
