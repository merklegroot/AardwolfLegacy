using StructureMap;

namespace coss_arb_service_test_con
{
    public class CossArbServiceTestConRunner
    {
        public void Run()
        {
            var container = Container.For<CossArbServiceTestConRegistry>();
            var app = container.GetInstance<ICossArbServiceTestConApp>();

            app.Run();
        }
    }
}
