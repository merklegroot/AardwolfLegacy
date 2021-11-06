using config_lib;
using config_model;
using service_lib.Handlers;
using System;
using System.Collections.Generic;
using trade_constants;
using trade_contracts;
using trade_contracts.Messages.Config;
using trade_contracts.Messages.Config.Arb;
using trade_contracts.Messages.Config.Mew;
using trade_contracts.Models.Arb;
using trade_model.ArbConfig;

namespace config_service_lib.Handlers
{
    public interface IConfigHandler : 
        IRequestResponseHandler<GetCredentialsRequestMessage, GetCredentialsResponseMessage>,
        IRequestResponseHandler<GetCossAgentConfigRequestMessage, GetCossAgentConfigResponseMessage>,
        IRequestResponseHandler<SetCossAgentConfigRequestMessage, SetCossAgentConfigResponseMessage>,
        IRequestResponseHandler<GetMewPasswordRequestMessage, GetMewPasswordResponseMessage>,
        IRequestResponseHandler<SetMewPasswordRequestMessage, SetMewPasswordResponseMessage>,
        IRequestResponseHandler<GetMewWalletFileNameRequestMessage, GetMewWalletFileNameResponseMessage>,
        IRequestResponseHandler<SetMewWalletFileNameRequestMessage, SetMewWalletFileNameResponseMessage>,
        IRequestResponseHandler<GetPasswordRequestMessage, GetPasswordResponseMessage>,
        IRequestResponseHandler<SetPasswordRequestMessage, SetPasswordResponseMessage>,
        IRequestResponseHandler<GetBinanceArbConfigRequestMessage, GetBinanceArbConfigResponseMessage>,
        IRequestResponseHandler<SetBinanceArbConfigRequestMessage, SetBinanceArbConfigResponseMessage>
    { }

    public class ConfigHandler : IConfigHandler
    {
        private readonly IConfigRepo _configRepo;

        public ConfigHandler(IConfigRepo configRepo)
        {
            _configRepo = configRepo;
        }

        public GetCredentialsResponseMessage Handle(GetCredentialsRequestMessage message)
        {
            var exchangeDictionary = new Dictionary<string, Func<UsernameAndPassword>>(StringComparer.InvariantCultureIgnoreCase)
            {
                { IntegrationNameRes.Coss, new Func<UsernameAndPassword>(() => _configRepo.GetCossCredentials()) },
                { IntegrationNameRes.Bitz , new Func<UsernameAndPassword>(() => _configRepo.GetBitzLoginCredentials()) }
            };

            if (string.IsNullOrWhiteSpace(message.Exchange)) { throw new ArgumentNullException(nameof(message.Exchange)); }
            var effectiveExchange = message.Exchange.Trim().Replace("-", string.Empty);
            if (!exchangeDictionary.ContainsKey(effectiveExchange))
            {
                throw new ApplicationException($"Unexpected exchange {message.Exchange}.");
            }

            var credentials = exchangeDictionary[effectiveExchange]();

            return new GetCredentialsResponseMessage
            {
                Credentials = credentials != null
                ? new UsernameAndPasswordContract
                {
                    UserName = credentials.UserName,
                    Password = credentials.Password
                }
                : null
            };
        }

        public GetCossAgentConfigResponseMessage Handle(GetCossAgentConfigRequestMessage message)
        {
            var model = _configRepo.GetCossAgentConfig();
            return new GetCossAgentConfigResponseMessage
            {
                Payload = ToContract(model)
            };
        }

        public SetCossAgentConfigResponseMessage Handle(SetCossAgentConfigRequestMessage message)
        {
            _configRepo.SetCossAgentConfig(ToModel(message.Payload));

            return new SetCossAgentConfigResponseMessage();
        }

        public GetMewPasswordResponseMessage Handle(GetMewPasswordRequestMessage message)
        {
            return new GetMewPasswordResponseMessage
            {
                Payload = new GetMewPasswordResponseMessage.ResponsePayload
                {
                    Password = _configRepo.GetMewPassword()
                }
            };
        }

        public SetMewPasswordResponseMessage Handle(SetMewPasswordRequestMessage message)
        {
            _configRepo.SetMewPassword(message?.Payload?.Password);
            return new SetMewPasswordResponseMessage();
        }

        public GetMewWalletFileNameResponseMessage Handle(GetMewWalletFileNameRequestMessage message)
        {
            var fileName = _configRepo.GetMewWalletFileName();
            return new GetMewWalletFileNameResponseMessage
            {
                Payload = new GetMewWalletFileNameResponseMessage.ResponsePayload
                {
                    FileName = fileName
                }
            };
        }

