using System;

namespace service_test_con
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                var app = new ServiceTestConRunner();
                app.Run();
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
        }
    }
}
