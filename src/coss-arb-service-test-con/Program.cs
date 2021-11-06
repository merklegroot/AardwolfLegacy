using System;

namespace coss_arb_service_test_con
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                var app = new CossArbServiceTestConRunner();
                app.Run();
            }
            catch(Exception exception)
            {
                Console.WriteLine(exception);
            }
        }
    }
}
