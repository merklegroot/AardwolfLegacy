using exchange_service_test_con.App;
using System;

namespace exchange_service_test_con
{
    class Program
    {
        static void Main(string[] args)
        {
            new TestAppBootstrapper().Bootstrap();
            Console.WriteLine("Press the any key");
            Console.ReadKey(true);
        }
    }
}
