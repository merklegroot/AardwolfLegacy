angular.module('main')
    .controller('historyController', function (
        $scope, $http,
        dataService, alertService,
        commodityService) {

        $scope.pageIndex = 0;
        $scope.pageSize = 100;
        $scope.totalPages = 0;
        $scope.pages = [];

        $scope.isRefreshingBinance = false;
        $scope.history = {};
        $scope.commodities = {};
        $scope.commodityFilters = [
        ];

        $scope.effectiveCommodities = [
        ];

        $scope.onRefreshHistoryClicked = function (exchange) {
            var onSuccess = function (response) {
                alertService.info(response.data);
            };

            var onError = function (err) {
                alertService.onError(err);
            };

            $scope.isRefreshingBinance = true;
            $http.post('api/refresh-history', { exchange: exchange })
                .then(onSuccess, onError);
        };

        $scope.onPageClicked = function (pageIndex) {
            $scope.pageIndex = pageIndex;
        };

        $scope.onFirstPageClicked = function () {
            $scope.pageIndex = 0;
        };

        $scope.onPreviousPageClicked = function () {
            $scope.pageIndex = $scope.pageIndex > 0 ? $scope.pageIndex - 1 : 0;
        };

        $scope.onNextPageClicked = function () {
            if ($scope.pageIndex < $scope.totalPages - 1) {
                $scope.pageIndex++;
            }
        };

        $scope.onLastPageClicked = function () {
            $scope.pageIndex = $scope.totalPages - 1;
        };

        $scope.onToggleCossClicked = function () {
            $scope.hideCoss = !$scope.hideCoss;
            filterData();
        };

        $scope.onToggleLivecoinClicked = function () {
            $scope.hideLivecoin = !$scope.hideLivecoin;
            filterData();
        };

        $scope.onToggleBinanceClicked = function () {
            $scope.hideBinance = !$scope.hideBinance;
            filterData();
        };

        $scope.onToggleMewClicked = function () {
            $scope.hideMew = !$scope.hideMew;
            filterData();
        };
        
        $scope.onToggleBitzClicked = function () {
            $scope.hideBitz = !$scope.hideBitz;
            filterData();
        };

        $scope.onToggleKrakenClicked = function () {
            $scope.hideKraken = !$scope.hideKraken;
            filterData();
        };

        $scope.onToggleCoinbaseClicked = function () {
            $scope.hideCoinbase = !$scope.hideCoinbase;
            filterData();
        };
        
        $scope.onToggleIdexClicked = function () {
            $scope.hideIdex = !$scope.hideIdex;
            filterData();
        };
        
        $scope.onToggleBuyClicked = function () {
            $scope.hideBuy = !$scope.hideBuy;
            filterData();
        };

        $scope.onToggleSellClicked = function () {
            $scope.hideSell = !$scope.hideSell;
            filterData();
        };

        $scope.onToggleDepositClicked = function () {
            $scope.hideDeposit = !$scope.hideDeposit;
            filterData();
        };

        $scope.onToggleWithdrawClicked = function () {
            $scope.hideWithdraw = !$scope.hideWithdraw;
            filterData();
        };

        $scope.getData = function () {
            return $scope.history.data;
        };

        $scope.onCommodityFilterTagClicked = function (commodity) {
            if (!$scope.commodityFilters) { return; }

            for (var i = $scope.commodityFilters.length - 1; i >= 0; i--) {
                if ($scope.commodityFilters[i].symbol === commodity.symbol) {
                    $scope.commodityFilters.splice(i, 1);
                }
            }

            filterCommodities();
        };

        $scope.onCommodityRowClicked = function (commodity) {
            if (_.some($scope.commodityFilters, item => item.symbol === commodity.symbol)) {
                return;
            }

            $scope.commodityFilters.push(commodity);
            filterCommodities();
        };

        $scope.onCommodityTypeAheadChanged = function () {
            filterCommodities();
        };

        $scope.getTransactionLink = function (historyItem) {
            if (historyItem.symbol === 'ARK' && historyItem.transactionHash) {
                return "https://explorer.ark.io/transaction/" + historyItem.transactionHash;
            }
            return null;
        };

        var filterCommodities = function () {
            $scope.effectiveCommodities = _.filter($scope.commodities.data,
                function (queryCommoditiy) {
                    if (_.some($scope.commodityFilters, queryCommodityFilter => queryCommodityFilter.symbol === queryCommoditiy.symbol)) {
                        return false;
                    }

                    if ($scope.commodityTypeAheadText === undefined || $scope.commodityTypeAheadText === null) {
                        return true;
                    }

                    var trimmedUpperText = $scope.commodityTypeAheadText.toUpperCase();
                    if (trimmedUpperText.length <= 0) { return true; }
                    if (queryCommoditiy.symbol !== undefined
                        && queryCommoditiy.symbol !== null
                        && queryCommoditiy.symbol.length > 0
                        && queryCommoditiy.symbol.trim().toUpperCase().indexOf(trimmedUpperText) >= 0) {
                            return true;
                    }

                    if (queryCommoditiy.name !== undefined
                        && queryCommoditiy.name !== null
                        && queryCommoditiy.name.length > 0
                        && queryCommoditiy.name.trim().toUpperCase().indexOf(trimmedUpperText) >= 0) {
                            return true;
                    }

                    return false;
                });

            filterData();
        };

        var filterData = function () {
            $scope.effectiveData = [];
            for (var i = 0; i < $scope.history.data.length; i++) {
                var item = $scope.history.data[i];
                if ($scope.hideLivecoin && item.exchange.toUpperCase() === 'Livecoin'.toUpperCase()) {
                    continue;
                }

                if ($scope.hideCoss && item.exchange.toUpperCase() === 'Coss'.toUpperCase()) {
                    continue;
                }

                if ($scope.hideBinance && item.exchange.toUpperCase() === 'Binance'.toUpperCase()) {
                    continue;
                }

                if ($scope.hideMew && item.exchange.toUpperCase() === 'Mew'.toUpperCase()) {
                    continue;
                }

                if ($scope.hideBitz && item.exchange.toUpperCase() === 'Bit-Z'.toUpperCase()) {
                    continue;
                }

                if ($scope.hideKraken && item.exchange.toUpperCase() === 'Kraken'.toUpperCase()) {
                    continue;
                }

                if ($scope.hideCoinbase && item.exchange.toUpperCase() === 'Coinbase'.toUpperCase()) {
                    continue;
                }

                // hideIdex
                if ($scope.hideIdex && item.exchange.toUpperCase() === 'Idex'.toUpperCase()) {
                    continue;
                }

                if ($scope.hideBuy && item.tradeType.toUpperCase() === 'Buy'.toUpperCase()) {
                    continue;
                }

                if ($scope.hideSell && item.tradeType.toUpperCase() === 'Sell'.toUpperCase()) {
                    continue;
                }

                if ($scope.hideDeposit && item.tradeType.toUpperCase() === 'Deposit'.toUpperCase()) {
                    continue;
                }

                if ($scope.hideWithdraw && item.tradeType.toUpperCase() === 'Withdraw'.toUpperCase()) {
                    continue;
                }

                if ($scope.commodityFilters && $scope.commodityFilters.length > 0) {
                    if (item.symbol !== undefined && item.symbol !== null) {
                        var trimmedItemUpperSymbol = item.symbol.trim().toUpperCase();
                        if (!_.some($scope.commodityFilters, filter =>
                            filter === null
                            || filter.symbol === undefined
                            || filter.symbol === null
                            || filter.symbol.length <= 0
                            || filter.symbol.trim().toUpperCase() === trimmedItemUpperSymbol)) {
                            continue;
                        }
                    }
                }

                $scope.effectiveData.push(item);
            }

            $scope.pages = [];

            var totalItems = $scope.effectiveData.length;
            $scope.totalPages = parseInt(totalItems / $scope.pageSize);
            if (totalItems % $scope.pageSize !== 0) { $scope.totalPages++; }

            var maxPages = 10;
            for (var pageIndex = 0; pageIndex < $scope.totalPages && pageIndex < maxPages; pageIndex++) {
                $scope.pages.push({ displayText: (pageIndex + 1).toString(), pageIndex: pageIndex });
            }

            if ($scope.totalPages > maxPages) {
                $scope.pages.push({ displayText: "..", displayOnly: true });
                $scope.pages.push({ displayText: $scope.totalPages.toString(), pageIndex: $scope.totalPages - 1 });
            }
        };

        var afterSuccess = function () {
            filterData();
        };

        dataService.loadData($scope.history, function () { return $http.post('api/get-history'); }, afterSuccess);

        dataService.loadData($scope.commodities, function () { return commodityService.getCommodities(); },
            function () {
                filterCommodities();
            });
});