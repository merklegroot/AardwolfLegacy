using idex_model;

namespace idex_data_lib
{
    public interface IIdexFrameRepo
    {
        void Insert(IdexFrameContainer container);
        void TruncateOldData();
    }
}
