angular.module('main')
    .controller('commodityDetailController', function (
        $stateParams,
        $scope,
        dataService,
        alertService,
        valuationService,
        commodityService,
        orderBookService,
        exchangeDictionary) {

        var controllerData = {
            symbol: $stateParams.symbol,
            valuations: {}
        };

        $scope.model = {
            symbol: controllerData.symbol.toUpperCase(),
            displayName: controllerData.symbol.toUpperCase(),
            commodity: {},
            baseSymbols: [],
            exchanges: [],
            valuations: [],
            bestBid: {},
            bestAsk: {}
        };

        $scope.getDisplayName = function () {
            if (!$scope.commodity.data) { return controllerData.symbol; }
            if ($scope.commodity.data.canonicalName !== undefined && $scope.commodity.data.canonicalName !== null
                && $scope.commodity.data.canonicalName.length > 0
                && $scope.commodity.data.canonicalName.toUpperCase() !== $scope.commodity.data.symbol.toUpperCase()) {
                return $scope.commodity.data.canonicalName + " (" + $scope.commodity.data.symbol + ")";
            }

            return $scope.commodity.data.symbol;
        };

        var updateBests = function () {
            var bestBid = null;
            var bestAsk = null;

            for (var i = 0; i < $scope.model.exchanges.length; i++) {
                var exchange = $scope.model.exchanges[i];
                if (!exchange || !exchange.orderBooks) { continue; }
                for (var j = 0; j < exchange.orderBooks.length; j++) {
                    var orderBook = exchange.orderBooks[j];
                    if (!orderBook || !orderBook.data) { continue; }

                    var baseSymbol = orderBook.baseSymbol;
                    if (baseSymbol === undefined || baseSymbol === null) { continue; }

                    var matchingValuation = controllerData.valuations[baseSymbol.toUpperCase()];
                    if (!matchingValuation) { continue; }

                    var valuationPrice = matchingValuation.data;
                    if (!valuationPrice) { continue; }

                    var currentOrder = {};
                    var asks = orderBook.data.asks;
                    if (asks) {
                        for (var askIndex = 0; askIndex < asks.length; askIndex++) {
                            currentOrder = asks[askIndex];
                            if (!currentOrder) { continue; }

                            var usdPrice = currentOrder.price * valuationPrice;

                            if (!bestAsk || !bestAsk.usdPrice || usdPrice < bestAsk.usdPrice) {
                                bestAsk = {
                                    exchangeId: exchange.id,
                                    exchangeDisplayName: exchange.displayName,
                                    quantity: currentOrder.quantity,
                                    baseSymbol: baseSymbol,
                                    symbolPrice: currentOrder.price,
                                    usdPrice: usdPrice
                                };
                            }
                        }
                    }

                    var bids = orderBook.data.bids;
                    if (bids) {
                        for (var bidIndex = 0; bidIndex < bids.length; bidIndex++) {
                            currentOrder = bids[bidIndex];
                            if (!currentOrder) { continue; }

                            var xUsdPrice = currentOrder.price * valuationPrice;

                            if (!bestBid || !bestBid.usdPrice || xUsdPrice > bestBid.usdPrice) {
                                bestBid = {
                                    exchangeId: exchange.id,
                                    exchangeDisplayName: exchange.displayName,
                                    baseSymbol: baseSymbol,
                                    quantity: currentOrder.quantity,
                                    symbolPrice: currentOrder.price,
                                    usdPrice: xUsdPrice
                                };
                            }
                        }
                    }
                }
            }

            $scope.model.bestBid = bestBid;
            $scope.model.bestAsk = bestAsk;
        };

        var loadOrderBookByIndex = function (exchange, orderBookIndex) {
            var orderBook = exchange.orderBooks[orderBookIndex];
            var baseSymbol = orderBook.baseSymbol;

            var onSuccess = function () {
                updateBests();

                if (orderBookIndex + 1 < exchange.orderBooks.length) {
                    loadOrderBookByIndex(exchange, orderBookIndex + 1);
                }
            };

            if (!_.some(exchange.baseSymbols, function (queryBaseSymbol) { return queryBaseSymbol === baseSymbol; })) {
                onSuccess();
                return;
            }
            
            var retriever = function () {
                return orderBookService.getOrderBook(
                    controllerData.symbol,
                    baseSymbol,
                    exchange.id,
                    null,
                    'OnlyUseCacheUnlessEmpty');
            };  

            dataService.loadData(orderBook, retriever, onSuccess);
        };

        var loadInitialOrderBooksForExchange = function (exchange) {
            if (!exchange.baseSymbols) { return; }
            exchange.orderBooks = [];
            for (var i = 0; i < $scope.model.baseSymbols.length; i++) {
                var baseSymbol = $scope.model.baseSymbols[i];
                if (baseSymbol.toUpperCase() === "USD") { continue; }
                var orderBook = { baseSymbol: baseSymbol };
                exchange.orderBooks.push(orderBook);
            }

            updateBests();

            loadOrderBookByIndex(exchange, 0);
        };

        var loadInitialOrderBooks = function () {
            if (!$scope.model.exchanges) { return; }
            for (var exchangeIndex = 0; exchangeIndex < $scope.model.exchanges.length; exchangeIndex++) {
                var exchange = $scope.model.exchanges[exchangeIndex];
                if (exchange.id.toUpperCase() === 'MEW') { continue; }
                loadInitialOrderBooksForExchange(exchange);
            }
        };

        $scope.onRefreshValuationClicked = function (valuation) {
            var valuationSymbol = valuation.symbol;
            var retriever = function () {
                return valuationService.getUsdValue(valuationSymbol, true);
            };

            var onSuccess = function () {
                updateBests();
            };

            dataService.loadData(valuation, retriever, onSuccess);
        };

        $scope.onRefreshOrderBookClicked = function (exchange, orderBookModel) {
            var closureData = {
                exchangeId: exchange.id,
                symbol: controllerData.symbol,
                baseSymbol: orderBookModel.baseSymbol,
                orderBookModel: orderBookModel,
                orderBook: {}
            };

            var onSuccess = function () {
                // _.merge(closureData.orderBook, closureData.orderBookModel);
                closureData.orderBookModel.isLoading = closureData.orderBook.isLoading;
                closureData.orderBookModel.data.asks = closureData.orderBook.data.asks;
                closureData.orderBookModel.data.bids = closureData.orderBook.data.bids;
                closureData.orderBookModel.data.asOf = closureData.orderBook.data.asOf;
                alertService.success("Order book loaded!");
                updateBests();
            };

            var retriever = function () {
                return orderBookService.getOrderBook(
                    closureData.symbol,
                    closureData.baseSymbol,
                    closureData.exchangeId,
                    true);
            };

            alertService.info("Loading order book");
            closureData.orderBookModel.isLoading = true;
            dataService.loadData(closureData.orderBook, retriever, onSuccess);
        };

        var loadValuationByIndex = function (valuationSymbols, valuationIndex) {
            var closureData = {
                valuation: {},
                controllerData: controllerData,
                model: $scope.model,
                valuationSymbols: valuationSymbols,
                valuationSymbol: valuationSymbols[valuationIndex],
                valuationIndex: valuationIndex
            };
            
            var onComplete = function () {
                if (closureData.valuationIndex + 1 < closureData.valuationSymbols.length) {
                    loadValuationByIndex(closureData.valuationSymbols, closureData.valuationIndex + 1);
                }
            };

            var onSuccess = function () {                
                applyValuation(closureData.valuationSymbol, closureData.valuation);
                onComplete();                
            };

            var retriever = function () {
                return valuationService.getUsdValue(closureData.valuationSymbol);
            };

            dataService.loadData(closureData.valuation, retriever, onSuccess, onComplete);
        };

        var applyValuation = function (valuationSymbol, valuation) {
            controllerData.valuations[valuationSymbol.toUpperCase()] = _.cloneDeep(valuation);

            var valuationModel = _.cloneDeep(valuation);
            valuationModel.symbol = valuationSymbol;

            var modelMatch = _.find($scope.model.valuations, function (item) { return item.symbol.toUpperCase() === valuationSymbol.toUpperCase(); });
            if (modelMatch) {
                modelMatch.data = valuationModel.data;
                modelMatch.isLoading = false;
            } else {
                $scope.model.valuations.push(valuationModel);
            }

            updateBests();
        };

        var loadValuations = function () {
            $scope.model.valuationSymbols = [];

            $scope.model.valuationSymbols.push(controllerData.symbol);
            for (var i = 0; i < $scope.model.baseSymbols.length; i++) {
                var baseSymbol = $scope.model.baseSymbols[i];
                $scope.model.valuationSymbols.push(baseSymbol);
            }

            loadValuationByIndex($scope.model.valuationSymbols, 0);
        };

        var onCommodityLoaded = function () {
            if ($scope.model.commodity.data.canonicalName) {
                $scope.model.displayName = $scope.model.commodity.data.canonicalName + " (" + controllerData.symbol.toUpperCase() + ")";
            }

            var keys = $scope.model.commodity.data.exchanges ? Object.keys($scope.model.commodity.data.exchanges) : [];
            $scope.model.exchanges = _.map(keys, function (key) {
                var baseSymbols = $scope.model.commodity.data.exchanges[key];

                if (baseSymbols) {
                    for (var baseSymbolIndex = 0; baseSymbolIndex < baseSymbols.length; baseSymbolIndex++) {
                        var baseSymbol = baseSymbols[baseSymbolIndex].toUpperCase();
                        if (!_.some($scope.model.baseSymbols, function (item) { return item === baseSymbol; })) {
                            $scope.model.baseSymbols.push(baseSymbol);
                        }
                    }
                }

                var displayName = key.toLowerCase();
                if (exchangeDictionary[key.toLowerCase()]) {
                    displayName = exchangeDictionary[key.toLowerCase()].displayName;
                }

                var details = _.find($scope.model.commodity.data.exchangesWithDetails, function (item) {
                    return item && item.exchange && item.exchange.toUpperCase() === key.toUpperCase();
                });

                return {
                    id: key.toLowerCase(),
                    displayName: displayName,
                    baseSymbols: baseSymbols,
                    canDeposit: details && details.canDeposit,
                    canWithdraw: details && details.canWithdraw
                };
            });

            loadInitialOrderBooks();
            loadValuations();
        };

        var loadCommodityDetails = function () {
            var retriever = function () { return commodityService.getCommodityDetails(controllerData.symbol, false); };
            var onSuccess = function () { onCommodityLoaded(); };

            return dataService.loadData($scope.model.commodity, retriever, onSuccess);

        };

        var init = function () {
            loadCommodityDetails();
        };

        init();
    });