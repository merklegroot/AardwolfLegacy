angular.module('main')
    .controller('ethController', function ($scope, $http,
        alertService,
        valuationService,
        dataService) {

        $scope.model = {
            eth: {},
            btc: {},
            ethWalletAddress: {}
        };

        $scope.onClipboardSuccess = function (e) {
            alertService.success('Address was copied to the clipboard!');
        };

        $scope.onClipboardError = function (e) {
            alertService.error('Failed to copy address to the clipboard.');
        };

        $scope.onRefreshBtcClicked = function () {
            alertService.info('onRefreshBtcClicked');
            loadSymbol('btc', true);
        }

        $scope.onRefreshEthClicked = function () {
            alertService.info('onRefreshEthClicked');
            loadSymbol('eth', true);
        }

        var loadSymbol = function (symbol, forceRefresh) {
            dataService.loadData($scope.model[symbol.trim().toLowerCase()],
                function () { return valuationService.getUsdValue(symbol, forceRefresh); });
        }

        var loadEthWalletAddress = function () {
            dataService.loadData($scope.model.ethWalletAddress, function () { return $http.post('api/eth-wallet'); });
        };

        var init = function () {
            loadSymbol('eth');
            loadSymbol('btc');
            loadEthWalletAddress();
        };

        init();
    });

