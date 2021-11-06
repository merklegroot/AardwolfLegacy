angular.module('main')
    .controller("exchangeDetailController", function (
        $stateParams, $scope, $http, $uibModal, $timeout,
        dataService,
        alertService,
        openOrderService,
        balanceService,
        exchangeDictionary) {

        var onOpenOrdersLoaded = function (forceRefresh) {
            var iterate = function (i) {

                if (!$scope.model || !$scope.model.openOrders || !$scope.model.openOrders.data) {
                    return;
                }

                if (i >= $scope.model.openOrders.data.length) { return;}

                var openOrder = $scope.model.openOrders.data[i];
                if (openOrder === null) { return; }
                var getOrderBookServiceModel = {
                    symbol: openOrder.symbol,
                    baseSymbol: openOrder.baseSymbol,
                    exchange: integrationId,
                    forceRefresh: forceRefresh !== undefined && forceRefresh !== null
                        ? forceRefresh
                        : false
                };

                var onLoaded = function () {
                    if (i + 1 < $scope.model.openOrders.data.length) {
                        $timeout(function () { iterate(i + 1); }, 1);
                    }
                };

                var onSuccess = function (response) {
                    try {
                        openOrder.orderBook = response.data;
                        openOrder.bestBidPrice = response.data.bids[0].price;
                        openOrder.bestAskPrice = response.data.asks[0].price;
                        if (openOrder.orderTypeText.trim().toUpperCase() === 'Bid'.toUpperCase()) {
                            openOrder.competingPrice = openOrder.bestBidPrice;
                            if (openOrder.bestBidPrice) {
                                openOrder.isWinning = openOrder.price >= openOrder.bestBidPrice;
                            }
                        } else if (openOrder.orderTypeText.trim().toUpperCase() === 'Ask'.toUpperCase()) {
                            openOrder.competingPrice = openOrder.bestAskPrice;
                            if (openOrder.bestAskPrice) {
                                openOrder.isWinning = openOrder.price <= openOrder.bestAskPrice;
                            }
                        } else {
                            openOrder.competingPrice = null;
                        }
                    }
                    catch (ex) {
                        console.log(ex);
                    }

                    onLoaded();
                };

                var onError = function (err) {
                    alertService.onError(err);

                    onLoaded();
                };

                openOrder.bestBidPrice = 'Loading...';
                openOrder.bestAskPrice = 'Loading...';

                $http.post('api/get-order-book', getOrderBookServiceModel)
                    .then(onSuccess, onError);
            };

            $timeout(function () { iterate(0); }, 1);
        };

        var loadOpenOrders = function (forceRefresh) {
            var retriever = function () {
                return openOrderService.getOpenOrders(integrationId);
            };

            var onLoaded = function () {
                onOpenOrdersLoaded(forceRefresh);
            };

            dataService.loadData($scope.model.openOrders, retriever, onLoaded);
        };

        var updateValuation = function () {
            if (!$scope.model.valuation.data || !$scope.model.holdings.data || !$scope.model.holdings.data.holdings) {
                return;
            }

            var totalValue = 0;
            for (var i = 0; i < $scope.model.holdings.data.holdings.length; i++) {
                var holding = $scope.model.holdings.data.holdings[i];
                var key = holding.asset.toLowerCase();
                var price = $scope.model.valuation.data[key];
                if (price) {
                    var itemValue = price * holding.total;
                    holding.value = itemValue;
                    totalValue += itemValue;
                }
            }

            $scope.model.totalValue = totalValue;

            $scope.model.holdings.data.holdings = _.orderBy($scope.model.holdings.data.holdings, function (item) { return item.value ? item.value : 0; }, 'desc');
        };

        var integrationId = $stateParams.id;
        $scope.exchangeId = $stateParams.id;
        $scope.integrationDisplayName = exchangeDictionary[integrationId].displayName;

        $scope.model = {};
        $scope.model.commodities = {};
        $scope.model.holdings = {};
        $scope.model.valuation = {};
        $scope.model.openOrders = {};
        $scope.model.totalValue = null;

        var retriever = function () { return balanceService.getBalanceForExchange(integrationId); };
        dataService.loadData($scope.model.holdings, retriever, updateValuation);

        dataService.loadData($scope.model.valuation, function () { return $http.post('api/get-valuation-dictionary'); }, function (response) {
            updateValuation();
        });

        if (integrationId === 'coss' || integrationId === 'idex' || integrationId === 'bit-z') {
            loadOpenOrders();
        }

        $scope.onRefreshOpenOrdersClicked = function () {
            alertService.info("Refreshing...");
            loadOpenOrders(true);
        };

        $scope.onRefreshHoldingsClicked = function () {
            alertService.info("Refreshing...");
            var retriever = function () { return balanceService.getBalanceForExchange(integrationId, 'ForceRefresh'); };
            dataService.loadData($scope.model.holdings, retriever, updateValuation);
        };

        $scope.shouldShowHolding = function (item) {
            if (!item || !item.asset) { return false; }
            if (item.asset.toUpperCase() === 'ETH' || item.asset.toUpperCase() === 'BTC') { return true; }

            if ($scope.showSmallAssets) { return true; }

            if (item.total <= 0) { return false; }
            if (item.value === undefined || item.value === null) { return true; }

            return item.value > 1;
        };

        $scope.onRefreshDepositAddressesClicked = function () {
            var onSuccess = function (response) { alertService.info(response.data); };
            var onError = function (err) { alertService.onError(err); };

            $http.post('api/refresh-deposit-addresses', { name: integrationId }).then(onSuccess, onError);
        };      

        $scope.onTransferClicked = function (holding) {

            var destinations = [
                { id: 'binance', name: 'Binance' },
                { id: 'kucoin', name: 'KuCoin' },
                { id: 'livecoin', name: 'Livecoin' },
                { id: 'bitz', 'name': 'Bit-Z' },
                { id: 'coss', name: 'Coss' },
                { id: 'mew', name: 'Mew' },
                { id: 'hitbtc', 'name': 'HitBTC' },
                { id: 'qryptos', 'name': 'Liquid' },
                { id: 'cryptopia', 'name': 'Cryptopia' },
                { id: 'yobit', 'name': 'YoBit' }
            ];

            var availableDestinations = _.filter(destinations, function (item) { return item.id !== integrationId; });
            var suggestedDestination = _.cloneDeep(availableDestinations[0]);

            var data = {
                source: { id: integrationId, name: $scope.integrationDisplayName },
                destination: suggestedDestination,
                symbol: holding.asset,
                available: holding.available,
                quantityToSend: holding.available,
                availableDestinations: availableDestinations
            };

            alertService.info(data);
            var dialog = $uibModal.open({
                templateUrl: 'app/pages/exchange/dialog/transfer-funds-dialog.html',
                controller: 'transferFundsDialogController',
                resolve: { data: function () { return data; } }
            });

            dialog.result.then(function (result) {
                console.log(result);

                if (!(result && result.wasConfirmed)) { console.log('nope'); return; }

                if (!confirm('Are you sure you want to transfer these funds?\r\r' +
                    'Symbol: ' + result.data.symbol + '\r' + 
                    'Quantity: ' + result.data.quantity + '\r' + 
                    'Source: ' + '(' + result.data.source.id + ')' + result.data.source.name + '\r' + 
                    'Destination: ' + '(' + result.data.destination.id + ')' + result.data.destination.name + '\r\r' + 
                    JSON.stringify(result))) {
                    alertService.info('NOT sending.');
                }

                alertService.info('Sending');                

                var serviceModel = {
                    symbol: result.data.symbol,
                    quantity: result.data.quantity,
                    source: result.data.source.id,
                    destination: result.data.destination.id,
                    nonce: new Date().getUTCMilliseconds()
                };

                $http.post('api/transfer-funds', serviceModel)
                    .then(function (response) {
                        alertService.success('Success!' + '\r\n' + JSON.stringify(response.data));
                    }, function (err) {
                        alertService.error('Error!' + '\r\n' + JSON.stringify(err));
                    });
            });
        };

        var transferFunds = function (item, destination) {
            var serviceModel = {
                symbol: item.asset,
                quantity: item.available,
                sourceId: integrationId,
                sourceDisplayName: integrationDisplayName,
                destination: destination
            };

            var onSuccess = function (response) {
                alertService.onSuccess(JSON.stringify(response.data));
            };
            var onError = function (err) { alertService.onError(err); };

            alertService.info('Sending!\r\n' + JSON.stringify(serviceModel));
            $http.post('api/transfer-funds', serviceModel).then(onSuccess, onError);
        };
});