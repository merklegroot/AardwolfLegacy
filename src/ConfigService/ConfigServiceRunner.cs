using config_service_lib;
using service_lib;
using StructureMap;
using System;

namespace config_service_con
{
    public interface IConfigServiceRunner : IServiceRunner
    { }

    public class ConfigServiceRunner : ServiceRunner, IConfigServiceRunner
    {
        public event Action OnStarted;

        public override void Run(string overriddenQueueName = null)
        {
            var container = Container.For<ConfigServiceRegistry>();

            var service = container.GetInstance<IConfigServiceApp>();
            if (!string.IsNullOrWhiteSpace(overriddenQueueName))
            {
                service.OverrideQueue(overriddenQueueName);
            }

            service.OnStarted += () => { OnStarted?.Invoke(); };
            try
            {
                service.Run();
            }
            finally
            {
                service.Dispose();
            }
        }
    }
}
