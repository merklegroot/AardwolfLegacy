using bitz_browser_lib;
using bitz_data_lib;
using config_client_lib;
using env_config_lib;
using log_lib;
using rabbit_lib;
using tfa_lib;
using wait_for_it_lib;
using web_util;

namespace bitz_browser_con
{
    class Program
    {
        static void Main(string[] args)
        {
            var configClient = new ConfigClient();
            var tradeHistoryRepo = new BitzTradeHistoryRepo(configClient);
            var fundsRepo = new BitzFundsRepo(configClient);
            var webUtil = new WebUtil();
            var tfaUtil = new TfaUtil(webUtil);
            var waitForIt = new WaitForIt();
            var envConfig = new EnvironmentConfigRepo();
            var rabbitConnectionFactory = new RabbitConnectionFactory(envConfig);
            var log = new LogRepo();

            var browserUtil = new BitzBrowserUtil(configClient, tradeHistoryRepo, fundsRepo, tfaUtil, waitForIt, log);

            var app = new App(rabbitConnectionFactory, browserUtil, log);

            app.Run();
        }
    }
}
