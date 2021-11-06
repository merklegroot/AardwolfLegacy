namespace service_lib
{
    public interface IServiceRunner
    {
        void Run(string overriddenQueueName = null);
    }
}
