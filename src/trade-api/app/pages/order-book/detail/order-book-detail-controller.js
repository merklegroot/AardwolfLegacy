angular.module('main')
    .controller('orderBookDetailController', function (
        $stateParams,
        $scope,
        $http,
        exchangeDictionary,
        orderBookService,
        dataService,
        alertService) {

        var exchangeId = $stateParams.exchange.toLowerCase();
        var symbol = $stateParams.symbol;
        var baseSymbol = $stateParams.baseSymbol;

        $scope.model = {
            orderBook: {},
            exchangeDisplayName: exchangeDictionary[exchangeId].displayName,
            exchange: exchangeId,
            symbol: symbol,
            baseSymbol: baseSymbol
        };

        var loadData = function (forceRefresh) {
            alertService.info('Loading...');
            dataService.loadData($scope.model.orderBook, function () {
                return orderBookService.getOrderBook(symbol, baseSymbol, exchangeId, forceRefresh);
            }, function () {
                alertService.info('Crescent fresh!');
            });
        };

        $scope.onRefreshClicked = function () {
            loadData(true);
        };

        loadData(false);

    });