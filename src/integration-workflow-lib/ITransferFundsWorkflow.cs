using trade_lib;
using trade_res;

namespace integration_workflow_lib
{
    public interface ITransferFundsWorkflow
    {
        void Transfer(
            string source,
            string destination,
            Commodity commodity,
            bool shouldTransferAll,
            decimal? quantity = null);
    }
}
