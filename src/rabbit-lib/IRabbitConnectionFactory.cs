namespace rabbit_lib
{
    public interface IRabbitConnectionFactory
    {
        IRabbitConnection Connect();
        void OverrideConfigKey(string configKey);
        void UseDefaultConfigKey();
    }
}
