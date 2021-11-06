angular.module('main')
    .controller('exchangeCommodityListDirectiveController', function ($scope, $http, $timeout, dataService, alertService) {
        $scope.model = {};
        $scope.model.commodities = {};

        $scope.onClipboardSuccess = function (e) {
            alertService.success('Address was copied to the clipboard!');
        };

        $scope.onClipboardError = function (e) {
            alertService.error('Failed to copy address to the clipboard.');
        };

        $scope.onFilterChanged = function () {
            refreshFilteredData();
        };

        $scope.clearFilter = function () {
            $scope.filter = '';
            refreshFilteredData();
        };

        $scope.isLimitEnabled = true;

        $scope.getLimit = function () {
            if ($scope.isLimitEnabled) { return 5; }
            return $scope.filtered.length;
        };

        $scope.toggleLimit = function () {
            $scope.isLimitEnabled = !$scope.isLimitEnabled;
        };

        $scope.onGetBalanceClicked = function (commodity) {
            var serviceModel = {
                exchange: $scope.exchange,
                symbol: commodity.symbol
            };

            var onSuccess = function (response) {
                console.log(response.data);
                alertService.info(response.data);
            };

            var onError = function (err) { alertService.onError(err); };

            $http.post('api/get-token-balance', serviceModel)
            .then(onSuccess, onError);
        };

        var refreshFilteredData = function () {
            var upperFilter = $scope.filter !== undefined && $scope.filter !== null
                ? $scope.filter.toUpperCase()
                : '';

            var filtered = [];
            if ($scope.model.commodities.data !== undefined && $scope.model.commodities.data !== null) {
                for (var i = 0; i < $scope.model.commodities.data.length; i++) {
                    var item = $scope.model.commodities.data[i];

                    if (upperFilter === undefined || upperFilter === null || upperFilter.length === 0) {
                        filtered.push(item);
                    }
                    else {
                        var upperSymbol = item && item.symbol !== null ? item.symbol.toUpperCase() : "";
                        if (upperSymbol.indexOf(upperFilter) !== -1) {
                            filtered.push(item);
                        }
                    }
                }
            }

            $scope.filtered = _.orderBy(filtered, function (item) { return item.symbol; });
        };

        var getDepositAddress = function (index) {
            if (index >= $scope.model.commodities.data.length) { return; }

            var commodity = $scope.model.commodities.data[index];
            var onSuccess = function (response) {
                if (response.data) {
                    commodity.depositAddress = response.data.depositAddress;
                    commodity.depositMemo = response.data.depositMemo;
                }

                if (index + 1 < $scope.model.commodities.data.length) {
                    $timeout(function () { getDepositAddress(index + 1); }, 1);
                }
            };

            var onError = function (err) {
                // alertService.onError(err);

                if (index + 1 < $scope.model.commodities.data.length) {
                    $timeout(function () { getDepositAddress(index + 1); }, 1);
                }
            };

            $http.post('api/get-deposit-address', { exchange: $scope.exchange, symbol: commodity.symbol }).then(onSuccess, onError);
        };

        var init = function () {
            dataService.loadData($scope.model.commodities, function () { return $http.post('api/get-commodities-for-exchange', { exchange: $scope.exchange }); }, function () {
                refreshFilteredData();

                if ($scope.model.commodities.data.length > 0) {
                    getDepositAddress(0);
                }
            });


            refreshFilteredData();
        };

        init();    
});

