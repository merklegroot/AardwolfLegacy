using balance_lib;
using binance_lib;
using bit_z_lib;
using bitz_browser_lib;
using bitz_data_lib;
using browser_automation_client_lib;
using client_lib;
using coin_lib;
using coinbase_lib;
using config_client_lib;
using config_connection_string_lib;
using config_lib;
using coss_api_client_lib;
using coss_browser_service_client;
using coss_cookie_lib;
using coss_data_lib;
using coss_lib;
using cryptocompare_client_lib;
using cryptocompare_lib;
using cryptopia_lib;
using currency_converter_lib;
using env_config_lib;
using etherscan_lib;
using exchange_client_lib;
using hitbtc_lib;
using hitbtc_lib.Client;
using idex_client_lib;
using idex_data_lib;
using idex_integration_lib;
using integration_workflow_lib;
using kraken_integration_lib;
using kucoin_lib;
using kucoin_lib.Client;
using livecoin_lib;
using livecoin_lib.Client;
using log_lib;
using mew_integration_lib;
using qryptos_lib;
using qryptos_lib.Client;
using rabbit_lib;
using StructureMap;
using tfa_lib;
using tidex_integration_library;
using token_balance_lib;
using trade_email_lib;
using trade_lib;
using trade_lib.Repo;
using trade_node_integration;
using wait_for_it_lib;
using web_util;
using workflow_client_lib;
using yobit_lib;
using yobit_lib.Client;

namespace trade_ioc
{
    public class DefaultRegistry : Registry
    {
        public DefaultRegistry()
        {
            For<IWebUtil>().Use(() => new WebUtil());
            For<IWaitForIt>().Use(() => new WaitForIt());
            For<IConfigRepo>().Use(() => new ConfigRepo());
            For<ILogRepo>().Use(() => new LogRepo());

            For<ITradeNodeUtil>().Use<TradeNodeUtil>();
            For<IGetConnectionString>().Use<ConfigClient>();

            For<IEtherscanHoldingRepo>().Use<EtherscanHoldingRepo>();
            For<IEtherscanHistoryRepo>().Use<EtherscanHistoryRepo>();

            For<IIdexOrderBookRepo>().Use<IdexOrderBookRepo>();
            For<IIdexHoldingsRepo>().Use<IdexHoldingsRepo>();
            For<IIdexOpenOrdersRepo>().Use<IdexOpenOrdersRepo>();
            For<IIdexHistoryRepo>().Use<IdexHistoryRepo>();

            For<IHitBtcClient>().Use<HitBtcClient>();
            For<IHitBtcIntegration>().Use<HitBtcIntegration>();
            For<IBinanceIntegration>().Use<BinanceIntegration>();
            For<ICryptopiaIntegration>().Use<CryptopiaIntegration>();
            For<IBitzIntegration>().Use<BitzIntegration>();

            For<IOpenOrdersSnapshotRepo>().Use<OpenOrdersSnapshotRepo>();

            For<IPingClient>().Use<PingClient>();

            For<ICossApiClient>().Use<CossApiClient>();
            For<ICossIntegration>().Use<CossIntegration>();
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
            For<ICoinbaseIntegration>().Use<CoinbaseIntegration>();
            For<IMewIntegration>().Use<MewIntegration>();
            For<ICryptoCompareIntegration>().Use<CryptoCompareIntegration>();

            For<IQryptosClient>().Use<QryptosClient>();
            For<IQryptosIntegration>().Use<QryptosIntegration>();

            For<ICossHistoryRepo>().Use<CossHistoryRepo>();

            For<ITradeEmailUtil>().Use<TradeEmailUtil>();
            For<ICossOpenOrderRepo>().Use<CossOpenOrderRepo>();
            For<ICossXhrOpenOrderRepo>().Use<CossXhrOpenOrderRepo>();
            For<IDepositAddressValidator>().Use<DepositAddressValidator>();
            For<ITransferFundsWorkflow>().Use<TransferFundsWorkflow>();

            For<ITfaUtil>().Use<TfaUtil>();
            For<ICurrencyConverterClient>().Use<CurrencyConverterClient>();
            For<ICurrencyConverterIntegration>().Use<CurrencyConverterIntegration>();
            For<IValuationWorkflow>().Use<ValuationWorkflow>();
            For<IBitzFundsRepo>().Use<BitzFundsRepo>();
            For<IBitzBrowserUtil>().Use<BitzBrowserUtil>();

            For<IBinanceTwitterReader>().Use<BinanceTwitterReader>();

            For<IEnvironmentConfigRepo>().Use<EnvironmentConfigRepo>();
            For<IRabbitConnectionFactory>().Use<RabbitConnectionFactory>();
            For<IBalanceAggregator>().Use<BalanceAggregator>();
            For<ITokenBalanceIntegration>().Use<TokenBalanceIntegration>();

            For<IServiceInvoker>().Use<ServiceInvoker>();
            For<IRequestResponse>().Use<RequestResponse>();

            For<IConfigClient>().Use<ConfigClient>();
            For<IExchangeClient>().Use<ExchangeClient>();
            For<ICryptoCompareClient>().Use<CryptoCompareClient>();
            For<IWorkflowClient>().Use<WorkflowClient>();
            For<IBrowserAutomationClient>().Use<BrowserAutomationClient>();

            For<ICoinVmGenerator>().Use<CoinVmGenerator>();
            For<ICossBrowserClient>().Use<CossBrowserClient>();
            For<ICossCookieUtil>().Use<CossCookieUtil>();
        }
    }
}