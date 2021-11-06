using client_lib;
using config_model;
using env_config_lib;
using rabbit_lib;
using System;
using System.Text;
using trade_constants;
using trade_contracts;
using trade_contracts.Messages.Config;
using trade_contracts.Messages.Config.Arb;
using trade_contracts.Messages.Config.Mew;
using trade_contracts.Messages.ConfigMessages;
using trade_contracts.Models.Arb;
using trade_model;
using trade_model.ArbConfig;

namespace config_client_lib
{
    public class ConfigClient : ServiceClient, IConfigClient
    {
        protected override string QueueName => TradeRabbitConstants.Queues.ConfigServiceQueue;

        private static Func<IRequestResponse> RequestResponseFactory = new Func<IRequestResponse>(() =>
        {
            var envConfigRepo = new EnvironmentConfigRepo();
            var rabbitConnectionFactory = new RabbitConnectionFactory(envConfigRepo);
            return new RequestResponse(rabbitConnectionFactory);
        });

        // Creating the clients is supposed to be easy.
        public ConfigClient() : this(RequestResponseFactory())
        {
        }

        public ConfigClient(IRequestResponse requestResponse)
            : base(requestResponse)
        {
        }

        public string GetMewWalletFileName()
        {
            var request = new GetMewWalletFileNameRequestMessage();
            var response = RequestResponse.Execute<GetMewWalletFileNameRequestMessage, GetMewWalletFileNameResponseMessage>
                (request, VersionedQueue(1));

            return response?.Payload?.FileName;
        }

        public void SetMewWalletFileName(string fileName)
        {
            var request = new SetMewWalletFileNameRequestMessage
            {
                Payload = new SetMewWalletFileNameRequestMessage.RequestPayload
                {
                    FileName = fileName
                }
            };

            var response = RequestResponse.Execute<SetMewWalletFileNameRequestMessage, SetMewWalletFileNameResponseMessage>
                (request, VersionedQueue(1));

            ValidateResponse(response);
        }

        public string GetMewWalletAddress()
        {
            return ExecuteV1SimpleRequest("get-mew-wallet-address")?.Payload;
        }

        public void SetMewWalletAddress(string address)
        {
            var request = new SetMewWalletAddressRequestMessage { Address = address };
            var response = RequestResponse.Execute<SetMewWalletAddressRequestMessage, SetMewWalletAddressResponseMessage>
                (request, VersionedQueue(1));

            ValidateResponse(response);
        }

        public string GetMewPassword()
        {
            var request = new GetMewPasswordRequestMessage();
            var response = RequestResponse.Execute<GetMewPasswordRequestMessage, GetMewPasswordResponseMessage>
                (request, VersionedQueue(1));

            return response?.Payload?.Password;
        }

        public void SetMewPassword(string password)
        {
            var request = new SetMewPasswordRequestMessage
            {
                Payload = new SetMewPasswordRequestMessage.RequestPayload
                {
                    Password = password
                }
            };

            RequestResponse.Execute<SetMewPasswordRequestMessage, SetMewPasswordResponseMessage>(request, VersionedQueue(1));
        }

        public string GetBitzTradePassword()
        {
            var req = new GetPasswordRequestMessage
            {
                Payload = new GetPasswordRequestMessage.RequestPayload
                {
                    Key = "bitz-trade"
                }
            };

            var response = RequestResponse.Execute<GetPasswordRequestMessage, GetPasswordResponseMessage>(req, VersionedQueue(1));

            return response?.Payload?.Password;
        }

        public void SetBitzTradePassword(string password)
        {
            var req = new SetPasswordRequestMessage
            {
                Payload =  new SetPasswordRequestMessage.RequestPayload
                {
                    Key = "bitz-trade",
                    Password = password
                }
            };

            RequestResponse.Execute<SetPasswordRequestMessage, SetPasswordResponseMessage>(req, VersionedQueue(1));
        }

        public string GetKucoinTradePassword()
        {
            var req = new GetPasswordRequestMessage
            {
                Payload = new GetPasswordRequestMessage.RequestPayload
                {
                    Key = "kucoin-trade"
                }
            };

            var response = RequestResponse.Execute<GetPasswordRequestMessage, GetPasswordResponseMessage>(req, VersionedQueue(1));

            return response?.Payload?.Password;
        }

        public void SetKucoinTradePassword(string password)
        {
            var req = new SetPasswordRequestMessage
            {
                Payload = new SetPasswordRequestMessage.RequestPayload
                {
                    Key = "kucoin-trade",
                    Password = password
                }
            };

            RequestResponse.Execute<SetPasswordRequestMessage, SetPasswordResponseMessage>(req, VersionedQueue(1));
        }

        public string GetKucoinApiPassphrase()
        {
            var req = new GetPasswordRequestMessage
            {
                Payload = new GetPasswordRequestMessage.RequestPayload
                {
                    Key = "kucoin-api-passphrase"
                }
            };

            var response = RequestResponse.Execute<GetPasswordRequestMessage, GetPasswordResponseMessage>(req, VersionedQueue(1));

            return response?.Payload?.Password;
        }

