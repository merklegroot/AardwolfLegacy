using crypt_lib;
using env_config_lib.Constants;
using env_config_lib.Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace env_config_lib
{
    public class EnvironmentConfigRepo : IEnvironmentConfigRepo
    {
        private const string LinuxEnvFileName = @"/trade/config/env.json";

        private static Random _random = new Random();
        private string _configKeyOverride = null;

        private string RabbitConfigKey
            => !string.IsNullOrWhiteSpace(_configKeyOverride)
            ? _configKeyOverride
            : EnvironmentConstants.DefaultRabbitConfigKey;

        private byte[] PkBytes => Convert.FromBase64String(EnvironmentConstants.Pk);

        public EnvironmentConfigRepo()
        {
        }

        public EnvironmentConfigRepo(string configKeyOverride)
        {
            _configKeyOverride = configKeyOverride;
        }

        public void OverrideConfigKey(string configKeyOverride)
        {
            _configKeyOverride = configKeyOverride;
        }

        public void UseDefaultConfigKey()
        {
            _configKeyOverride = null;
        }

        public RabbitClientConfig GetRabbitClientConfig()
        {
            string serialized = null;
            if (!string.IsNullOrWhiteSpace(_configKeyOverride))
            {
                var encryptedValue = GetEnvironmentVariable(_configKeyOverride);
                if (!string.IsNullOrWhiteSpace(encryptedValue))
                {
                    serialized = GetEnvironmentVariableAndDecrypt(RabbitConfigKey);
                }
            }

            if (string.IsNullOrWhiteSpace(serialized))
            {
                serialized = GetEnvironmentVariableAndDecrypt(RabbitConfigKey);
            }

            return !string.IsNullOrWhiteSpace(serialized)
                ? JsonConvert.DeserializeObject<RabbitClientConfig>(serialized)
                : null;
        }

        public void SetRabbitClientConfig(RabbitClientConfig value)
        {
            var serialized = JsonConvert.SerializeObject(value);
            EncryptAndSetEnvironmentVariable(RabbitConfigKey, serialized);
        }

        private void EncryptAndSetEnvironmentVariable(string key, string value)
        {
            var iv = new byte[16];
            _random.NextBytes(iv);

            var encryptedBytes = SymmUtil.EncryptStringToBytes(value, PkBytes, iv);
            var combo = new byte[iv.Length + encryptedBytes.Length];
            Array.Copy(iv, combo, iv.Length);
            Array.Copy(encryptedBytes, 0, combo, iv.Length, encryptedBytes.Length);
            var combo64 = Convert.ToBase64String(combo);

            SetEnvironmentVariable(RabbitConfigKey, combo64);
        }

        private string GetEnvironmentVariableAndDecrypt(string key)
        {
            var encryptedText = GetEnvironmentVariable(key);
            if (string.IsNullOrWhiteSpace(encryptedText)) { return encryptedText; }

            var encryptedComboData = Convert.FromBase64String(encryptedText);
            if (encryptedComboData.Length <= 0) { return null; }
            if (encryptedComboData.Length < 16) { throw new ApplicationException($"Failed to decrypt environment variable \"{key}\"."); }

            var iv = new byte[16];
            Array.Copy(encryptedComboData, iv, iv.Length);
            var encryptedBytes = new byte[encryptedComboData.Length - 16];
            Array.Copy(encryptedComboData, iv.Length, encryptedBytes, 0, encryptedBytes.Length);

            return SymmUtil.DecryptStringFromBytes(encryptedBytes, PkBytes, iv);
        }

        private Dictionary<string, string> ReadLinuxEnvDictionary()
        {
            var contents = File.Exists(LinuxEnvFileName) ? File.ReadAllText(LinuxEnvFileName) : null;
            return !string.IsNullOrWhiteSpace(contents)
                ? JsonConvert.DeserializeObject<Dictionary<string, string>>(contents)
                : new Dictionary<string, string>();
        }

        private void WriteLinuxEnvDictionary(Dictionary<string, string> envDictionary)
        {
            var contents = JsonConvert.SerializeObject(envDictionary, Formatting.Indented);
            File.WriteAllText(LinuxEnvFileName, contents);
        }

        private string GetEnvironmentVariable(string key)
        {
            if (!IsLinux) { return Environment.GetEnvironmentVariable(key, EnvironmentVariableTarget.Machine); }
     
            var dict = ReadLinuxEnvDictionary();
            return dict.ContainsKey(key) ? dict[key] : null;
        }

        private void SetEnvironmentVariable(string key, string value)
        {
            if (!IsLinux) { Environment.SetEnvironmentVariable(key, value, EnvironmentVariableTarget.Machine); }

            var dict = ReadLinuxEnvDictionary() ?? new Dictionary<string, string>();
            dict[key] = value;
            WriteLinuxEnvDictionary(dict);            
        }

        // https://stackoverflow.com/questions/5116977/how-to-check-the-os-version-at-runtime-e-g-windows-or-linux-without-using-a-con#47390306
        public static bool IsLinux => new[] { 4, 6, 128 }.Any(queryPlatformId => queryPlatformId == (int)Environment.OSVersion.Platform);
    }
}