        public SetMewWalletFileNameResponseMessage Handle(SetMewWalletFileNameRequestMessage message)
        {
            if (message == null) { throw new ArgumentNullException(nameof(message)); }
            if (message.Payload == null) { throw new ArgumentNullException(nameof(message.Payload)); }

            _configRepo.SetMewWalletFileName(message.Payload.FileName);
            return new SetMewWalletFileNameResponseMessage();
        }

        public GetPasswordResponseMessage Handle(GetPasswordRequestMessage message)
        {
            if (message == null) { throw new ArgumentNullException(nameof(message)); }
            if (message.Payload == null) { throw new ArgumentNullException(nameof(message.Payload)); }
            if (string.IsNullOrWhiteSpace(message.Payload.Key)) { throw new ArgumentNullException(nameof(message.Payload.Key)); }

            if (string.Equals(message.Payload.Key, "bitz-trade", StringComparison.InvariantCultureIgnoreCase))
            {
                return new GetPasswordResponseMessage
                {
                    Payload = new GetPasswordResponseMessage.ResponsePayload
                    {
                        Password = _configRepo.GetBitzTradePassword()
                    }
                };                
            }

            if (string.Equals(message.Payload.Key, "kucoin-trade", StringComparison.InvariantCultureIgnoreCase))
            {
                return new GetPasswordResponseMessage
                {
                    Payload = new GetPasswordResponseMessage.ResponsePayload
                    {
                        Password = _configRepo.GetKucoinTradePassword()
                    }
                };
            }

            if (string.Equals(message.Payload.Key, "kucoin-api-passphrase", StringComparison.InvariantCultureIgnoreCase))
            {
                return new GetPasswordResponseMessage
                {
                    Payload = new GetPasswordResponseMessage.ResponsePayload
                    {
                        Password = _configRepo.GetKucoinApiPassphrase()
                    }
                };
            }

            if (string.Equals(message.Payload.Key, IntegrationNameRes.Mew, StringComparison.InvariantCultureIgnoreCase))
            {
                return new GetPasswordResponseMessage
                {
                    Payload = new GetPasswordResponseMessage.ResponsePayload
                    {
                        Password = _configRepo.GetMewPassword()
                    }
                };
            }

            throw new ArgumentException($"Unexpected key: {message.Payload.Key};");
        }

        public SetPasswordResponseMessage Handle(SetPasswordRequestMessage message)
        {
            if (message == null) { throw new ArgumentNullException(nameof(message)); }
            if (message.Payload == null) { throw new ArgumentNullException(nameof(message.Payload)); }
            if (string.IsNullOrWhiteSpace(message.Payload.Key)) { throw new ArgumentNullException(nameof(message.Payload.Key)); }

            if (string.Equals(message.Payload.Key, "bitz-trade", StringComparison.InvariantCultureIgnoreCase))
            {
                _configRepo.SetBitzTradePassword(message.Payload.Password);
                return new SetPasswordResponseMessage();
            }

            if (string.Equals(message.Payload.Key, "kucoin-trade", StringComparison.InvariantCultureIgnoreCase))
            {
                _configRepo.SetKucoinTradePassword(message.Payload.Password);
                return new SetPasswordResponseMessage();
            }

            if (string.Equals(message.Payload.Key, "kucoin-api-passphrase", StringComparison.InvariantCultureIgnoreCase))
            {
                _configRepo.SetKucoinApiPassphrase(message.Payload.Password);
                return new SetPasswordResponseMessage();
            }

            if (string.Equals(message.Payload.Key, IntegrationNameRes.Mew, StringComparison.InvariantCultureIgnoreCase))
            {
                _configRepo.SetMewPassword(message.Payload.Password);
                return new SetPasswordResponseMessage();
            }

            throw new ArgumentException($"Unexpected key: {message.Payload.Key};");
        }

        public GetBinanceArbConfigResponseMessage Handle(GetBinanceArbConfigRequestMessage message)
        {
            var arbConfig = _configRepo.GetBinanceArbConfig();

            return new GetBinanceArbConfigResponseMessage
            {
                Payload = ToContract(arbConfig)
            };
        }

        public SetBinanceArbConfigResponseMessage Handle(SetBinanceArbConfigRequestMessage message)
        {
            var model = ToModel(message.Payload);
            _configRepo.SetBinanceArbConfig(model);

            return new SetBinanceArbConfigResponseMessage();
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

        private CossAgentConfig ToModel(CossAgentConfigContract model)
        {
            return model != null
                ? new CossAgentConfig
                {
                    EthThreshold = model.EthThreshold,
                    IsCossAutoTradingEnabled = model.IsCossAutoTradingEnabled,
                    TokenThreshold = model.TokenThreshold
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
    }
}
