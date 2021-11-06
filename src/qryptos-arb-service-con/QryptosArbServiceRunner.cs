using qryptos_arb_service_lib.App;
using service_lib;
using StructureMap;
using System;

namespace qryptos_arb_service_con
{
    public interface IQryptosArbServiceRunner : IServiceRunner
    {
    }

    public class QryptosArbServiceRunner : IQryptosArbServiceRunner
    {
        public event Action OnStarted;

        public void Run(string overriddenQueueName = null)
        {
            var container = Container.For<QryptosArbServiceRegistry>();
            var service = container.GetInstance<IQryptosArbServiceApp>();

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
