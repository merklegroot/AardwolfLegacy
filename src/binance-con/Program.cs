using System;

namespace binance_con
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Starting application...");
            try
            {
                new App().Run();
            }
            catch(Exception exception)
            {
                Console.WriteLine(exception);
            }

            Console.WriteLine("Done. Press any key.");
            Console.ReadKey(true);
        }
    }
}
