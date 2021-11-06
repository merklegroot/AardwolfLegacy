using binance_lib;
using bit_z_lib;
using bitz_data_lib;
using cache_lib;
using config_connection_string_lib;
using coss_browser_service_client;
using coss_cookie_lib;
using coss_data_lib;
using coss_lib;
using cryptocompare_lib;
using cryptopia_lib;
using env_config_lib;
using etherscan_lib;
using exchange_service_lib.App;
using exchange_service_lib.Handlers;
using exchange_service_lib.Workflows;
using hitbtc_lib;
using idex_data_lib;
using idex_integration_lib;
using client_lib;
using kraken_integration_lib;
using kucoin_lib;
using livecoin_lib;
using log_lib;
using mew_integration_lib;
using qryptos_lib;
using rabbit_lib;
using StructureMap;
using System;
using tidex_integration_library;
using trade_email_lib;
using trade_node_integration;
using wait_for_it_lib;
using web_util;
using yobit_lib;
using exchange_client_lib;
using config_client_lib;
using browser_automation_client_lib;
using coinbase_lib;
using tfa_lib;
using trade_lib.Repo;
using qryptos_lib.Client;
using hitbtc_lib.Client;
using livecoin_lib.Client;
using oex_lib.Client;
using oex_lib;
using yobit_lib.Client;
using coss_api_client_lib;
using gemini_lib;
using gemini_lib.Client;
using blocktrade_lib;
using BlocktradeExchangeLib;
using idex_client_lib;
using kucoin_lib.Client;

namespace exchange_service_con
{
    public class ExchangeServiceRegistry : Registry
    {
        public ExchangeServiceRegistry()
        {
            var logRepoFactory = new Func<ILogRepo>(() =>
            {
                var getter = new Func<string>(() => new ConfigClient().GetConnectionString());

                return new LogRepo(getter);
            });

            For<ILogRepo>().Use(() => logRepoFactory());

            For<IBrowserAutomationClient>().Use<BrowserAutomationClient>();
            For<IWaitForIt>().Use<WaitForIt>();
            For<IWebUtil>().Use<WebUtil>();
            For<IRequestResponse>().Use<RequestResponse>();

            For<IGetConnectionString>().Use<ConfigClient>();
            For<IConfigClient>().Use<ConfigClient>();

            For<ITradeEmailUtil>().Use<TradeEmailUtil>();
            For<ITradeNodeUtil>().Use<TradeNodeUtil>();
            For<ITfaUtil>().Use<TfaUtil>();

            For<IOpenOrdersSnapshotRepo>().Use<OpenOrdersSnapshotRepo>();

            For<IHitBtcClient>().Use<HitBtcClient>();
            For<IHitBtcIntegration>().Use<HitBtcIntegration>();
            For<IBinanceIntegration>().Use<BinanceIntegration>();
            For<ICryptopiaIntegration>().Use<CryptopiaIntegration>();

            For<IBitzClient>().Use<BitzClient>();
            For<IBitzFundsRepo>().Use<BitzFundsRepo>();
            For<IBitzIntegration>().Use<BitzIntegration>();

            For<ICossBrowserClient>().Use<CossBrowserClient>();
            For<ICossCookieUtil>().Use<CossCookieUtil>();
            For<ICossHistoryRepo>().Use<CossHistoryRepo>();
            For<ICossXhrOpenOrderRepo>().Use<CossXhrOpenOrderRepo>();
            For<ICossOpenOrderRepo>().Use<CossOpenOrderRepo>();
            For<ICossApiClient>().Use<CossApiClient>();
            For<ICossIntegration>().Use<CossIntegration>();

            For<IBlocktradeClient>().Use<BlocktradeClient>();
            For<IWebClient>().Use<WebClient>();
            For<IBlockTradeExchange>().Use<BlockTradeExchange>();

            For<IIdexHoldingsRepo>().Use<IdexHoldingsRepo>();
            For<IIdexOrderBookRepo>().Use<IdexOrderBookRepo>();
            For<IIdexOpenOrdersRepo>().Use<IdexOpenOrdersRepo>();
            For<IIdexHistoryRepo>().Use<IdexHistoryRepo>();
            For<IIdexClient>().Use<IdexClient>();
            For<IIdexIntegration>().Use<IdexIntegration>();

            For<IKucoinClient>().Use<KucoinClient>();
            For<IKucoinIntegration>().Use<KucoinIntegration>();

            For<IYobitClient>().Use<YobitClient>();
            For<IYobitIntegration>().Use<YobitIntegration>();
            For<ITidexIntegration>().Use<TidexIntegration>();
            For<IKrakenIntegration>().Use<KrakenIntegration>();

            For<ILivecoinClient>().Use<LivecoinClient>();
            For<ILivecoinIntegration>().Use<LivecoinIntegration>();
    
            For<IEtherscanHoldingRepo>().Use<EtherscanHoldingRepo>();
            For<IEtherscanHistoryRepo>().Use<EtherscanHistoryRepo>();
            For<IMewIntegration>().Use<MewIntegration>();

            For<IQryptosClient>().Use<QryptosClient>();
            For<IQryptosIntegration>().Use<QryptosIntegration>();

            For<IOexClient>().Use<OexClient>();
            For<IOexExchange>().Use<OexExchange>();

            For<IGeminiClient>().Use<GeminiClient>();
            For<IGeminiExchange>().Use<GeminiExchange>();

            For<IExchangeWorkflow>().Use<ExchangeWorkflow>();

            For<IEnvironmentConfigRepo>().Use<EnvironmentConfigRepo>();
            For<IRabbitConnectionFactory>().Use<RabbitConnectionFactory>();
            For<IExchangeServiceApp>().Use<ExchangeServiceApp>();           
            For<IExchangeHandler>().Use<ExchangeHandler>();            

            For<IExchangeClient>().Use<ExchangeClient>();
            For<IServiceInvoker>().Use<ServiceInvoker>();
            For<ICryptoCompareIntegration>().Use<CryptoCompareIntegration>();

            For<ICacheUtil>().Use<CacheUtil>();

            For<ICoinbaseIntegration>().Use<CoinbaseIntegration>();
        }
    }
}
