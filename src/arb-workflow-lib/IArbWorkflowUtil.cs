using System.Collections.Generic;
using trade_model;

namespace arb_workflow_lib
{
    public interface IArbWorkflowUtil
    {
        void AutoSymbol(
            string symbol,
            string arbExchange,
            string compExchange,
            string altBaseSymbol = null,
            bool waiveArbDepositAndWithdrawalCheck = false,
            bool waiveCompDepositAndWithdrawalCheck = false,
            decimal? maxUsdValueToOwn = null,
            decimal? idealPercentDiff = null,
            Dictionary<string, decimal> openBidQuantityOverride = null);

        void KucoinUsdc();

        void AutoSell(string exchange, string symbol, string baseSymbol, decimal? maxToAcquire = null);

        void AutoStraddle(string exchange, string symbol, string baseSymbol);

        void AcquireUsdcEth();

        void AutoEthXusd(string exchange, string dollarSymbol, bool shouldBuy, bool shouldSell);

        void AutoReverseXusd(string exchange, string dollarSymbol, string cryptoSymbol);

        void AutoEthBtc(string exchange);
    }
}
