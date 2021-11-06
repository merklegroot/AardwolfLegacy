using bitz_arb_service_lib.App;
using service_lib;
using StructureMap;
using System;

namespace bitz_arb_service_con
{
    public interface IBitzArbServiceRunner : IServiceRunner
    {
    }

    public class BitzArbServiceRunner : IBitzArbServiceRunner
    {
        public event Action OnStarted;

        public void Run(string overriddenQueueName = null)
        {
            var container = Container.For<BitzArbServiceRegistry>();
            var service = container.GetInstance<IBitzArbServiceApp>();

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
