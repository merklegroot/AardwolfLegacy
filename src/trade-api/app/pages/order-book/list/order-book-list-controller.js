angular.module('main')
    .controller('orderBookListController', function (
        $scope, $http,
        orderBookService,
        dataService, alertService) {

        var exchanges = {
            'binance': { displayName: 'Binance' },
            'coss': { displayName: 'Coss' },
        };

        $scope.orderBooks = [
            { exchange: 'binance', symbol: 'ARK', baseSymbol: 'ETH' },
            { exchange: 'coss', symbol: 'ARK', baseSymbol: 'ETH' },
            { exchange: 'binance', symbol: 'ETH', baseSymbol: 'BTC' },
            { exchange: 'coss', symbol: 'ETH', baseSymbol: 'BTC' },
        ];

        for (var i = 0; i < $scope.orderBooks.length; i++) {
            var orderBook = $scope.orderBooks[i];
            orderBook.exchangeDisplayName = exchanges[orderBook.exchange].displayName;
        }

    });