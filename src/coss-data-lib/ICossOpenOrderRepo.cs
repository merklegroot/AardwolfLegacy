using coss_data_model;
using System.Collections.Generic;
using trade_model;

namespace coss_data_lib
{
    public interface ICossOpenOrderRepo
    {
        void Insert(CossOpenOrdersForTradingPairContainer container);
        List<OpenOrder> Get(string symbol, string baseSymbol);        
    }
}
