using refesher_con.IoC;
using refresher_lib;
using StructureMap;

namespace refesher_con
{
    class Program
    {
        static void Main(string[] args)
        {
            var container = Container.For<RefresherRegistry>();

            var app = container.GetInstance<IRefresherApp>();
            app.Run();
        }
    }
}
