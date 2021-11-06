using binance_lib;
using coss_lib;
using hitbtc_lib;
using idex_integration_lib;
using StructureMap;
using System;
using trade_ioc;

namespace idex_con
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                var container = Container.For<DefaultRegistry>();
                var idexIntegration = container.GetInstance<IIdexIntegration>();
                var hitBtcIntegration = container.GetInstance<IHitBtcIntegration>();
                var binanceIntegration = container.GetInstance<IBinanceIntegration>();
                var cossIntegration = container.GetInstance<ICossIntegration>();

                new App(idexIntegration, hitBtcIntegration, binanceIntegration, cossIntegration).Run();
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception.Message);
            }

            Console.WriteLine("All done!");
            Console.ReadKey(true);
        }
    }
}
