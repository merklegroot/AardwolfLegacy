using config_model;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using trade_model;
using trade_model.ArbConfig;

namespace config_lib
{
    public class ConfigRepo : IConfigRepo
    {
        private static string Root
        {
            get
            {
                return IsLinux
                    ? "/trade/config"
                    : @"C:\trade\config\";
            }
        }

        private readonly string MewFileName = Path.Combine(Root, "mew-config.json");
        private readonly string DatabaseConfigFileName = Path.Combine(Root, "database-config.json");
        private readonly string BinanceFileName = Path.Combine(Root, "binance-config.json");
        private readonly string HitBtcFileName = Path.Combine(Root, "hit-btc-config.json");
        private readonly string KrakenFileName = Path.Combine(Root, "kraken-config.json");
        private readonly string EtherScanFileName = Path.Combine(Root, "etherscan-config.json");
        private readonly string PkFileName = Path.Combine(Root, "pk.json");
        private readonly string CossFileName = Path.Combine(Root, "coss-config.json");
        private readonly string LivecoinFileName = Path.Combine(Root, "livecoin-config.json");
        private readonly string PriorityFileName = Path.Combine(Root, "priority-config.json");
        private readonly string KucoinFileName = Path.Combine(Root, "kucoin-config.json");
        private readonly string BitzApiConfigFileName = Path.Combine(Root, "bitz-config.json");
        private readonly string BitzAgentConfigFileName = Path.Combine(Root, "bitz-agent-config.json");
        private readonly string BitzTradePasswordFileName = Path.Combine(Root, "bitz-trade-config.json");
        private readonly string BitzLoginCredentialsFileName = Path.Combine(Root, "bitz-login-config.json");
        private readonly string QryptosApiConfigFileName = Path.Combine(Root, "qryptos-api-key-config.json");

        private readonly string CossEmailConfigFileName = Path.Combine(Root, "coss-email-config.json");
        private readonly string KucoinEmailConfigFileName = Path.Combine(Root, "kucoin-email-config.json");

        private readonly string KucoinTradingPasswordConfigFileName = Path.Combine(Root, "kucoin-trade-config.json");
        private readonly string KucoinApiPassphraseConfigFileName = Path.Combine(Root, "kucoin-api-passphrase-config.json");

        private readonly string MewWalletConfigFileName = Path.Combine(Root, "mew-wallet-config.json");
        private readonly string MewPasswordConfigFileName = Path.Combine(Root, "mew-cred-config.json");

        private readonly string CryptopiaApiConfigFileName = Path.Combine(Root, "cryptopia-api-config.json");
        private readonly string CossApiConfigFileName = Path.Combine(Root, "coss-api-config.json");
        private readonly string CoinbaseApiConfigFileName = Path.Combine(Root, "coinbase-api-config.json");
        private readonly string CossAgentConfigFileName = Path.Combine(Root, "coss-agent-config.json");

        private readonly string TwitterApiKeyFileName = Path.Combine(Root, "twitter-api-key-config.json");

        private readonly string CcxtConfigFileName = Path.Combine(Root, "ccxt-config.json");

        private readonly string IntegrationEnablednessFileName = Path.Combine(Root, "enabledness-config.json");

        private readonly string BinanceArbConfigFileName = Path.Combine(Root, "binance-arb-config.json");

        private readonly string BlocktradeApiConfigFileName = Path.Combine(Root, "blocktrade-api-config.json");

        private readonly string InfuraApiConfigFileName = Path.Combine(Root, "infura-api-config.json");

        public ConfigRepo()
        {
            if (!Directory.Exists(Root)) { Directory.CreateDirectory(Root); }
        }

        public void SetIntegrationEnabledness(string name, bool isEnabled)
        {
            throw new NotImplementedException();
        }

        public bool GetIntegrationEnabledness(string name)
        {
            throw new NotImplementedException();
        }

