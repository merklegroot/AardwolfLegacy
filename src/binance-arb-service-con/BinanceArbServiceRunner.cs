using arb_service_lib;
using binance_arb_service_lib.App;

namespace binance_arb_service_con
{
    public interface IBinanceArbServiceRunner : IArbServiceRunner<BinanceArbServiceApp, BinanceArbServiceRegistry>
    {
    }

    public class BinanceArbServiceRunner : ArbServiceRunner<BinanceArbServiceApp, BinanceArbServiceRegistry>, IBinanceArbServiceRunner
    {
    }
}
