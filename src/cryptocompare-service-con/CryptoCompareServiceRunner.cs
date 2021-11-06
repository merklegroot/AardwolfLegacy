using cryptocompare_service_lib.App;
using StructureMap;
using System;

namespace cryptocompare_service_con
{
    public class CryptoCompareServiceRunner
    {
        public event Action OnStarted;

        public void Run(string overriddenQueueName = null)
        {
            var container = Container.For<CryptoCompareServiceRegistry>();
            var service = container.GetInstance<ICryptoCompareServiceApp>();

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
