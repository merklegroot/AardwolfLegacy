using trade_model;
using trade_res;

namespace trade_lib
{
    public interface IWithdrawableTradeIntegration
    {
        bool Withdraw(Commodity commodity, decimal quantity, DepositAddress address);
    }
}
