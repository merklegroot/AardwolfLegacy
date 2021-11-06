using mew_agent_con.Models;
using System;
using trade_res;

namespace mew_agent_con
{
    public interface IMewBrowser : IDisposable
    {
        void Run();
        bool Login();
                
        void Send(Commodity commodity, QuantityToSend quantity, string integrationName);
        void SendAllToBinance(Commodity commodity);

        void SendAllLendToBinance();
        void SendAllIcnToBinance();
        void SendAllIotxToBinance();
        void SendAllPoeToBinance();
        void SendAllQuarkChainToBinance();
        void SendAllSubToBinance();
        void SendAllNcashToBinance();
        void SendAllOmisegoToBinance();
        void SendAllBancorToBinance();
        void SendAllMonacoToBinance();
        void SendAllArnToBinance();
        void SendAllSonmToBinance();

        void SendEthToBinance(QuantityToSend quantity);
        void SendEthToCoss(QuantityToSend quantity);
    }
}
