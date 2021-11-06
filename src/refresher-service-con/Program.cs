using refesher_service_con.IoC;
using refresher_service_lib.App;
using StructureMap;

namespace refresher_con
{
    class Program
    {
        static void Main(string[] args)
        {
            var container = Container.For<RefresherRegistry>();

            var app = container.GetInstance<IRefresherServiceApp>();
            app.Run();
        }
    }
}
