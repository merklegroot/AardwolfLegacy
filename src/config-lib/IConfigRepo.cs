using trade_model;
using trade_model.ArbConfig;

namespace config_lib
{
    public interface IConfigRepo :
        IGetConnectionStringDirect,
        IBinanceApiKeyRepoDirect, IHitBtcApiKeyRepo, 
        IKrakenApiKeyRepo, IEtherscanConfigRepo, 
        ICossCredentialRepo, ICossAgentConfigRepo,
        ILivecoinApiKeyRepo, IKucoinApiRepo,
        IBitzApiKeyRepo, IBitzAgentConfigRepo,
        IQryptosApiKeyRepo,
        IMewConfigRepo,
        ICryptopiaApiKeyRepo,
        ICossApiKeyRepo,
        ITwitterApiKeyRepo,
        ICcxtConfigRepoDirect,
        ICoinbaseApiKeyRepo,
        IInfuraApiKeyRepo
    {
        void SetConnectionString(string connectionString);

        BinanceArbConfig GetBinanceArbConfig();
        void SetBinanceArbConfig(BinanceArbConfig arbConfig);

        void SetBlocktradeApiKey(ApiKey apiKey);
        ApiKey GetBlocktradeApiKey();
    }
}
