angular.module('main')
    .controller("exchangeCommodityDetailController", function ($stateParams, $scope, $http, $uibModal, $timeout,
        dataService, alertService,
        orderBookService, balanceService,
        valuationService, commodityService) {

        $scope.exchange = $stateParams.exchange;
        $scope.nativeSymbol = $stateParams.nativeSymbol;

        $scope.model = {
            commodity: {},
            canon: {},
            selectedCanonId: null,
            canonList: {},
            orderBooks: [],
            balance: {},
            exchanges: {},
            valuations: []
        };

        dataService.loadData($scope.model.commodity, function () {
            return commodityService.getExchangeCommodityByNativeSymbol($scope.exchange, $scope.nativeSymbol);
        }, function () {
            selectCanonFromNative();
            loadOrderBooks();
            loadBalance();
            loadExchanges();
            loadValuations();
        });

        dataService.loadData($scope.model.canonList, function () { return $http.post('api/get-canon'); }, function () {
            updateFiltered();
            selectCanonFromNative();
        });

        var loadBalance = function (forceRefresh) {
            var symbol = $scope.model.commodity.data.symbol;
            var retriever = function () { return balanceService.getBalance(symbol, $scope.exchange, forceRefresh); };
            dataService.loadData($scope.model.balance, retriever, function () {
                if (forceRefresh) {
                    alertService.success('Done refreshing balance.');
                }
            });
        };

        var loadExchanges = function () {
            var symbol = $scope.model.commodity.data.symbol;
            var serviceModel = { symbol: symbol, forceRefresh: false };
            var retriever = function () { return $http.post('api/get-exchanges-for-commodity', serviceModel); };
            dataService.loadData($scope.model.exchanges, retriever);
        };

        var loadValuations = function () {

            loadValuation($scope.model.commodity.data.symbol);

            //if ($scope.model.valuations[$scope.model.commodity.data.symbol] === undefined || $scope.model.valuations[$scope.model.commodity.data.symbol] === null) {
            //    $scope.model.valuations[$scope.model.commodity.data.symbol] = {  };
            //}

            // dataService.loadData($scope.model.valuations[$scope.model.commodity.data.symbol], function () { return valuationService.getUsdValue($scope.model.commodity.data.symbol); });

            for (var i = 0; $scope.model.commodity.data.baseSymbols && i < $scope.model.commodity.data.baseSymbols.length; i++) {
                var baseSymbol = $scope.model.commodity.data.baseSymbols[i];
                loadValuation(baseSymbol);
            }
        };

        var loadValuation = function (symbol) {
            var match = _.find($scope.model.valuations, function (queryItem) { return queryItem.symbol !== undefined && queryItem.symbol !== null && queryItem.symbol.toUpperCase() === symbol.toUpperCase(); });
            if (match === undefined || match === null) {
                match = { symbol: symbol };
                $scope.model.valuations.push(match);
            }

            dataService.loadData(match, function () { return valuationService.getUsdValue(symbol); });
        };

        $scope.onFilterChanged = function () {
            // alertService.info($scope.filter);

            updateFiltered();
        };

        $scope.onCanonListItemClicked = function (canonListItem) {
            // alertService.info(canonListItem);
            $scope.model.canon = canonListItem;
        };

        $scope.getStyle = function (canonListItem) {
            if ($scope.model.commodity.data
                && $scope.model.commodity.data.canonicalId === canonListItem.id) {
                return { 'background-color': '#00FFFF' };
            }

            if ($scope.model.canon
                && $scope.model.canon.id === canonListItem.id) {
                return { 'background-color': '#FFFF00' };
            }

            return {};
        };

        var updateFiltered = function () {
            if (!$scope.model.canonList.data) {
                $scope.filtered = [];
                return;
            }

            $scope.filtered = _.filter($scope.model.canonList.data, function (item) {
                if ($scope.filter === undefined || $scope.filter === null) { return true; }
                var effectiveFilter = $scope.filter.trim();
                if (effectiveFilter.length === 0) { return true; }

                if (item.symbol !== undefined
                    && item.symbol !== null
                    && item.symbol.toUpperCase().indexOf(effectiveFilter.toUpperCase()) !== -1) { return true; }

                if (item.name !== undefined
                    && item.name !== null
                    && item.name.toUpperCase().indexOf(effectiveFilter.toUpperCase()) !== -1) { return true; }

                return false;
            });
        };

        var loadOrderBooks = function () {
            $scope.model.orderBooks = [];
            if (!$scope.model.commodity.data.baseSymbols) { return; }
            for (var i = 0; i < $scope.model.commodity.data.baseSymbols.length; i++) {
                var symbol = $scope.model.commodity.data.symbol;
                var baseSymbol = $scope.model.commodity.data.baseSymbols[i];
                var orderBook = {
                    symbol: symbol,
                    baseSymbol: baseSymbol
                };

                dataService.loadData(orderBook,
                    function () { return orderBookService.getOrderBook(symbol, baseSymbol, $scope.exchange, false); });

                $scope.model.orderBooks.push(orderBook);
            }
        };

        $scope.onRefreshOrderBookClicked = function (orderBook) {
            dataService.loadData(orderBook,
                function () { return orderBookService.getOrderBook(orderBook.symbol, orderBook.baseSymbol, $scope.exchange, true); });
        };

        $scope.getOrderBookStyle = function (orderBook) {
            return orderBook.isLoading ? { 'background-color': '#DDDDDD' } : {};
        };

        $scope.onMapCanonClicked = function () {
            if (!$scope.model.canon) { return; }
            var confirmationText = 'Are you sure you want to map this commodity to canon "' + $scope.model.canon.id + '"?';
            var confirmationResult = confirm(confirmationText);
            if (!confirmationResult) {
                alertService.info("Not mapping.");
                return;
            }

            alertService.info("Doing it!");

            var serviceModel = {
                exchange: $scope.model.commodity.data.exchange,
                nativeSymbol: $scope.model.commodity.data.nativeSymbol,
                canonicalId: $scope.model.canon.id
            }

            var onSuccess = function (response) {
                    alertService.success("Mapped!" + "\r\n" + response.data);
            };

            var onError = function (err) {
                    alertService.onError("Failed to map!" + "\r\n" + err)
            };

            $http.post('api/map-canon', serviceModel)
                .then(onSuccess, onError);          
        };

        $scope.onRefreshCommodityClicked = function () {
            alertService.info('Refreshing...');
            dataService.loadData($scope.model.commodity, function () { return commodityService.getExchangeCommodityByNativeSymbol($scope.exchange, $scope.nativeSymbol); }, function () {
                alertService.success("Refreshed!");
            });
        };

        $scope.onRefreshBalanceClicked = function () {
            alertService.info('Refreshing...');
            loadBalance(true);
        };

        var selectCanonFromNative = function () {
            if ($scope.model.commodity && $scope.model.commodity.data) {
                $scope.model.canon = getCanonById($scope.model.commodity.data.canonicalId);
            } else {
                $scope.model.canon = null;
            }
        };

        var getCanonById = function (id) {
            var matches = _.filter($scope.model.canonList.data, function (item) {
                return item.id === id;
            });

            return matches !== null && matches.length >= 1 ? matches[0] : null;
        };
    });
