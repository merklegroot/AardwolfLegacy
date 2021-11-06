using System.Collections.Generic;

namespace trade_constants
{
    public static class TradeRabbitConstants
    {
        public static class Queues
        {
            public const string EtherscanAgentQueue = "etherscan-agent";
            public const string MewAgentQueue = "mew-agent";
            public const string IdexAgentQueue = "idex-agent";
            public const string KucoinAgentQueue = "kucoin-agent";
            public const string CossAgentQueue = "coss-agent";
            public const string BitzBrowserAgentQueue = "bitz-browser-agent";

            public const string ConfigServiceQueue = "config-service";
            public const string ExchangeServiceQueue = "exchange-service";
            public const string CryptoCompareServiceQueue = "cryptocompare-service";
            public const string WorkflowServiceQueue = "workflow-service";
            public const string CossBrowserServiceQueue = "coss-browser-service";
            public const string CossArbServiceQueue = "coss-arb-service";
            public const string QryptosArbServiceQueue = "qryptos-arb-service";
            public const string BitzArbServiceQueue = "bitz-arb-service";
            public const string KucoinArbServiceQueue = "kucoin-arb-service";
            public const string HitbtcArbServiceQueue = "hitbtc-arb-service";
            public const string BinanceArbServiceQueue = "binance-arb-service";
            public const string BlocktradeArbServiceQueue = "blocktrade-arb-service";
            public const string LivecoinArbServiceQueue = "livecoin-arb-service";

            public const string BrowserAutomationServiceQueue = "browser-automation-service";
        }

        public static class Messages
        {
            public const string UpdateFunds = "update-funds";
            public const string OpenUrl = "open-url";
        }
    }
}
