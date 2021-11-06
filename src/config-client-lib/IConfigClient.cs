using client_lib;
using config_connection_string_lib;
using config_model;
using trade_contracts;
using trade_model;
using trade_model.ArbConfig;

namespace config_client_lib
{
    public interface IConfigClient : IGetConnectionString, IServiceClient
    {
        string GetMewWalletFileName();
        void SetMewWalletFileName(string fileName);

        string GetMewWalletAddress();
        void SetMewWalletAddress(string address);
        
        void SetConnectionString(string connectionString);

        ApiKey GetApiKey(string exchange);
        ApiKey GetBinanceApiKey();
        ApiKey GetHitBtcApiKey();
        ApiKey GetLivecoinApiKey();
        ApiKey GetKucoinApiKey();
        ApiKey GetCryptopiaApiKey();
        ApiKey GetBitzApiKey();
        ApiKey GetQryptosApiKey();
        ApiKey GetKrakenApiKey();
        ApiKey GetTwitterApiKey();
        ApiKey GetEtherscanApiKey();
        string GetMewPassword();
        void SetMewPassword(string password);

        string GetBitzTradePassword();
        void SetBitzTradePassword(string password);

        string GetKucoinTradePassword();
        void SetKucoinTradePassword(string password);

        string GetKucoinApiPassphrase();
        void SetKucoinApiPassphrase(string password);

        void SetApiKey(string exchange, string key, string secret);

        CossAgentConfig GetCossAgentConfig();
        void SetCossAgentConfig(CossAgentConfig config);

        UsernameAndPassword GetCossCredentials();

        string GetCcxtUrl();
        void SetCcxtUrl(string url);

        AgentConfigContract GetBitzAgentConfig();

        UsernameAndPassword GetBitzLoginCredentials();

        BinanceArbConfig GetBinanceArbConfig();

        void SetBinanceArbConfig(BinanceArbConfig arbConfig);
    }
}
