using config_lib;
using service_lib.Handlers;
using System;
using System.Collections.Generic;
using trade_contracts;
using trade_contracts.Messages.Config;
using trade_model;

namespace config_service_lib.Handlers
{
    public interface IApiKeyHandler :
        IRequestResponseHandler<GetApiKeyRequestMessage, GetApiKeyResponseMessage>,
        IRequestResponseHandler<SetApiKeyRequestMessage, SetApiKeyResponseMessage>
    { }

    public class ApiKeyHandler : IApiKeyHandler
    {
        private readonly IConfigRepo _configRepo;

        public ApiKeyHandler(IConfigRepo configRepo)
        {
            _configRepo = configRepo;
        }

        public GetApiKeyResponseMessage Handle(GetApiKeyRequestMessage message)
        {
            var exchangeDictionary = new Dictionary<string, Func<ApiKey>>(StringComparer.InvariantCultureIgnoreCase)
            {
                { "binance", new Func<ApiKey>(() => _configRepo.GetBinanceApiKey()) },
                { "cryptopia" , new Func<ApiKey>(() => _configRepo.GetCryptopiaApiKey()) },
                { "coss" , new Func<ApiKey>(() => _configRepo.GetCossApiKey()) },
                { "bitz" , new Func<ApiKey>(() => _configRepo.GetBitzApiKey()) },
                { "bit-z" , new Func<ApiKey>(() => _configRepo.GetBitzApiKey()) },
                { "etherscan" , new Func<ApiKey>(() => new ApiKey { Secret = _configRepo.GetEtherscanApiKey() }) },
                { "hitbtc" , new Func<ApiKey>(() => _configRepo.GetHitBtcApiKey()) },
                { "kraken" , new Func<ApiKey>(() => _configRepo.GetKrakenApiKey()) },
                { "kucoin" , new Func<ApiKey>(() => _configRepo.GetKucoinApiKey()) },
                { "livecoin" , new Func<ApiKey>(() => _configRepo.GetLivecoinApiKey()) },
                { "qryptos" , new Func<ApiKey>(() => _configRepo.GetQryptosApiKey()) },
                { "twitter" , new Func<ApiKey>(() => _configRepo.GetTwitterApiKey()) },
                { "coinbase" , new Func<ApiKey>(() => _configRepo.GetCoinbaseApiKey()) },
                { "blocktrade" , new Func<ApiKey>(() => _configRepo.GetBlocktradeApiKey()) },
                { "infura" , new Func<ApiKey>(() => _configRepo.GetInfuraApiKey()) }
            };

            ApiKeyContract apiKeyContract = null;
            if (exchangeDictionary.ContainsKey(message.Exchange))
            {
                var apiKey = exchangeDictionary[message.Exchange]();
                apiKeyContract = new ApiKeyContract
                {
                    Key = apiKey?.Key,
                    Secret = apiKey?.Secret
                };
            }

            return new GetApiKeyResponseMessage
            {
                ApiKey = apiKeyContract
            };
        }

        public SetApiKeyResponseMessage Handle(SetApiKeyRequestMessage message)
        {
            if (message == null) { throw new ArgumentNullException(nameof(message)); }
            if (message.Payload == null) { throw new ArgumentNullException(nameof(message.Payload)); }
            if (string.IsNullOrWhiteSpace(message.Payload.Exchange)) { throw new ArgumentNullException(nameof(message.Payload.Exchange)); }

            var exchangeDictionary = new Dictionary<string, Action<ApiKey>>(StringComparer.InvariantCultureIgnoreCase)
            {
                { "binance", new Action<ApiKey>(key => _configRepo.SetBinanceApiKey(key)) },
                { "cryptopia" , new Action<ApiKey>(key => _configRepo.SetCryptopiaApiKey(key)) },
                { "coss" , new Action<ApiKey>(key => _configRepo.SetCossApiKey(key)) },
                { "bitz" , new Action<ApiKey>(key => _configRepo.SetBitzApiKey(key)) },
                { "etherscan" , new Action<ApiKey>(key => _configRepo.SetEtherscanApiKey(key?.Secret)) },
                { "hitbtc" , new Action<ApiKey>(key => _configRepo.SetHitBtcApiKey(key)) },
                { "kraken" , new Action<ApiKey>(key => _configRepo.SetKrakenApiKey(key)) },
                { "kucoin" , new Action<ApiKey>(key => _configRepo.SetKucoinApiKey(key)) },
                { "livecoin" , new Action<ApiKey>(key => _configRepo.SetLivecoinApiKey(key)) },
                { "qryptos" , new Action<ApiKey>(key => _configRepo.SetQryptosApiKey(key)) },
                { "twitter" , new Action<ApiKey>(key => _configRepo.SetTwitterApiKey(key)) },
                { "coinbase" , new Action<ApiKey>(key => _configRepo.SetCoinbaseApiKey(key)) },
                { "blocktrade" , new Action<ApiKey>(key => _configRepo.SetBlocktradeApiKey(key)) },
                { "infura" , new Action<ApiKey>(key => _configRepo.SetInfuraApiKey(key)) }
            };

            if (!exchangeDictionary.ContainsKey(message.Payload.Exchange))
            {
                throw new ApplicationException($"SetApiKey is not supported for exchange \"{message.Payload.Exchange}\".");
            }

            var apiKey = new ApiKey
            {
                Key = message.Payload.Key,
                Secret = message.Payload.Secret
            };

            exchangeDictionary[message.Payload.Exchange].Invoke(apiKey);

            return new SetApiKeyResponseMessage();
        }
    }
}
