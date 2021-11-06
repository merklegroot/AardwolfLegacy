using arb_service_lib;
using blocktrade_arb_service_lib.Workers;
using log_lib;
using rabbit_lib;
using trade_constants;

namespace blocktrade_arb_service_lib.App
{
    public interface IBlocktradeArbServiceApp : IArbServiceApp { }

    public class BlocktradeArbServiceApp : ArbServiceApp, IBlocktradeArbServiceApp
    {
        public BlocktradeArbServiceApp(            
            IRabbitConnectionFactory rabbitConnectionFactory, 
            IBlocktradeArbWorker blocktradeArbWorker, 
            ILogRepo log) : base(rabbitConnectionFactory, blocktradeArbWorker, log)
        {
        }

        public override string ApplicationName => "Blocktrade Arb Service";

        protected override string BaseQueueName => TradeRabbitConstants.Queues.BlocktradeArbServiceQueue;
    }
}
