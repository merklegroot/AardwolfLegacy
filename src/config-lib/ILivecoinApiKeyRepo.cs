using System.Collections.Generic;
using trade_model;

namespace config_lib
{
    public interface ILivecoinApiKeyRepo
    {
        ApiKey GetLivecoinApiKey();
        void SetLivecoinApiKey(ApiKey apiKey);
    }
}
