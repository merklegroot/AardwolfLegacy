using exchange_service_con;

namespace exchange_service_test_con.App
{
    public class TestAppServiceRegistry : ExchangeServiceRegistry
    {
        public TestAppServiceRegistry() : base()
        {
            For<ITestApp>().Use<TestApp>();
        }
    }
}
