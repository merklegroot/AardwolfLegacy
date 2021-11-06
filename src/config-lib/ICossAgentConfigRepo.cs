using config_model;

namespace config_lib
{
    public interface ICossAgentConfigRepo
    {
        CossAgentConfig GetCossAgentConfig();
        void SetCossAgentConfig(CossAgentConfig config);
    }
}
