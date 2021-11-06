using StructureMap;

namespace coss_browser_service_test_con
{
    public class CossBrowserServiceTestConRunner
    {
        public void Run()
        {
            var container = Container.For<CossBrowserServiceTestConRegistry>();
            var app = container.GetInstance<ICossBrowserServiceTestConApp>();

            app.Run();
        }
    }
}
