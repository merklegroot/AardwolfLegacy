using etherscan_lib.Models;
using trade_model;

namespace etherscan_lib
{
    public interface IEtherscanHoldingRepo
    {
        void Insert(EtherScanTokenHoldingContainer container);
        HoldingInfo Get();
    }
}
