using etherscan_lib.Models;
using System.Collections.Generic;
using trade_model;

namespace etherscan_lib
{
    public interface IEtherscanHistoryRepo
    {
        void Insert(EtherscanTransactionHistoryContainer container);
        List<string> GetTransactionHashes();
        List<HistoricalTrade> Get();

        void InsertTransaction(EtherscanTransactionContainer container);
        EtherscanTransactionContainer GetTransaction(string transactionHash);
    }
}
