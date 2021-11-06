using config_connection_string_lib;
using env_config_lib;
using exchange_service_lib.App;
using Newtonsoft.Json;
using service_lib;
using StructureMap;
using System;
using web_util;

namespace exchange_service_con
{
    public interface IExchangeServiceRunner : IServiceRunner
    { }

    public class ExchangeServiceRunner : ServiceRunner, IExchangeServiceRunner
    {
        public event Action OnStarted;

        public void RunTestingStuff(string overriddenQueueName = null)
        {
            Console.WriteLine("Loading container...");
            var container = Container.For<ExchangeServiceRegistry>();
            Console.WriteLine("Container loaded.");

            Console.WriteLine("Resolving IWebUtil...");
            var webUtil = container.GetInstance<IWebUtil>();
            Console.WriteLine("Resolved.");

            Console.WriteLine("Resolving IEnvironmentConfigRepo...");
            var environmentConfigRepo = container.GetInstance<IEnvironmentConfigRepo>();
            Console.WriteLine("Resolved.");
            
            Console.WriteLine("Getting the rabbitClientConfig...");
            var rabbitClientConfig = environmentConfigRepo.GetRabbitClientConfig();
            var serializedConfig = JsonConvert.SerializeObject(rabbitClientConfig);
            Console.WriteLine(serializedConfig);

            Console.WriteLine("Resolving ILogRepo...");
            var getConnectionString = container.GetInstance<IGetConnectionString>();
            Console.WriteLine("Resolved.");

            Console.WriteLine("Getting the connection string...");
            var conn = getConnectionString.GetConnectionString();
            Console.WriteLine($"Connection String: \"{conn}\"");

            Console.WriteLine("we are done here..");
        }

        public override void Run(string overriddenQueueName = null)
        {
            var container = Container.For<ExchangeServiceRegistry>();
            var service = container.GetInstance<IExchangeServiceApp>();

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
