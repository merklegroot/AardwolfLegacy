using service_lib;
using StructureMap;
using System;

namespace arb_service_lib
{
    public interface IArbServiceRunner<TServiceApp, TServiceRegistry> : IServiceRunner
        where TServiceApp : IArbServiceApp
        where TServiceRegistry : Registry, new()
    {
    }

    public abstract class ArbServiceRunner<TServiceApp, TServiceRegistry> : IArbServiceRunner<TServiceApp, TServiceRegistry>
        where TServiceApp : IArbServiceApp
        where TServiceRegistry: Registry, new()
    {
        public event Action OnStarted;

        public virtual void Run(string overriddenQueueName = null)
        {
            var container = Container.For<TServiceRegistry>();
            var service = container.GetInstance<TServiceApp>();

            if (!string.IsNullOrWhiteSpace(overriddenQueueName))
            {
                service.OverrideQueue(overriddenQueueName);
            }

            service.OnStarted += () => { OnStarted?.Invoke(); };
            try
            {
                service.RunBackgroundProcess();
                service.Run();
            }
            finally
            {
                service.Dispose();
            }
        }        
    }
}
