//using binance_lib;
//using bit_z_lib;
//using coss_lib;
//using cryptopia_lib;
//using hitbtc_lib;
//using idex_integration_lib;
//using kraken_integration_lib;
//using kucoin_lib;
//using livecoin_lib;
//using mew_integration_lib;
//using qryptos_lib;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using tidex_integration_library;
//using trade_lib;
//using yobit_lib;

//namespace integration_workflow_lib
//{
//    public class ExchangeRetriever : IExchangeRetriever
//    {
//        private readonly ICossIntegration _cossIntegration;
//        private readonly IBinanceIntegration _binanceIntegration;
//        private readonly IHitBtcIntegration _hitBtcIntegration;
//        private readonly IKrakenIntegration _krakenIntegration;
//        private readonly IKucoinIntegration _kucoinIntegration;
//        private readonly IBitzIntegration _bitzIntegration;
//        private readonly ILivecoinIntegration _livecoinIntegration;
//        private readonly IMewIntegration _mewIntegration;
//        private readonly IQryptosIntegration _qryptosIntegration;
//        private readonly ITidexIntegration _tidexIntegration;

//        private readonly ICryptopiaIntegration _cryptopiaIntegration;
//        private readonly IIdexIntegration _idexIntegration;
//        private readonly IYobitIntegration _yobitIntegration;

//        private readonly List<ITradeIntegration> _exchanges;

//        public ExchangeRetriever(ICossIntegration cossIntegration,
//            IBinanceIntegration binanceIntegration,
//            IBitzIntegration bitzIntegration,
//            IKucoinIntegration kucoinIntegration,
//            IHitBtcIntegration hitBtcIntegration,
//            ILivecoinIntegration livecoinIntegration,
//            IKrakenIntegration krakenIntegration,
//            IMewIntegration mewIntegration,
//            IIdexIntegration idexIntegration,
//            ICryptopiaIntegration cryptopiaIntegration,
//            IYobitIntegration yobitIntegration,
//            IQryptosIntegration qryptosIntegration,
//            ITidexIntegration tidexIntegration)
//        {
//            _cossIntegration = cossIntegration;
//            _binanceIntegration = binanceIntegration;
//            _hitBtcIntegration = hitBtcIntegration;
//            _krakenIntegration = krakenIntegration;
//            _kucoinIntegration = kucoinIntegration;
//            _bitzIntegration = bitzIntegration;
//            _livecoinIntegration = livecoinIntegration;
//            _cryptopiaIntegration = cryptopiaIntegration;
//            _idexIntegration = idexIntegration;
//            _mewIntegration = mewIntegration;
//            _yobitIntegration = yobitIntegration;
//            _qryptosIntegration = qryptosIntegration;
//            _cryptopiaIntegration = cryptopiaIntegration;
//            _tidexIntegration = tidexIntegration;

//            _exchanges = new List<ITradeIntegration>
//            {
//                _cossIntegration,
//                _binanceIntegration,
//                _hitBtcIntegration,
//                _krakenIntegration,
//                _kucoinIntegration,
//                _bitzIntegration,
//                _livecoinIntegration,
//                _cryptopiaIntegration,
//                _idexIntegration,
//                _mewIntegration,
//                _yobitIntegration,
//                _qryptosIntegration,
//                _cryptopiaIntegration,
//                _tidexIntegration
//            };
//        }

//        public ITradeIntegration GetExchangeByName(string name)
//        {
//            if (string.IsNullOrWhiteSpace(name)) { return null; }

//            return _exchanges.Single(integration => string.Equals(name, integration.Name, StringComparison.InvariantCultureIgnoreCase));
//        }
//    }
//}