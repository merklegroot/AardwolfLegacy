using arb_service_lib;
using hitbtc_arb_service_lib.App;
using StructureMap;
using System;

namespace hitbtc_arb_service_con
{
    public interface IHitbtcArbServiceRunner : IArbServiceRunner<HitbtcArbServiceApp, HitbtcArbServiceRegistry>
    {
    }

    public class HitbtcArbServiceRunner : ArbServiceRunner<HitbtcArbServiceApp, HitbtcArbServiceRegistry>, IHitbtcArbServiceRunner
    {
    }
}
