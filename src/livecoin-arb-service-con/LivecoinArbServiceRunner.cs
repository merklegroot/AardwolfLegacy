using arb_service_lib;
using livecoin_arb_service_lib.App;

namespace livecoin_arb_service_con
{
    public interface ILivecoinArbServiceRunner : IArbServiceRunner<LivecoinArbServiceApp, LivecoinArbServiceRegistry>
    {
    }

    public class LivecoinArbServiceRunner : ArbServiceRunner<LivecoinArbServiceApp, LivecoinArbServiceRegistry>, ILivecoinArbServiceRunner
    {
    }
}
