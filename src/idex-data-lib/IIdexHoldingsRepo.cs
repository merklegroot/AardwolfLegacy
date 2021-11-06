using idex_model;
using trade_model;

namespace idex_data_lib
{
    public interface IIdexHoldingsRepo
    {
        void Insert(IdexHoldingContainer container);
        HoldingInfo Get();
        Holding GetHoldingForSymbol(string symbol);
    }
}
