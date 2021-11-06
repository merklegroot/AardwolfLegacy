using arb_service_lib;
using blocktrade_arb_service_lib.App;

namespace blocktrade_arb_service_con
{
    public interface IBlocktradeArbServiceRunner : IArbServiceRunner<IBlocktradeArbServiceApp, BlocktradeArbServiceRegistry>
    {
    }

    public class BlocktradeArbServiceRunner : ArbServiceRunner<IBlocktradeArbServiceApp, BlocktradeArbServiceRegistry>, IBlocktradeArbServiceRunner
    {
    }
}