        public void SetKucoinApiPassphrase(string password)
        {
            var req = new SetPasswordRequestMessage
            {
                Payload = new SetPasswordRequestMessage.RequestPayload
                {
                    Key = "kucoin-api-passphrase",
                    Password = password
                }
            };

            RequestResponse.Execute<SetPasswordRequestMessage, SetPasswordResponseMessage>(req, VersionedQueue(1));
        }

        private static string CachedConnectionString = null;
        private static DateTime? CachedConnectionStringTimeStamp = null;
        private static TimeSpan CachedConnectionStringMaxAge = TimeSpan.FromSeconds(30);

        public string GetConnectionString()
        {
            if (CachedConnectionStringTimeStamp.HasValue && 
                (DateTime.UtcNow - CachedConnectionStringTimeStamp.Value <= CachedConnectionStringMaxAge))
            {
                return CachedConnectionString;
            }

            var req = new GetConnectionStringRequestMessage();
            var response = RequestResponse.Execute<GetConnectionStringRequestMessage, GetConnectionStringResponseMessage>(req, VersionedQueue(3));

            CachedConnectionString = response.ConnectionString;
            CachedConnectionStringTimeStamp = DateTime.UtcNow;

            return response.ConnectionString;
        }

        private SimpleResponseMessage ExecuteV0SimpleRequest(string payload)
        {
            return RequestResponse.Execute<SimpleRequestMessage, SimpleResponseMessage>(
                new SimpleRequestMessage { Payload = payload },
                VersionedQueue(0));
        }

        private SimpleResponseMessage ExecuteV1SimpleRequest(string payload)
        {
            return RequestResponse.Execute<SimpleRequestMessage, SimpleResponseMessage>(
                new SimpleRequestMessage { Payload = payload },
                VersionedQueue(1));
        }

        private SimpleResponseMessage ExecuteSimpleRequest(string queue, string payload)
        {
            return RequestResponse.Execute<SimpleRequestMessage, SimpleResponseMessage>(
                new SimpleRequestMessage { Payload = payload },
                queue);
        }

        public void SetConnectionString(string connectionString)
        {
            CachedConnectionStringTimeStamp = null;

            var req = new SetConnectionStringRequestMessage
            {
                ConnectionString = connectionString
            };

            var response = RequestResponse.Execute<SetConnectionStringRequestMessage, SetConnectionStringResponseMessage>(req, VersionedQueue(2));
            ValidateResponse(response);

            CachedConnectionStringTimeStamp = null;
        }

        public ApiKey GetApiKey(string exchange)
        {
            var req = new GetApiKeyRequestMessage { Exchange = exchange };
            var response = RequestResponse.Execute<GetApiKeyRequestMessage, GetApiKeyResponseMessage>(req, VersionedQueue(3));
            return response?.ApiKey != null
                ? new ApiKey { Key = response?.ApiKey?.Key, Secret = response?.ApiKey.Secret }
                : null;
        }

        public void SetApiKey(string exchange, string key, string secret)
        {
            var req = new SetApiKeyRequestMessage
            {
                Payload = new SetApiKeyRequestMessage.RequestPayload
                {
                    Exchange = exchange,
                    Key = key,
                    Secret = secret
                }
            };

            RequestResponse.Execute<SetApiKeyRequestMessage, SetApiKeyResponseMessage>(req, VersionedQueue(3));
        }

        private void ValidateResponse(IResultContract response)
        {
            if (response == null) { throw new ApplicationException("Service bus returned a null response."); }
            if (!response.WasSuccessful)
            {
                var errorBuilder = new StringBuilder()
                    .AppendLine("Service bus response indicated failure.");

                if (!string.IsNullOrWhiteSpace(response.FailureReason))
                {
                    errorBuilder.AppendLine(response.FailureReason.Trim());
                }

                throw new ApplicationException(errorBuilder.ToString());
            }
        }

        public ApiKey GetBinanceApiKey() => GetApiKey("binance");
        public ApiKey GetHitBtcApiKey() => GetApiKey("hitbtc");
        public ApiKey GetKucoinApiKey() => GetApiKey("kucoin");
        public ApiKey GetLivecoinApiKey() => GetApiKey("livecoin");
        public ApiKey GetCryptopiaApiKey() => GetApiKey("cryptopia");
        public ApiKey GetBitzApiKey() => GetApiKey("bitz");
        public ApiKey GetQryptosApiKey() => GetApiKey("qryptos");
        public ApiKey GetKrakenApiKey() => GetApiKey("kraken");
        public ApiKey GetTwitterApiKey() => GetApiKey("twitter");
        public ApiKey GetEtherscanApiKey() => GetApiKey("etherscan");

        public string GetCcxtUrl()
        {
            var req = new GetCcxtUrlRequestMessage();
            var response = RequestResponse.Execute<GetCcxtUrlRequestMessage, GetCcxtUrlResponseMessage>(req, VersionedQueue(4));
            return response?.Url;
        }

