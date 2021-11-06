using coss_data_model;
using System.Collections.Generic;
using trade_model;

namespace coss_data_lib
{
    public interface ICossHistoryRepo
    {
        void Insert(CossResponseContainer<CossExchangeHistoryResponse> container);
        void Insert(CossResponseContainer<CossDepositAndWithdrawalHistoryResponse> container);
        List<HistoricalTrade> Get();
    }
}
