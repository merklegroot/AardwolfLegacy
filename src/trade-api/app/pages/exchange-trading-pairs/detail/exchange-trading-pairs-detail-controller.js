angular.module('main')
    .controller("exchangeTradingPairsDetailController", function (
        $scope, $stateParams, $timeout,
        exchangeDictionary,
        dataService,
        alertService,
        orderBookService,
        balanceService,
        valuationService,
        commodityService,
        exchangeService) {

        var exchange = $stateParams.id;

        $scope.model = {
            exchangeDisplayName: exchangeDictionary[exchange].displayName,
            exchange: exchange,
            tradingPairs: {},
            filteredTradingPairs: {}
        };

        dataService.loadData($scope.model.tradingPairs, function () {
            return exchangeService.getTradingPairsForExchange(exchange, false, 'OnlyUseCacheUnlessEmpty');
        }, function () {
            updateFilteredTradingPairs();
        });

        $scope.onFilterTextChanged = function () {
            updateFilteredTradingPairs();
        };

        $scope.onRefreshClicked = function () {
            alertService.info("Refreshing...");
            dataService.loadData($scope.model.tradingPairs, function () {
                return exchangeService.getTradingPairsForExchange(exchange, true);
            }, function () {
                updateFilteredTradingPairs();
                alertService.info("Refreshed!");
            });
        };

        var updateFilteredTradingPairs = function () {
            var filterText = $scope.filterText;
            var tradingPairs = $scope.model.tradingPairs;

            if (!tradingPairs) {
                $scope.model.filteredTradingPairs = [];
                return;
            }

            if (filterText === undefined || filterText === null) {
                $scope.model.filteredTradingPairs = tradingPairs.data;
                return;
            }

            filterText = filterText.trim().toUpperCase();
            if (filterText.length === 0) {
                $scope.model.filteredTradingPairs = tradingPairs.data;
                return;
            }

            $scope.model.filteredTradingPairs = _.filter(tradingPairs.data, function (item) {
                if (item.symbol !== undefined && item.symbol !== null && item.symbol.indexOf(filterText) >= 0) {
                    return true;
                }

                if (item.baseSymbol !== undefined && item.baseSymbol !== null && item.baseSymbol.indexOf(filterText) >= 0) {
                    return true;
                }

                return false;
            });
        };
    });
