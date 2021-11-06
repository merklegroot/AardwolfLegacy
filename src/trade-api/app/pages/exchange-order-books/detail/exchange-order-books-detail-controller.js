angular.module('main')
    .controller("exchangeOrderBooksDetailController", function (
        $scope,
        $stateParams,
        $timeout,
        exchangeDictionary,
        dataService,
        alertService,
        orderBookService,
        exchangeService)
    {
        var exchange = $stateParams.exchange;

        $scope.model = {
            exchange: exchangeDictionary[exchange].displayName,
            tradingPairs: {},
            filteredTradingPairs: []
        };

        var loadTradingPairs = function (forceRefresh, cachePolicy) {
            var retriever = function () {
                return exchangeService.getTradingPairsForExchange(exchange, forceRefresh);
            };

            var onSuccess = function () {
                sortTradingPairs();
                updateFilteredTradingPairs();
                loadOrderBooks();
            };

            dataService.loadData($scope.model.tradingPairs, retriever, onSuccess);
        }

        $scope.onRefreshTradingPairsClicked = function () {
            loadTradingPairs(true);
        };

        $scope.onRefreshOrderBookClicked = function (tradingPair) {
            alertService.info(tradingPair);
            var retriever = function () {
                return orderBookService.getOrderBook(tradingPair.symbol, tradingPair.baseSymbol, exchange, true);
            };

            dataService.loadData(tradingPair.orderBook, retriever);
        }

        $scope.onFilterTextChanged = function () {
            updateFilteredTradingPairs();
        };

        var loadOrderBooks = function () {
            var clone = $scope.model.tradingPairs.data ? _.clone($scope.model.tradingPairs.data) : {};
            if (clone !== undefined && clone !== null && clone.length > 0) {
                // loadOrderBookByIndex(clone, 0, false, 'OnlyUseCache');
                loadOrderBookByIndex(clone, 0, false, 'OnlyUseCacheUnlessEmpty');
            }
        };

        var loadOrderBookByIndex = function (clone, index, forceRefresh, cachePolicy) {
            var item = clone[index];
            var orderBook = {};

            var retriever = function () {
                return orderBookService.getOrderBook(item.symbol, item.baseSymbol, exchange, forceRefresh, cachePolicy);
            };

            var onLoadingComplete = function () {
                var modelMatch = _.find($scope.model.tradingPairs.data, function (queryTp) { return queryTp.symbol === item.symbol && queryTp.baseSymbol === item.baseSymbol });;
                if (modelMatch) {
                    modelMatch.orderBook = orderBook;
                }

                var filteredMatch = _.find($scope.model.filteredTradingPairs, function (queryTp) { return queryTp.symbol === item.symbol && queryTp.baseSymbol === item.baseSymbol });;
                if (filteredMatch) {
                    modelMatch.orderBook = orderBook;
                }

                alertService.info("Loaded " + item.symbol + "-" + item.baseSymbol + " (" + index + 1 + " of " + clone.length + ")");

                if (index + 1 < clone.length) {
                    $timeout(function () {
                        loadOrderBookByIndex(clone, index + 1, forceRefresh, cachePolicy);
                    }, 1);
                }
            };

            dataService.loadData(orderBook, retriever, onLoadingComplete, onLoadingComplete);
        };

        var sortTradingPairs = function () {
            $scope.model.tradingPairs.data = _.sortBy($scope.model.tradingPairs.data,
                function (item) {
                    return item.symbol;
                });
        };

        var updateFilteredTradingPairs = function () {
            var filterText = $scope.filterText;
            var tradingPairs = $scope.model.tradingPairs.data;

            if (filterText !== undefined && filterText !== null) {
                filterText = filterText.trim().toUpperCase();
            }

            var filtered;
            if (filterText !== undefined &&
                filterText !== null &&
                filterText.length !== 0) {
                filtered = _.filter(tradingPairs, function (item) {
                    if (item.symbol !== undefined && item.symbol !== null && item.symbol.indexOf(filterText) >= 0) {
                        return true;
                    }

                    return false;
                });
            } else {
                filtered = tradingPairs;
            }

            var sorted = _.sortBy(filtered, function (item) { return item.symbol; });

            $scope.model.filteredTradingPairs = sorted;
        };

        loadTradingPairs(false, 'onlyUseCache');
    });



