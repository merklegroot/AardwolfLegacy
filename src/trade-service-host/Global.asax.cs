using exchange_service_con;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Mvc;

namespace trade_service_host
{
    public class WebApiApplication : System.Web.HttpApplication
    {
        private static ExchangeServiceRunner _runner;
        public static bool IsRunnerStarted = false;

        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            GlobalConfiguration.Configure(WebApiConfig.Register);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);

            _runner = new ExchangeServiceRunner();
            _runner.OnStarted += _runner_OnStarted;
            var runnerTask = new Task(() =>
            {
                _runner.Run();
            }, TaskCreationOptions.LongRunning);

            runnerTask.Start();
        }

        private void _runner_OnStarted()
        {
            IsRunnerStarted = true;
        }
    }
}
