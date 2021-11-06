using idex_model;
using System.Collections.Generic;
using trade_model;

namespace idex_data_lib
{
    public interface IIdexOpenOrdersRepo
    {
        void Insert(IdexOpenOrdersContainer container);
        List<OpenOrderForTradingPair> Get();
    }
}
