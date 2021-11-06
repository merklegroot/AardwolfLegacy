using trade_model;
using trade_res;

namespace trade_lib
{
    public interface IDepositAddressValidator
    {
        void Validate(string symbol, DepositAddress address);
        void Validate(Commodity commodity, DepositAddress address);
        void ValidateEthOrEthTokenAddress(string address);
    }
}
