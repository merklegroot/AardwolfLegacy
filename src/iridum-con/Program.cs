using iridum_con.App;
using StructureMap;
using System;

namespace iridum_con
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                var container = Container.For<IridiumRegistry>();
                var app = container.GetInstance<IIridiumApp>();
                app.Run();
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                Console.WriteLine("Press the any key.");
                Console.ReadKey(true);
            }
        }
    }
}
