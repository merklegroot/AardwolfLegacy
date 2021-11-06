using System;

namespace coss_browser_service_test_con
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                new CossBrowserServiceTestConRunner().Run();
            }
            catch(Exception exception)
            {
                Console.WriteLine(exception);
            }
        }
    }
}
