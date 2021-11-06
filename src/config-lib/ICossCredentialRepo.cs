using config_model;

namespace config_lib
{
    public interface ICossCredentialRepo
    {
        UsernameAndPassword GetCossCredentials();
        void SetCossCredentials(UsernameAndPassword cossCredentails);

        UsernameAndPassword GetCossEmailCredentials();
        void SetCossEmailCredentials(UsernameAndPassword credentials);
    }
}
