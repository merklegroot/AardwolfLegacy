using bit_z_model;

namespace bitz_data_lib
{
    public interface IBitzTradeHistoryRepo
    {
        void InsertTradeHistory(BitzTradeHistoryContainer container);
    }
}
