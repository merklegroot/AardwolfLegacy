using StructureMap;

namespace exchange_service_test_con.App
{
    public class TestAppBootstrapper
    {
        public void Bootstrap()
        {
            var container = Container.For<TestAppServiceRegistry>();
            var testApp = container.GetInstance<ITestApp>();

            testApp.Run();
        }
    }
}
