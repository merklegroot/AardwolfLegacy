
angular.module('main')
    .controller('valuationController', function (
        $scope, $http, $timeout,
        dataService, alertService,
        valuationService) {

        $scope.model = {};
        $scope.model.valuation = {};
        $scope.model.items = [];

        var setItems = function () {
            var data = [];
            var keys = Object.keys($scope.model.valuation.data);
            for (var i = 0; i < keys.length; i++) {
                var key = keys[i];
                var value = $scope.model.valuation.data[key];

                data.push({ symbol: key, value: value });
            }

            $scope.model.items = data;
        };

        dataService.loadData($scope.model.valuation, function () { return $http.post('api/get-valuation-dictionary'); }, setItems);

        $scope.onRefreshClicked = function () {
            /*
            alertService.info("Refreshing EVERYTHING!");            

            $scope.model.items = null;
            var serviceModel = { forceRefresh: true };
            dataService.loadData($scope.model.valuation, function () { return $http.post('api/get-valuation-dictionary', serviceModel); }, setItems);
            */

            refreshItem(0);
        };

        var refreshItem = function (index) {
            var item = $scope.model.items[index];
            alertService.info("Refreshing " + item.symbol.toUpperCase() + " ( " + (index + 1) + " of " + $scope.model.items.length + ")");

            var onSuccess = function (response) {
                item.value = response.data;
                alertService.success('Refreshed ' + item.symbol.toUpperCase());

                if (index + 1 < $scope.model.items.length) {
                    $timeout(function () { refreshItem(index + 1); }, 10);
                }
            };

            var onError = function (err) {
                alertService.error(err);
            };

            valuationService.getUsdValue(item.symbol, true)
                .then(onSuccess, onError);       
        };

        $scope.onRefreshCommodityClicked = function (item) {
            alertService.info("Refreshing...");
            var onSuccess = function (response) {                
                item.value = response.data;
                alertService.success('Refreshed!');
            };

            var onError = function (err) {
                alertService.error(err);
            };

            valuationService.getUsdValue(item.symbol, true)
                .then(onSuccess, onError);            
        };
    });