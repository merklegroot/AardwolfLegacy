using bitz_agent_con.IoC;
using bitz_agent_lib.App;
using StructureMap;
using System;

namespace bitz_agent_con
{
    class Program
    {
        static void Main(string[] args)
        {

            var container = Container.For<BitzAgentRegistry>();
            using (var app = container.GetInstance<IBitzAgentApp>())
            {
                app.Run();
            }

            Console.WriteLine("All done.");
            Console.WriteLine("Press the Any key.");

            Console.ReadKey(true);
        }
    }
}
