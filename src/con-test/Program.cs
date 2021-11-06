using System;

namespace con_test
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                new App().Run();
            }
            catch(Exception exception)
            {
                Console.WriteLine("Exception:");
                Console.WriteLine(exception);
                if(exception.InnerException != null)
                {
                    Console.WriteLine("Inner Exception:");
                    Console.WriteLine(exception.InnerException);
                }
            }

            Console.WriteLine("All done.");
            Console.WriteLine("Press a key to exit.");
            Console.ReadKey(true);
        }
    }
}
