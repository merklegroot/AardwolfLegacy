using kucoin_arb_service_lib.App;
using service_lib;
using StructureMap;
using System;

namespace kucoin_arb_service_con
{
    public interface IKucoinArbServiceRunner : IServiceRunner
    {
    }

    public class KucoinArbServiceRunner : IKucoinArbServiceRunner
    {
        public event Action OnStarted;

        public void Run(string overriddenQueueName = null)
        {
            var container = Container.For<KucoinArbServiceRegistry>();
            var service = container.GetInstance<IKucoinArbServiceApp>();

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
