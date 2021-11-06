namespace service_lib
{
    public abstract class ServiceRunner : IServiceRunner
    {       
        public abstract void Run(string overriddenQueueName = null);
    }
}
