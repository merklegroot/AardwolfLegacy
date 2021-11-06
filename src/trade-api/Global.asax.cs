using env_config_lib;
using rabbit_lib;
using System;
using System.Web.Http;
using System.Web.Mvc;

namespace trade_api
{
    public class WebApiApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            try
            {
                ConnectionContainer.Connection = new RabbitConnectionFactory(new EnvironmentConfigRepo()).Connect();
                new RabbitWorkflow().Initialize(ConnectionContainer.Connection);
            }
            catch(Exception exception)
            {
                Console.WriteLine(exception);
            }

            AreaRegistration.RegisterAllAreas();
            GlobalConfiguration.Configure(WebApiConfig.Register);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
        }

        protected void Application_Stop()
        {
            if (ConnectionContainer.Connection != null) { ConnectionContainer.Connection.Dispose(); }
        }
    }
}
