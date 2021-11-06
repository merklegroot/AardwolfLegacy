using coss_arb_service_lib.App;
using service_lib;
using StructureMap;
using System;

namespace coss_arb_service_con
{
    public interface ICossArbServiceRunner : IServiceRunner
    {
    }

    public class CossArbServiceRunner : ICossArbServiceRunner
    {
        public event Action OnStarted;

        public void Run(string overriddenQueueName = null)
        {
            var container = Container.For<CossArbServiceRegistry>();
            var service = container.GetInstance<ICossArbServiceApp>();

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
