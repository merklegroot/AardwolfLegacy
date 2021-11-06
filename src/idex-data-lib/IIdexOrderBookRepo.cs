using idex_model;

namespace idex_data_lib
{
    public interface IIdexOrderBookRepo
    {
        void Insert(IdexOrderBookContainer container);
        IdexOrderBookContainer Get(string symbol, string baseSymbol);
    }
}