        public void SetCcxtUrl(string url)
        {
            var req = new SetCcxtUrlRequestMessage
            {
                Payload = new SetCcxtUrlRequestMessage.RequestPayload
                {
                    Url = url
                }
            };

            var response = RequestResponse.Execute<SetCcxtUrlRequestMessage, SetCcxtUrlResponseMessage>(req, VersionedQueue(4));
        }

        public AgentConfigContract GetBitzAgentConfig()
        {
            var req = new GetBitzAgentConfigRequestMessage();
            var response = RequestResponse.Execute<GetBitzAgentConfigRequestMessage, GetBitzAgentConfigResponseMessage>(req, VersionedQueue(4));
            return response?.BitzAgentConfig;
        }

        public UsernameAndPassword GetCossCredentials()
        {
            var req = new GetCredentialsRequestMessage { Exchange = IntegrationNameRes.Coss };
            var response = RequestResponse.Execute<GetCredentialsRequestMessage, GetCredentialsResponseMessage>(req, VersionedQueue(4));
            return ToModel(response?.Credentials);
        }

        public CossAgentConfig GetCossAgentConfig()
        {
            var req = new GetCossAgentConfigRequestMessage();
            var response = RequestResponse.Execute<GetCossAgentConfigRequestMessage, GetCossAgentConfigResponseMessage>(req, VersionedQueue(4));

            return ToModel(response?.Payload);
        }

        public void SetCossAgentConfig(CossAgentConfig config)
        {
            var req = new SetCossAgentConfigRequestMessage
            {
                Payload = ToContract(config)
            };

            var response = RequestResponse.Execute<SetCossAgentConfigRequestMessage, SetCossAgentConfigResponseMessage>(req, VersionedQueue(4));            
        }

        private CossAgentConfigContract ToContract(CossAgentConfig model)
        {
            return model != null
                ? new CossAgentConfigContract
                {
                    EthThreshold = model.EthThreshold,
                    IsCossAutoTradingEnabled = model.IsCossAutoTradingEnabled,
                    TokenThreshold = model.TokenThreshold
                }
                : null;
        }

        private CossAgentConfig ToModel(CossAgentConfigContract contract)
        {
            return contract != null
                ? new CossAgentConfig
                {
                    EthThreshold = contract.EthThreshold,
                    IsCossAutoTradingEnabled = contract.IsCossAutoTradingEnabled,
                    TokenThreshold = contract.TokenThreshold
                }
                : null;
        }

        private BinanceArbConfig ToModel(BinanceArbConfigContract contract)
        {
            return contract != null
                ? new BinanceArbConfig
                {
                    IsEnabled = contract.IsEnabled,
                    ArkSaleTarget = contract.ArkSaleTarget,
                    TusdSaleTarget = contract.TusdSaleTarget,
                    EthSaleTarget = contract.EthSaleTarget,
                    LtcSaleTarget = contract.LtcSaleTarget,
                    WavesSaleTarget = contract.WavesSaleTarget,
                    SaleTargetDictionary = contract.SaleTargetDictionary
                }
                : null;
        }

        private BinanceArbConfigContract ToContract(BinanceArbConfig model)
        {
            return model != null
                ? new BinanceArbConfigContract
                {
                    IsEnabled = model.IsEnabled,
                    ArkSaleTarget = model.ArkSaleTarget,
                    TusdSaleTarget = model.TusdSaleTarget,
                    EthSaleTarget = model.EthSaleTarget,
                    LtcSaleTarget = model.LtcSaleTarget,
                    WavesSaleTarget = model.WavesSaleTarget,
                    SaleTargetDictionary = model.SaleTargetDictionary
                }
                : null;
        }

        public UsernameAndPassword GetBitzLoginCredentials()
        {
            var req = new GetCredentialsRequestMessage { Exchange = IntegrationNameRes.Bitz };
            var response = RequestResponse.Execute<GetCredentialsRequestMessage, GetCredentialsResponseMessage>(req, VersionedQueue(4));

            return ToModel(response?.Credentials);
        }

        private UsernameAndPassword ToModel(UsernameAndPasswordContract contract)
        {
            return contract != null
                ? new UsernameAndPassword
                {
                    UserName = contract.UserName,
                    Password = contract.Password
                }
                : null;
        }

        public BinanceArbConfig GetBinanceArbConfig()
        {
            var req = new GetBinanceArbConfigRequestMessage();
            var response = RequestResponse.Execute<GetBinanceArbConfigRequestMessage, GetBinanceArbConfigResponseMessage>(req, VersionedQueue(1));

            return ToModel(response?.Payload);
        }

        public void SetBinanceArbConfig(BinanceArbConfig arbConfig)
        {
            var req = new SetBinanceArbConfigRequestMessage
            {
                Payload = ToContract(arbConfig)
            };

            var response = RequestResponse.Execute<SetBinanceArbConfigRequestMessage, SetBinanceArbConfigResponseMessage>(req, VersionedQueue(1));
        }
    }
}
