using env_config_lib.Model;

namespace env_config_lib
{
    public interface IEnvironmentConfigRepo
    {
        RabbitClientConfig GetRabbitClientConfig();
        void SetRabbitClientConfig(RabbitClientConfig value);

        void OverrideConfigKey(string configKeyOverride);
        void UseDefaultConfigKey();
    }
}