        public string GetMewWalletAddress()
        {
            return GetEncryptedItem<MewContainer>(MewFileName)?.MewAddress;
        }

        public void SetEthAddress(string value)
        {
            SetEncryptedItem(MewFileName, new MewContainer { MewAddress = value });
        }
        
        public string GetConnectionString()
        {
            return GetEncryptedItem<DatabaseConfiguration>(DatabaseConfigFileName)
                ?.ConnectionString;
        }

        public void SetConnectionString(string connectionString)
        {
            SetEncryptedItem(DatabaseConfigFileName, new DatabaseConfiguration { ConnectionString = connectionString });
        }

        public ApiKey GetKrakenApiKey()
        {
            return GetEncryptedItem<ApiKey>(KrakenFileName);
        }

        public void SetKrakenApiKey(ApiKey apiKey)
        {
            SetEncryptedItem(KrakenFileName, apiKey);
        }

        public ApiKey GetHitBtcApiKey()
        {
            return GetEncryptedItem<ApiKey>(HitBtcFileName);
        }

        public void SetHitBtcApiKey(ApiKey apiKey)
        {
            SetEncryptedItem(HitBtcFileName, apiKey);
        }

        public ApiKey GetBinanceApiKey()
        {
            return GetEncryptedItem<ApiKey>(BinanceFileName);            
        }

        public void SetBinanceApiKey(ApiKey apiKey)
        {
            SetEncryptedItem(BinanceFileName, apiKey);
        }

        public void SetEtherscanApiKey(string apiKey)
        {
            SetEncryptedItem(EtherScanFileName, new ApiKey { Key = apiKey, Secret = null });
        }

        public string GetEtherscanApiKey()
        {
            var apiKey = GetEncryptedItem<ApiKey>(EtherScanFileName);
            return apiKey?.Key;
        }

        public ApiKey GetLivecoinApiKey()
        {
            return GetEncryptedItem<ApiKey>(LivecoinFileName);
        }

        public void SetLivecoinApiKey(ApiKey apiKey)
        {
            SetEncryptedItem(LivecoinFileName, apiKey);
        }

        public ApiKey GetKucoinApiKey()
        {
            return GetEncryptedItem<ApiKey>(KucoinFileName);
        }

        public void SetKucoinApiKey(ApiKey apiKey)
        {
            SetEncryptedItem(KucoinFileName, apiKey);
        }

        public ApiKey GetBitzApiKey()
        {
            return GetEncryptedItem<ApiKey>(BitzApiConfigFileName);
        }

        public void SetBitzApiKey(ApiKey apiKey)
        {
            SetEncryptedItem(BitzApiConfigFileName, apiKey);
        }

        public string GetBitzTradePassword()
        {
            var container = GetEncryptedItem<PasswordContainer>(BitzTradePasswordFileName);
            return container?.Password;
        }

        public void SetBitzTradePassword(string password)
        {
            var container = new PasswordContainer { Password = password };
            SetEncryptedItem(BitzTradePasswordFileName, container);
        }

        public UsernameAndPassword GetCossCredentials()
        {
            return GetEncryptedItem<UsernameAndPassword>(CossFileName);
        }

        public void SetCossCredentials(UsernameAndPassword cossCredentails)
        {
            SetEncryptedItem(CossFileName, cossCredentails);
        }

        public UsernameAndPassword GetCossEmailCredentials()
        {
            return GetEncryptedItem<UsernameAndPassword>(CossEmailConfigFileName);
        }

        public void SetCossEmailCredentials(UsernameAndPassword credentials)
        {
            SetEncryptedItem(CossEmailConfigFileName, credentials);
        }

        public UsernameAndPassword GetKucoinEmailCredentials()
        {
            return GetEncryptedItem<UsernameAndPassword>(KucoinEmailConfigFileName);
        }

        public void SetKucoinEmailCredentials(UsernameAndPassword credentials)
        {
            SetEncryptedItem(KucoinEmailConfigFileName, credentials);
        }

