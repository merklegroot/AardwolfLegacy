angular.module('main')
    .controller('holdingsController', function (
        $scope,
        $http,
        $uibModal,
        balanceService,
        dataService,
        alertService) {

        $scope.model = {
            exchanges: {},
            valuation: {}
        };

        $scope.getTotalValue = function () {
            if (!$scope.model.exchanges || !$scope.model.exchanges.data || !$scope.model.exchanges.data.length) { return 0; }
            var total = 0;
            for (var i = 0; i < $scope.model.exchanges.data.length; i++) {
                var exchange = $scope.model.exchanges.data[i];
                if (exchange && exchange.holdings && exchange.holdings.totalValue) {
                    total += exchange.holdings.totalValue;
                }
            }

            return total;
        };

        var loadExchange = function (exchange) {
            if (!exchange.holdings) { exchange.holdings = {}; }

            var retriever = function () { return balanceService.getBalanceForExchange(exchange.id); };
            dataService.loadData(exchange.holdings, retriever, function () { updateValuationForExchange(exchange); });
        };

        var onGotExchanges = function (exchanges) {
            for (var i = 0; i < exchanges.data.length; i++) {
                loadExchange(exchanges.data[i]);
            }
        };

        var loadExchanges = function () {
            dataService.loadData($scope.model.exchanges, function () { return $http.post('api/get-holding-exchanges'); }, onGotExchanges);
        };

        var updateValuation = function () {
            if (!$scope.model.valuation.data || !$scope.model.exchanges.data) {
                return;
            }

            for (var exchangeIndex = 0; exchangeIndex < $scope.model.exchanges.data.length; exchangeIndex++) {
                var exchange = $scope.model.exchanges.data[exchangeIndex];

                updateValuationForExchange(exchange);
            }
        };

        var updateValuationForExchange = function (exchange) {
            if (!$scope.model.valuation.data) {
                return;
            }

            var totalValue = 0;
            for (var i = 0; i < exchange.holdings.data.holdings.length; i++) {
                var holding = exchange.holdings.data.holdings[i];
                var key = holding.asset.toLowerCase();
                var price = $scope.model.valuation.data[key];
                if (price) {
                    var itemValue = price * holding.total;
                    holding.value = itemValue;
                    totalValue += itemValue;
                }
            }

            exchange.holdings.totalValue = totalValue;

            exchange.holdings.data.holdings = _.orderBy(exchange.holdings.data.holdings, function (item) { return item.value ? item.value : 0; }, 'desc');
        };

        dataService.loadData($scope.model.valuation, function () { return $http.post('api/get-valuation-dictionary'); }, function (response) {
            $scope.model.valuationArray = [];
            var valuationKeys = Object.keys($scope.model.valuation.data);
            for (var i = 0; i < valuationKeys.length; i++) {
                var key = valuationKeys[i];
                var value = $scope.model.valuation.data[key];
                $scope.model.valuationArray.push({symbol: key, value: value});
            }
            updateValuation();
        });

        loadExchanges();

        $scope.sort = function (holdings) {
            var result = _.sortBy(holdings, function (item) {
                return (item.asset === "ETH" ? "A" : (item.asset === "BTC" ? "B" : "Z")) + "_" + item.asset;
            });

            return result;
        };

        $scope.formatTime = function (time) {
            return moment.utc(time).local().format("ddd, MMMM DD YYYY hh:mm:ss A");
        };

        $scope.getAge = function (time) {
            var diff = (moment.utc() - moment.utc(time));
            var seconds = parseInt(diff / 1000);
            var minutes = parseInt(seconds / 60);
            seconds -= 60 * minutes;
            var hours = parseInt(minutes / 60);
            minutes -= 60 * hours;
            var days = parseInt(hours / 24);
            hours -= 24 * days;

            var displayText = seconds + " seconds";
            if (minutes || hours || days) {
                displayText = minutes + " minutes, " + displayText;
            }

            if (hours || days) {
                displayText = hours + " hours, " + displayText;
            }

            if (days) {
                displayText = days + " days, " + displayText;
            }

            return displayText;
        };

        $scope.shouldShowAsset = function (holding, exchange) {
            if (holding.total <= 0) { return false; }

            if (exchange.showSmallAssets) { return true; }
            if (holding.asset === 'ETH' || holding.asset === 'BTC' || holding.asset === 'USD' || holding.asset === 'BNB') { return true; }

            if (holding.asset === 'VEN') { return holding.asset.total >= 1; }
            if (holding.asset === 'WISH') { return holding.asset.total >= 100; }
            if (holding.asset === 'BERRY') { return holding.asset.total >= 100; }
            if (holding.asset === 'CHP') { return holding.asset.total >= 100; }
            if (holding.asset === 'CRPT') { return holding.asset.total >= 100; }
            if (holding.asset === 'DADI') { return holding.asset.total >= 10; }
            if (holding.asset === 'DRG') { return holding.asset.total >= 10; }
            if (holding.asset === 'FOTA') { return holding.asset.total >= 100; }
            if (holding.asset === 'BANCA') { return holding.asset.total >= 1000; }

            var scamTokens = ['Cybersecurity', 'SYMM', 'HEALP', 'JULLAR.io', 'MCUX', 'SYMM', 'VIN'];
            if (_.some(scamTokens, function (item) { return holding.asset.toUpperCase() === item.toUpperCase(); })) { return false; }
            
            return holding.value === undefined || holding.value === null || holding.value > 1;
        }

        $scope.onSellAllMarketClicked = function (exchange, symbol, baseSymbol, quantity) {
            var data = { exchange: exchange.name, symbol: symbol, baseSymbol: baseSymbol, quantity: quantity };
            alertService.info(data);

            $uibModal.open({
                templateUrl: 'app/pages/holdings/dialog/holdings-sell-dialog.html?',
                controller: function ($scope, $uibModalInstance, data) {
                    $scope.data = {};
                    angular.copy(data, $scope.data);

                    $scope.ok = function () {
                        $uibModalInstance.close();
                        sellFunds($scope.data);
                    };

                    $scope.cancel = function () {
                        $uibModalInstance.dismiss('cancel');
                    };
                },
                resolve: {
                    data: function () { return data; }
                }
            });
        };

        var sellFunds = function (data) {
            alert('I am about to sell them.\r\n' + JSON.stringify(data));

            $http.post('api/sell-funds', data)
                .then(function (response) {
                    alert('Success!' + '\r\n' + JSON.stringify(response.data));
                }, function (err) {
                    alert('Error!' + '\r\n' + JSON.stringify(err));
                });
        };

        $scope.onTransferClicked = function (exchange, holding, dest) {
            var data = {
                source: exchange.name,
                destination: dest,
                symbol: holding.asset,
                quantity: holding.available
            };

            alertService.info(data);

            $uibModal.open({
                templateUrl: 'app/pages/holdings/dialog/holdings-transfer-dialog.html',
                controller: function ($scope, $uibModalInstance, data) {
                    $scope.data = {};
                    angular.copy(data, $scope.data);

                    $scope.ok = function () {
                        $uibModalInstance.close();
                        transferFunds($scope.data);
                    };

                    $scope.cancel = function () {
                        $uibModalInstance.dismiss('cancel');
                    };
                },
                resolve: {
                    data: function () { return data; }
                }
            });
        };

        var transferFunds = function (data) {
            // data.shouldTransferAll = true;
            alert(JSON.stringify(data));

            $http.post('api/transfer-funds', data)
                .then(function (response) {
                    alertService.success('Success!' + '\r\n' + JSON.stringify(response.data));
                }, function (err) {
                    alertService.error('Error!' + '\r\n' + JSON.stringify(err));
                });
        };

        var refreshExchange = function (exchange) {
            alertService.info("Refreshing...");

            var onSuccess = function (response) {
                alertService.success('Refreshed!');
                exchange.holdings.holdings = response.data.holdings;
            };
            var onError = function (err) { alertService.error(err); };

            var data = { id: exchange.id, forceRefresh: true };
            $http.post('api/get-holdings-for-exchange', data)
                .then(onSuccess, onError);
        };

        $scope.onRefreshExchangeClicked = function (exchange) {
            refreshExchange(exchange);
        };

        $scope.canTransferToCoss = function (asset) {
            if (!asset) { return false; }
            if (asset === 'BNB' || asset === 'GXS' || asset === 'KCS') { return false; }
            return true;
        };

        $scope.canTransferToBitz = function (asset) {
            return asset === 'GXS';
        }
});