//using System;
//using System.Text;
//using trade_constants;
//using trade_contracts;
//using trade_contracts.Messages;
//using trade_contracts.Messages.Config;
//using trade_contracts.Messages.ConfigMessages;

//namespace iridium_lib
//{
//    public class ConfigClient: IConfigClient
//    {   
//        private readonly IRequestResponse _requestResponse;

//        public ConfigClient(IRequestResponse requestResponse)
//        {
//            _requestResponse = requestResponse;
//        }

//        public string GetMewWalletFileName()
//        {
//            return ExecuteV0SimpleRequest("get-mew-wallet-filename")?.Payload;
//        }

//        public string GetMewWalletAddress()
//        {
//            return ExecuteV1SimpleRequest("get-mew-wallet-address")?.Payload;
//        }

//        public void SetMewWalletAddress(string address)
//        {
//            var request = new SetMewWalletAddressRequestMessage { Address = address };
//            var response = _requestResponse.Execute<SetMewWalletAddressRequestMessage, SetMewWalletAddressResponseMessage>
//                (request, VersionedQueue(1));

//            ValidateResponse(response);
//        }

//        public string GetConnectionString()
//        {
//            var req = new GetConnectionStringRequestMessage();
//            var response = _requestResponse.Execute<GetConnectionStringRequestMessage, GetConnectionStringResponseMessage>(req, VersionedQueue(3));

//            return response.ConnectionString;
//        }

//        private string VersionedQueue(int version) => $"{TradeRabbitConstants.Queues.ConfigServiceQueue}" 
//            + (version > 0 ? $"-v{version}" : string.Empty);

//        public bool Ping()
//        {
//            return _requestResponse.Execute<PingMessage, PongMessage>
//                (new PingMessage(), VersionedQueue(0))
//                != null;
//        }

//        private SimpleResponseMessage ExecuteV0SimpleRequest(string payload)
//        {
//            return _requestResponse.Execute<SimpleRequestMessage, SimpleResponseMessage>(
//                new SimpleRequestMessage { Payload = payload },
//                VersionedQueue(0));
//        }

//        private SimpleResponseMessage ExecuteV1SimpleRequest(string payload)
//        {
//            return _requestResponse.Execute<SimpleRequestMessage, SimpleResponseMessage>(
//                new SimpleRequestMessage { Payload = payload },
//                VersionedQueue(1));
//        }

//        private SimpleResponseMessage ExecuteSimpleRequest(string queue, string payload)
//        {
//            return _requestResponse.Execute<SimpleRequestMessage, SimpleResponseMessage>(
//                new SimpleRequestMessage { Payload = payload },
//                queue);
//        }

//        public void SetConnectionString(string connectionString)
//        {
//            var req = new SetConnectionStringRequestMessage
//            {
//                ConnectionString = connectionString
//            };

//            var response = _requestResponse.Execute<SetConnectionStringRequestMessage, SetConnectionStringResponseMessage>(req, VersionedQueue(2));
//            ValidateResponse(response);
//        }

//        public ApiKeyContract GetApiKey(string exchange)
//        {
//            var req = new GetApiKeyRequestMessage { Exchange = exchange };
//            var response = _requestResponse.Execute<GetApiKeyRequestMessage, GetApiKeyResponseMessage>(req, VersionedQueue(3));
//            return response?.ApiKey;
//        }

//        private void ValidateResponse(IResultContract response)
//        {
//            if (response == null) { throw new ApplicationException("Service bus returned a null response."); }
//            if (!response.WasSuccessful)
//            {
//                var errorBuilder = new StringBuilder()
//                    .AppendLine("Service bus response indicated failure.");

//                if (!string.IsNullOrWhiteSpace(response.FailureReason))
//                {
//                    errorBuilder.AppendLine(response.FailureReason.Trim());
//                }

//                throw new ApplicationException(errorBuilder.ToString());
//            }
//        }

//        public ApiKeyContract GetBinanceApiKey() => GetApiKey("binance");
//        public ApiKeyContract GetHitBtcApiKey() => GetApiKey("hitbtc");
//        public ApiKeyContract GetKucoinApiKey() => GetApiKey("kucoin");
//        public ApiKeyContract GetLivecoinApiKey() => GetApiKey("livecoin");
//        public ApiKeyContract GetCryptopiaApiKey() => GetApiKey("cryptopia");
//        public ApiKeyContract GetBitzApiKey() => GetApiKey("bitz");
//        public ApiKeyContract GetQryptosApiKey() => GetApiKey("qryptos");
//        public ApiKeyContract GetKrakenApiKey() => GetApiKey("kraken");
//        public ApiKeyContract GetTwitterApiKey() => GetApiKey("twitter");
//        public ApiKeyContract GetEtherscanApiKey() => GetApiKey("etherscan");
//    }
//}