        private void SetEncryptedItem<T>(string fileName, T item)
        {
            var contents = item != null ? JsonConvert.SerializeObject(item, Formatting.Indented) : null;

            var pk = ReadPk();
            var encryptionContainer = EncryptionContainer.Create(pk, contents);
            var encryptedContents = JsonConvert.SerializeObject(encryptionContainer, Formatting.Indented);

            File.WriteAllText(fileName, encryptedContents);
        }

        private T GetEncryptedItem<T>(string fileName)
        {
            if (!File.Exists(fileName)) { return default(T); }
            var contents = File.ReadAllText(fileName);
            if (string.IsNullOrWhiteSpace(contents)) { return default(T); }

            var container = JsonConvert.DeserializeObject<EncryptionContainer>(contents);
            if (container == null || container.Encrypted == null || container.Encrypted.Length == 0) { return default(T); }

            var decryptedContents = container.Decrypt(ReadPk());
            return !string.IsNullOrWhiteSpace(decryptedContents)
                ? JsonConvert.DeserializeObject<T>(decryptedContents)
                : default(T);
        }

        private byte[] ReadPk()
        {
            if (!File.Exists(PkFileName)) { throw new ApplicationException("Unable to read encryption key."); }
            var contents = File.ReadAllText(PkFileName);
            if (string.IsNullOrWhiteSpace(contents)) { throw new ApplicationException("Encryption key is invalid."); }

            var container = JsonConvert.DeserializeObject<PkContainer>(contents);
            if (container == null || container.pk == null || container.pk.Length == 0) { throw new ApplicationException("Encryption key is invalid."); }

            return container.pk;
        }

        public UsernameAndPassword GetBitzLoginCredentials()
        {
            return GetEncryptedItem<UsernameAndPassword>(BitzLoginCredentialsFileName);
        }

        public void SetBitzLoginCredentials(UsernameAndPassword credentials)
        {
            SetEncryptedItem(BitzLoginCredentialsFileName, credentials);
        }

        public void SetMewPassword(string password)
        {
            SetEncryptedItem(MewPasswordConfigFileName, new PasswordContainer { Password = password });
        }

        public string GetMewPassword()
        {
            return GetEncryptedItem<PasswordContainer>(MewPasswordConfigFileName)?.Password;
        }

        public void SetMewWalletFileName(string fileName)
        {
            SetEncryptedItem(MewWalletConfigFileName, new FileNameContainer { FileName = fileName });
        }

        public string GetMewWalletFileName()
        {
            return GetEncryptedItem<FileNameContainer>(MewWalletConfigFileName)?.FileName;
        }

        public ApiKey GetCryptopiaApiKey()
        {
            return GetEncryptedItem<ApiKey>(CryptopiaApiConfigFileName);
        }

        public void SetCryptopiaApiKey(ApiKey apiKey)
        {
            SetEncryptedItem(CryptopiaApiConfigFileName, apiKey);
        }

        public ApiKey GetCossApiKey()
        {
            return GetEncryptedItem<ApiKey>(CossApiConfigFileName);
        }

        public void SetCossApiKey(ApiKey apiKey)
        {
            SetEncryptedItem(CossApiConfigFileName, apiKey);
        }

        public ApiKey GetCoinbaseApiKey()
        {
            return GetEncryptedItem<ApiKey>(CoinbaseApiConfigFileName);
        }

        public void SetCoinbaseApiKey(ApiKey apiKey)
        {
            SetEncryptedItem(CoinbaseApiConfigFileName, apiKey);
        }

        public CossAgentConfig GetCossAgentConfig()
        {
            return GetEncryptedItem<CossAgentConfig>(CossAgentConfigFileName);
        }

        public void SetCossAgentConfig(CossAgentConfig config)
        {
            SetEncryptedItem(CossAgentConfigFileName, config);
        }

