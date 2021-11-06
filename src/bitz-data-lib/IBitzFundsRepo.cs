using bit_z_model;
using trade_model;

namespace bitz_data_lib
{
    public interface IBitzFundsRepo
    {
        void Insert(BitzFundsContainer container);

        BitzFundsContainer GetMostRecent();

        BitzFund GetBitzFund(string symbol);

        DepositAddress GetDepositAddress(string symbol);
    }
}
