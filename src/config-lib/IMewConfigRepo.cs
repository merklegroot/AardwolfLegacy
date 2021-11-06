namespace config_lib
{
    public interface IMewConfigRepo
    {
        string GetMewWalletAddress();
        void SetEthAddress(string value);

        void SetMewPassword(string password);
        string GetMewPassword();

        void SetMewWalletFileName(string fileName);
        string GetMewWalletFileName();
    }
}
