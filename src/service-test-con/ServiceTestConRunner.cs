using StructureMap;

namespace service_test_con
{
    public class ServiceTestConRunner
    {
        public void Run()
        {
            var container = Container.For<ServiceTestConRegistry>();
            var app = container.GetInstance<IServiceTestConApp>();

            app.Run();
        }
    }
}
