using env_config_lib;
using System;

namespace env_config_con
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                var envConfigRepo = new EnvironmentConfigRepo();
                var app = new EnvConfigApp(envConfigRepo);
                app.Run();
            }
            catch(Exception exception)
            {
                Console.WriteLine(exception);
            }
        }
    }
}
