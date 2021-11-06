using trade_model;

namespace trade_lib
{
    public interface ITradeWithSettableDepositAddress
    {
        void SetDepositAddress(DepositAddress depositAddress);
    }
}
