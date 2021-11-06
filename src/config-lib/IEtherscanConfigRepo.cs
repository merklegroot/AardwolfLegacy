namespace config_lib
{
    public interface IEtherscanConfigRepo
    {
        void SetEtherscanApiKey(string apiKey);
        string GetEtherscanApiKey();
    }
}
