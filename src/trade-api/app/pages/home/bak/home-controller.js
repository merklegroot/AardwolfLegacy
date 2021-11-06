(function () {
    var controller = function (
        $scope,
        $http,
        $timeout,
        alertService,
        dataService,
        exchangeService) {

        $scope.model = {
            exchanges: {}
        };

        $scope.isRefreshingAll = false;
        $scope.isRefreshingCoin = [];

        var indexToRefresh = 0;
        var isBackgroundRefreshRunning = false;

        var loadOrders = function (forceRefresh) {
            $scope.isRefreshingAll = true;

            alertService.info('Loading a ridiculous amount of data here...\r\nThis is gonna take awhile.', 'Hold on to your hats!');
            var serviceModel = { forceRefresh: forceRefresh, filteredOutExchanges: getDisabledExchanges() };
            $http.post('api/get-all-orders', serviceModel)
                .then(function (response) {
                    alertService.success('Crescent fresh!');

                    $scope.isRefreshingAll = false;
                    $scope.orders = response.data;
                    for (var i = 0; i < $scope.orders.coins.length; i++) {
                        $scope.orders.coins[i].formattedTime =
                            $scope.orders.coins[i].timeStampUtc
                                ? moment($scope.orders.coins[i].timeStampUtc).format("YYYY-MM-DD HH:mm:ss")
                                : null;
                    }

                    startBackgroundRefresh();
                }, function (err) {
                    $scope.isRefreshingAll = false;
                    alertService.error('Error: ' + JSON.stringify(err));
                });
        };

        var copySharedPropertyValues = function (source, dest) {
            for (var prop in source) {
                if (source.hasOwnProperty(prop) && dest.hasOwnProperty(prop)) {
                    dest[prop] = source[prop];
                }
            }
        };

        var getDisabledExchanges = function () {
            var disabledExchanges = [];
            if ($scope.model.exchanges && $scope.model.exchanges.data) {
                for (var i = 0; i < $scope.model.exchanges.data.length; i++) {
                    var key = $scope.model.exchanges.data[i].name
                    if ($scope.model.isExchangeDisabled[key]) {
                        disabledExchanges.push(key);
                    }
                }
            }

            return disabledExchanges;
        };

        $scope.refresh = function (coin, onDone) {
            var serviceModel = {
                symbol: coin.symbol,
                baseSymbol: coin.baseSymbol,
                exchangeA: coin.exchanges[0].name,
                exchangeB: coin.exchanges[1].name
            };

            coin.isRefreshing = true;

            alertService.info('Refreshing ' + '\r\n' + JSON.stringify(serviceModel));
            var onSuccess = function (response) {
                try {
                    coin.isRefreshing = false;
                    if (response === null || response.data === null) { return; }
                    copySharedPropertyValues(response.data, coin);

                    alertService.success('Crescent fresh!');
                }
                catch (ex) {
                    alertService.error(ex);
                }

                if (onDone) { onDone(); }
            };

            var onFailure = function (err) {
                coin.isRefreshing = false;
                alertService.error(JSON.stringify(err));

                if (onDone) { onDone(); }
            };

            $http.post('api/get-orders', serviceModel).then(onSuccess, onFailure);
        };

        $scope.loadClicked = function () {
            loadOrders(false);
        };

        $scope.loadRefreshAllClicked = function () {
            loadOrders(true);
        };

        $scope.getFilteredCoins = function () {
            if (!$scope.orders || !$scope.orders.coins) {
                return [];
            }

            return _.filter($scope.orders.coins, function (item) {
                return $scope.isExchangeEnabled(item.exchanges[0].name) && $scope.isExchangeEnabled(item.exchanges[1].name);
            });
        };

        $scope.model.isExchangeDisabled = {};
        $scope.onToggleExchangeClicked = function (exchangeName) {
            $scope.model.isExchangeDisabled[exchangeName] = !$scope.model.isExchangeDisabled[exchangeName];
        };

        $scope.isExchangeEnabled = function (exchangeName) {
            return !$scope.model.isExchangeDisabled[exchangeName];
        };

        dataService.loadData($scope.model.exchanges, exchangeService.getExchanges);
    };

    angular.module('main').controller('homeController', controller);
})();