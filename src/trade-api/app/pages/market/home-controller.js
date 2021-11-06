
angular.module('main')
    .controller('homeController', function ($scope, $http, dataService, alertService) {
        
        $scope.tradingPairs = {};

        var getOrderBook = function (serviceModel, index, inc) {
            $http.post('api/get-order-book', serviceModel)
                .then(function (response) {
                    var match = $scope.tradingPairs.data[index]; //_.find($scope.tradingPairs.data, function (item) { return item === serviceModel });
                    if (!match) { alertService.onError('Match not found.'); return; }

                    match.orderBookA = response.data.orderBookA;
                    match.orderBookB = response.data.orderBookB;
                    match.withdrawalFeeA = response.data.withdrawalFeeA;
                    match.withdrawalFeeB = response.data.withdrawalFeeB;

                    if (response.data.orderBookA.asks[0].price !== 0) {
                        match.profitA = 100.0 * (response.data.orderBookB.bids[0].price - response.data.orderBookA.asks[0].price) / response.data.orderBookA.asks[0].price;
                    }

                    if (response.data.orderBookB.asks[0].price !== 0) {
                        match.profitB = 100.0 * (response.data.orderBookA.bids[0].price - response.data.orderBookB.asks[0].price) / response.data.orderBookB.asks[0].price;
                    }

                    if (index + inc < $scope.tradingPairs.data.length) {
                        getOrderBook($scope.tradingPairs.data[index + inc], index + inc, inc);
                    }

                }, function (err) {
                    alertService.exception(err);
                    if (index + inc < $scope.tradingPairs.data.length) {
                        getOrderBook($scope.tradingPairs.data[index + inc], index + inc, inc);
                    }
                });
        }

        var onSuccess = function (response) {
            $scope.tradingPairs.isLoading = false;
            $scope.tradingPairs.data = response.data;

            var inc = 5;
            for (var i = 0; i < response.data.length && i < inc; i++) {
                getOrderBook(response.data[i], i, inc);
            }            
        };

        $http.post('api/get-trading-pairs')
            .then(onSuccess, function (err) {
                $scope.tradingPairs.isLoading = false;
                alertService.exception(err);
            });

    });