        public ApiKey GetTwitterApiKey()
        {
            return GetEncryptedItem<ApiKey>(TwitterApiKeyFileName);
        }

        public void SetTwitterApiKey(ApiKey apiKey)
        {
            SetEncryptedItem(TwitterApiKeyFileName, apiKey);
        }

        public AgentConfig GetBitzAgentConfig()
        {
            return GetEncryptedItem<AgentConfig>(BitzAgentConfigFileName);
        }

        public void SetBitzAgentConfig(AgentConfig config)
        {
            SetEncryptedItem(BitzAgentConfigFileName, config);
        }

        public string GetCcxtUrl()
        {
            const string DefaultCcxtUrl = "http://localhost:3010";

            var container = GetEncryptedItem<CcxtConfig>(CcxtConfigFileName);
            var url = !string.IsNullOrWhiteSpace(container?.Url) ? container.Url : DefaultCcxtUrl;
            return url;
        }

        public void SetCcxtUrl(string url)
        {
            var config = GetEncryptedItem<CcxtConfig>(CcxtConfigFileName) ?? new CcxtConfig();
            config.Url = url;

            SetEncryptedItem(CcxtConfigFileName, config);
        }

        public ApiKey GetQryptosApiKey()
        {
            return GetEncryptedItem<ApiKey>(QryptosApiConfigFileName);
        }

        public void SetQryptosApiKey(ApiKey apiKey)
        {
            SetEncryptedItem(QryptosApiConfigFileName, apiKey);
        }

        public BinanceArbConfig GetBinanceArbConfig()
        {
            return GetEncryptedItem<BinanceArbConfig>(BinanceArbConfigFileName);
        }

        public void SetBinanceArbConfig(BinanceArbConfig arbConfig)
        {
            SetEncryptedItem(BinanceArbConfigFileName, arbConfig);
        }

        public void SetBlocktradeApiKey(ApiKey apiKey)
        {
            SetEncryptedItem(BlocktradeApiConfigFileName, apiKey);
        }

        public ApiKey GetBlocktradeApiKey()
        {
            return GetEncryptedItem<ApiKey>(BlocktradeApiConfigFileName);
        }

        public ApiKey GetInfuraApiKey()
        {
            return GetEncryptedItem<ApiKey>(InfuraApiConfigFileName);
        }

        public void SetInfuraApiKey(ApiKey apiKey)
        {
            SetEncryptedItem(InfuraApiConfigFileName, apiKey);
        }

        public string GetKucoinTradePassword()
        {
            return GetEncryptedItem<PasswordContainer>(KucoinTradingPasswordConfigFileName)
                ?.Password;
        }

        public void SetKucoinTradePassword(string tradingPassword)
        {
            SetEncryptedItem(KucoinTradingPasswordConfigFileName, new PasswordContainer
            {
                Password = tradingPassword
            });
        }

        public string GetKucoinApiPassphrase()
        {
            return GetEncryptedItem<PasswordContainer>(KucoinApiPassphraseConfigFileName)
                ?.Password;
        }

        public void SetKucoinApiPassphrase(string passphrase)
        {
            SetEncryptedItem(KucoinApiPassphraseConfigFileName, new PasswordContainer
            {
                Password = passphrase
            });
        }

        private class CcxtConfig
        {
            [JsonProperty("url")]
            public string Url { get; set; }
        }

        private class PkContainer { public byte[] pk { get; set; } }

        private class DatabaseConfiguration { public string ConnectionString { get; set; } }

        private class MewContainer { public string MewAddress { get; set; } }

        private class PasswordContainer { public string Password { get; set; } }

        // TODO: this needs to move to its own class.
        // https://stackoverflow.com/questions/5116977/how-to-check-the-os-version-at-runtime-e-g-windows-or-linux-without-using-a-con#47390306
        private static bool IsLinux => new[] { 4, 6, 128 }.Any(queryPlatformId => queryPlatformId == (int)Environment.OSVersion.Platform);
    }
}
