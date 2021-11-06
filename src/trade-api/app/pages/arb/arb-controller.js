
(function () {
    var controller = function (
        $scope,
        $http,
        $stateParams,
        $timeout,
        exchangeDictionary,
        dataService,
        alertService,
        exchangeService,
        orderBookService) {

        $scope.model = {
            exchanges: {},
            arbs: [],
            profitableIndexes: [],
            unprofitableIndexes: []
        };

        var cossHitBtcSymbols = [
            "FYN", "PIX", "COSS"
        ];

        var binanceCossSymbols = [
            "SNM", "OMG", "LINK",
            "BLZ", "SUB", "ENJ",
            "KNC", "WTC", "REQ",
            "CVC", "BLZ", "BCH",
            "NEO", "BNT"];

        var kucoinCossSymbols = [
            "DAT", "GAT", "LALA",
            "LA", "OMG", "CVC",
            "CS", "BCH", "CAN",
            "PRL", "WTC", "NEO"
        ];

        var qryptosHitBytcSymbols = ["STU"];
        var qryptosCossSymbols = ["VZT", "IND"];

        var qryptosKucoinSymbols = [
            "DENT", "UKG", "ETC"
        ];

        var qryptosBinanceSymbols = [
            "DENT"
        ];

        var arbDefs = [];

        var setupArbDefs = function () {
            var addPair = function (exchanges, symbol) {
                arbDefs.push({
                    exchanges: [exchanges[0], exchanges[1]],
                    symbol: symbol
                });

                arbDefs.push({
                    exchanges: [exchanges[1], exchanges[0]],
                    symbol: symbol
                });
            };

            var addBinanceCossArbDef = function (symbol) {
                addPair(["binance", "coss"], symbol);                
            };

            var addKucoinCossArbDef = function (symbol) {
                addPair(["kucoin", "coss"], symbol);
            };

            var addQryptosCossArbDef = function (symbol) {
                addPair(["qryptos", "coss"], symbol);
            };

            var addQryptosHitBtcArbDef = function (symbol) {
                addPair(["qryptos", "hitbtc"], symbol);
            };

            var addQryptosKucoinArbDef = function (symbol) {
                addPair(["qryptos", "kucoin"], symbol);
            };

            var addCossHitBtcArbDef = function (symbol) {
                addPair(["coss", "hitbtc"], symbol);
            };

            var addQryptosBinanceArbDef = function (symbol) {
                addPair(["qryptos", "binance"], symbol);
            };

            var exchangeA = $stateParams.exchangeA;
            var exchangeB = $stateParams.exchangeB;
            var symbol = $stateParams.symbol;
            if (exchangeA && exchangeB && symbol) {
                // addPair([exchangeA, exchangeB], symbol);

                arbDefs.push({
                    exchanges: [exchangeA, exchangeB],
                    symbol: symbol
                });
            } else {
                var i;
                for (i = 0; i < binanceCossSymbols.length; i++) {
                    addBinanceCossArbDef(binanceCossSymbols[i]);
                }

                for (i = 0; i < kucoinCossSymbols.length; i++) {
                    addKucoinCossArbDef(kucoinCossSymbols[i]);
                }

                for (i = 0; i < qryptosCossSymbols.length; i++) {
                    addQryptosCossArbDef(qryptosCossSymbols[i]);
                }

                for (i = 0; i < qryptosHitBytcSymbols.length; i++) {
                    addQryptosHitBtcArbDef(qryptosHitBytcSymbols[i]);
                }

                for (i = 0; i < qryptosKucoinSymbols.length; i++) {
                    addQryptosKucoinArbDef(qryptosKucoinSymbols[i]);
                }

                for (i = 0; i < cossHitBtcSymbols.length; i++) {
                    addCossHitBtcArbDef(cossHitBtcSymbols[i]);
                }

                for (i = 0; i < qryptosBinanceSymbols.length; i++) {
                    addQryptosBinanceArbDef(qryptosBinanceSymbols[i]);
                }
            }

            $scope.model.arbs = _.map(arbDefs, function (queryArbDef) {
                return {
                    exchanges: _.map(queryArbDef.exchanges, function (queryExchange) {
                        return { id: queryExchange, displayName: exchangeDictionary[queryExchange].displayName }
                        }),
                    symbol: queryArbDef.symbol.toUpperCase()
                }
            });
        };

        var loadArb = function (arb) {
            var serviceModel = { exchangeA: arb.exchanges[0].id, exchangeB: arb.exchanges[1].id, symbol: arb.symbol, forceRefresh: false };
            var retriever = function () { return $http.post('api/get-arb', serviceModel); }
            
            dataService.loadData(arb, retriever);
        }

        var loadExchanges = function () {
            var retriever = function () { return exchangeService.getExchanges(); };
            dataService.loadData($scope.model.exchanges, retriever);
        }

        var loadArbByIndex = function (arbIndex, cachePolicy, onAllCompleted) {
            var arb = $scope.model.arbs[arbIndex];
            var serviceModel = { exchangeA: arb.exchanges[0].id, exchangeB: arb.exchanges[1].id, symbol: arb.symbol, forceRefresh: false };
            var retriever = function () { return $http.post('api/get-arb', serviceModel); }

            var onCompleted = function () {
                updateProfitableIndexes();
                if (arbIndex + 1 < $scope.model.arbs.length) {
                    loadArbByIndex(arbIndex + 1, cachePolicy, onAllCompleted);
                } else {
                    if (onAllCompleted) {
                        onAllCompleted();
                    }
                }
            };

            dataService.loadData(arb, retriever, onCompleted, onCompleted);
        }

        var updateProfitableIndexes = function () {
            var profitableIndexes = [];
            var unprofitableIndexes = [];
            for (var i = 0; i < $scope.model.arbs.length; i++) {
                var arb = $scope.model.arbs[i];
                if (arb.data && arb.data.expectedUsdProfit > 0) {
                    profitableIndexes.push(i);
                } else {
                    unprofitableIndexes.push(i);
                }
            }

            $scope.model.profitableIndexes = profitableIndexes;
            $scope.model.unprofitableIndexes = unprofitableIndexes;
        };

        var doneWithInitialArbLoad = function () {
            alertService.info('All clear, sir!');
            loadArbByIndex(0, 'allowCache', doneWithInitialArbLoad);
        };

        var loadArbs = function () {
            setupArbDefs();
            if ($scope.model.arbs.length === 1) {
                var onComplete = function () {
                    // loadArbByIndex(0, 'allowCache');
                };
                loadArbByIndex(0, 'onlyUseCacheUnlessEmpty', onComplete);
            } else {
                loadArbByIndex(0, 'onlyUseCacheUnlessEmpty', doneWithInitialArbLoad);
            }
        };

        var init = function () {
            loadExchanges();
            loadArbs();
        };

        init();
    };

    angular.module('main').controller('arbController', controller);
})();