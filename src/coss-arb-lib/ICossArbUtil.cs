namespace coss_arb_lib
{
    public interface ICossArbUtil
    {
        void OpenBid();
        void AutoSell();
        void AutoEthBtc();
        void AutoEthBtcV2();

        void AutoEthGusd();
        void AutoEthUsdc();
        void AutoEthUsdt();
        void AutoBtcGusd();
        void AutoBtcUsdc();
        void AutoBtcUsdt();

        void AutoSymbol(string symbol, string compExchange);
        void AutoTusdWithReverseBinanceSymbol(string binanceSymbol);
        void AcquireAgainstBinanceSymbolV5(string sym);
        void AcquireArkCoss();
        void AcquireCossV4();
        void AcquireLtc();
        void AcquireBchabc();
        void AcquireXdceTusd();
        void AcquireXdce();

        void AcquireBwtTusd();
        void AcquireBwtGusd();

        void AutoCossBinance(string symbol, string quoteSymbol);
    }
}
