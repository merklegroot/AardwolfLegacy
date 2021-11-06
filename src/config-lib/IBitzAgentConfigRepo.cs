using config_model;

namespace config_lib
{
    public interface IBitzAgentConfigRepo
    {
        AgentConfig GetBitzAgentConfig();
        void SetBitzAgentConfig(AgentConfig config);
    }
}
