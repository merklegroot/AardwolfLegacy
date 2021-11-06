angular.module('main')
    .controller('testController', function (
        $scope,
        $http,
        alertService,
        dataService,
        exchangeService,
        orderBookService) {
        
        $scope.model = {
            exchanges: {},
            orderBook: {}
        };

        var loadExchange = function (exchange) {
            exchange.tradingPairs = {};
            dataService.loadData(exchange.tradingPairs, function () {
                return exchangeService.getTradingPairsForExchange(exchange.name, false);
            });
        };

        dataService.loadData($scope.model.exchanges, function () {
            return exchangeService.getExchanges();
        }, function () {
            for (var i = 0; i < $scope.model.exchanges.data.length; i++) {
                var exchange = $scope.model.exchanges.data[i];
                loadExchange(exchange);
            }
        });

        dataService.loadData($scope.model.orderBook, function () {
            return orderBookService.getOrderBook('wtc', 'eth', 'coss', false);
        });

    });