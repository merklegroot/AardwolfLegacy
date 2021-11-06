using browser_lib;
using env_config_lib;
using log_lib;
using rabbit_lib;
using wait_for_it_lib;

namespace kucoin_agent
{
    class Program
    {
        static void Main(string[] args)
        {
            var waitForIt = new WaitForIt();
            var browserUtil = new BrowserUtil(waitForIt);
            var envConfig = new EnvironmentConfigRepo();
            var rabbitConnectionFactory = new RabbitConnectionFactory(envConfig);
            var log = new LogRepo();

            using (var app = new App(browserUtil, waitForIt, rabbitConnectionFactory, log))
            {
                app.Run();
            }
        }
    }
}
