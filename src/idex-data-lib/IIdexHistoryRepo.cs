using idex_model;

namespace idex_data_lib
{
    public interface IIdexHistoryRepo
    {
        void Insert(IdexHistoryContainer container);
        IdexHistoryContainer Get();
    }
}
