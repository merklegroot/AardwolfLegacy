angular.module('main')
    .controller('exchangeOpenOrdersDetailController', function (
        $scope,
        $stateParams,
        $timeout,
        dataService,
        alertService,
        exchangeDictionary,
        exchangeService,
        openOrderService,
        valuationService,
        orderBookService) {

        var controllerData = {
            exchangeId: $stateParams.exchange,
            symbol: $stateParams.exchange,
            baseSymbol: $stateParams.baseSymbol,
            openOrders: {},
            tradingPairs: {},
            valuations: {},
            valuationSymbols: [
                "ETH", "BTC", "COSS",
                "SNM", "SUB", "ENJ", "FYN",
                "CS", "LTC", "NEO", "UFR",
                "OMG", "MITX", "QASH", "BNT"
            ],
            orderBooks: {},
            compDictionary: [
                ["MITX", "hitbtc"],
                ["CAN", "kucoin"]
            ]
        };

        controllerData.exchangeName = exchangeDictionary[controllerData.exchangeId].displayName;

        $scope.showSymbolUsdValueColumn = false;
        $scope.showBaseSymbolUsdValueColumn = false;

        $scope.model = {
            exchangeId: controllerData.exchangeId,
            exchangeName: controllerData.exchangeName,
            openOrdersContainer: {},
            valuations: []
        };

        $scope.onRefreshTradingPairClicked = function (symbol, baseSymbol) {
            refreshTradingPair(symbol, baseSymbol);
        };

        $scope.onRefreshAllClicked = function () {
            refreshAll();
        };

        $scope.onRefreshOrderBookClicked = function (orderBook) {
            console.log(orderBook);

            var symbol = orderBook.symbol;
            var baseSymbol = orderBook.baseSymbol;
            var exchange = orderBook.exchange;

            loadOrderBook(exchange, symbol, baseSymbol, "ForceRefresh");
        };


        var loadOrderBook = function (exchange, symbol, baseSymbol, cachePolicy) {
            var closureData = {
                exchange: exchange,
                symbol: symbol,
                baseSymbol: baseSymbol,
                cachePolicy: cachePolicy,
                orderBook: {},
                controllerData: controllerData,
                model: $scope.model
            };

            var retriever = function () {
                return orderBookService.getOrderBook(closureData.symbol, closureData.baseSymbol, closureData.exchange, null, closureData.cachePolicy);
            }

            var onSuccess = function () {
                alertService.success("Loaded!");
                if (closureData.orderBook.data) {
                    closureData.orderBook.data.symbol = closureData.symbol;
                    closureData.orderBook.data.baseSymbol = closureData.baseSymbol;
                    applyOrderBook(closureData.orderBook.data, closureData.exchange);
                }
            };

            alertService.info("Loading order book " + exchange + " " + symbol + "-" + baseSymbol);
            dataService.loadData(closureData.orderBook, retriever, onSuccess);
        }

        var refreshAll = function () {
            alertService.info("Let's do this.");

            if (!controllerData.tradingPairs || controllerData.tradingPairs.length === 0) { return; }
            refreshOpenOrdersForTradingPairByIndex(0, _.cloneDeep(controllerData.tradingPairs));
        };

        var refreshOpenOrdersForTradingPairByIndex = function (index, tradingPairs) {
            var tradingPair = tradingPairs[index];

            alertService.info("(" + (index + 1) + " of " + tradingPairs.length + " ) Refreshing " + tradingPair.symbol + "-" + tradingPair.baseSymbol);

            var closureData = {
                index: index,
                tradingPair: tradingPair,
                tradingPairs: tradingPairs,
                openOrdersForTradingPair: {},
                controllerData: controllerData
            };

            var onComplete = function () {
                applyOpenOrders();
                if (closureData.index + 1 < closureData.tradingPairs.length) {
                    refreshOpenOrdersForTradingPairByIndex(closureData.index + 1, closureData.tradingPairs);
                }
            };

            var onFailure = function () { onComplete(); };

            var onSuccess = function () {
                alertService.success("Done loading " + closureData.tradingPair.symbol + "-" + closureData.tradingPair.baseSymbol);
                onComplete();
            };

            var retriever = function () {
                return openOrderService.getOpenOrdersForTradingPairV2(closureData.controllerData.exchangeId, closureData.tradingPair.symbol, closureData.tradingPair.baseSymbol, "AllowCache");
            };

            dataService.loadData(closureData.openOrdersForTradingPair, retriever, onSuccess, onFailure);
        };

        var refreshTradingPair = function (symbol, baseSymbol) {
            alertService.info("Refreshing " + symbol + "-" + baseSymbol);

            var closureData = {
                symbol: symbol,
                baseSymbol: baseSymbol,
                openOrdersForTradingPair: {},
                controllerData: controllerData
            };

            var retriever = function () { return openOrderService.getOpenOrdersForTradingPairV2(controllerData.exchangeId, symbol, baseSymbol, "ForceRefresh"); };

            var onSuccess = function () {
                alertService.success(closureData.openOrdersForTradingPair);
                var key = symbol.toUpperCase() + "_" + baseSymbol.toUpperCase();
                closureData.controllerData.openOrders[key].openOrders = _.cloneDeep(closureData.openOrdersForTradingPair.data.openOrders);
                closureData.controllerData.openOrders[key].asOfUtc = _.cloneDeep(closureData.openOrdersForTradingPair.data.asOfUtc);

                applyOpenOrders();
            };

            dataService.loadData(closureData.openOrdersForTradingPair, retriever, onSuccess);
        };

        var loadOpenOrders = function () {
            var retriever = function () { return openOrderService.getOpenOrdersV2(controllerData.exchangeId); };
            var closureData = {
                openOrders: {},
                controllerData: controllerData,
                model: $scope.model
            };

            var onSuccess = function () {
                closureData.controllerData.openOrders = {};
                for (var i = 0; i < closureData.openOrders.data.length; i++) {
                    var openOrder = closureData.openOrders.data[i];
                    var key = openOrder.symbol.toUpperCase() + "_" + openOrder.baseSymbol.toUpperCase();
                    closureData.controllerData.openOrders[key] = _.cloneDeep(openOrder);
                }

                applyOpenOrders();
                applyValuations();
                closureData.model.openOrdersContainer.isLoading = false;
            };

            closureData.model.openOrdersContainer.isLoading = true;
            dataService.loadData(closureData.openOrders, retriever, onSuccess);
        };

        var applyOpenOrders = function () {
            var keys = Object.keys(controllerData.openOrders);
            for (var i = 0; i < keys.length; i++) {
                var key = keys[i];

                var pieces = key.split("_");
                if (pieces.length !== 2) { continue; }
                var symbol = pieces[0];
                var baseSymbol = pieces[1];

                var openOrdersForTradingPair = _.cloneDeep(controllerData.openOrders[key]);
                applyOpenOrdersForTradingPair(openOrdersForTradingPair);
            }
        };

        var applyOpenOrdersForTradingPair = function (openOrdersForTradingPair) {
            if (!$scope.model.openOrdersContainer) { $scope.model.openOrdersContainer = {}; }
            if (!$scope.model.openOrdersContainer.data) { $scope.model.openOrdersContainer.data = []; }

            var modelOpenOrdersForTradingPair = _.find($scope.model.openOrdersContainer.data, function (queryOpenOrder) {
                return queryOpenOrder.symbol === openOrdersForTradingPair.symbol && queryOpenOrder.baseSymbol === openOrdersForTradingPair.baseSymbol;
            });

            var foundMatch = modelOpenOrdersForTradingPair ? true : false;
            if (!foundMatch) {
                modelOpenOrdersForTradingPair = {};
            }

            modelOpenOrdersForTradingPair.symbol = openOrdersForTradingPair.symbol;
            modelOpenOrdersForTradingPair.baseSymbol = openOrdersForTradingPair.baseSymbol;
            modelOpenOrdersForTradingPair.asOfUtc = openOrdersForTradingPair.asOfUtc;
            modelOpenOrdersForTradingPair.openOrders = _.cloneDeep(openOrdersForTradingPair.openOrders);

            if (!foundMatch) {
                $scope.model.openOrdersContainer.data.push(modelOpenOrdersForTradingPair);
            }
        };

        var loadTradingPairs = function () {
            var retriever = function () {
                return exchangeService.getTradingPairsForExchange(controllerData.exchangeId, null, 'OnlyUseCacheUnlessEmpty');
            };

            var closureData = {
                tradingPairs: {},
                controllerData: controllerData,
                model: $scope.model
            };

            var onSuccess = function () {
                alertService.success("trading pairs loaded");
                closureData.controllerData.tradingPairs = _.cloneDeep(closureData.tradingPairs.data);
                closureData.model.tradingPairs.data = _.cloneDeep(closureData.tradingPairs.data);
                closureData.model.tradingPairs.isLoading = false;
            };

            closureData.model.tradingPairs = { isLoading: true };
            dataService.loadData(closureData.tradingPairs, retriever, onSuccess);
        };

        var applyValuations = function () {
            var keys = Object.keys(controllerData.valuations);
            for (var i = 0; i < keys.length; i++) {
                var key = keys[i];

                applyValuation(key);
            }
        };

        var applyValuation = function (valuationSymbol) {
            var controllerValuation = controllerData.valuations[valuationSymbol];
            if (!controllerValuation.data) { return; }

            var modelValuations = $scope.model.valuations;
            var modelValuationMatch = _.find(modelValuations, function (queryModelValuation) {
                return queryModelValuation.symbol === valuationSymbol;
            });

            if (!modelValuationMatch) {
                modelValuationMatch = {
                    symbol: valuationSymbol,
                    isLoading: controllerValuation.isLoading,
                    data: controllerValuation.data ? _.cloneDeep(controllerValuation.data) : null
                };

                modelValuations.push(modelValuationMatch);
            }

            var modelOpenOrders = $scope.model.openOrdersContainer.data;
            if (!modelOpenOrders) { return; }
            for (var i = 0; i < modelOpenOrders.length; i++) {
                var openOrder = modelOpenOrders[i];
                if (!openOrder || (openOrder.symbol !== valuationSymbol && openOrder.baseSymbol !== valuationSymbol)) {
                    continue;
                }

                var symbolValuation = controllerData.valuations[openOrder.symbol];
                var baseSymbolValuation = controllerData.valuations[openOrder.baseSymbol];

                if (symbolValuation && symbolValuation.data) {
                    openOrder.symbolUsdValue = symbolValuation.data.usdValue;
                }

                if (baseSymbolValuation && baseSymbolValuation.data) {
                    openOrder.baseSymbolUsdValue = baseSymbolValuation.data.usdValue;
                }
            }

            console.log("here");
        };


        var loadValuationByIndex = function (index, symbols) {
            var closureData = {
                index: index,
                symbols: symbols,
                symbol: symbols[index],
                controllerData: controllerData,
                valuation: {}
            };

            var retriever = function () { return valuationService.getUsdValueV2(closureData.symbol, "AllowCache"); };

            var onComplete = function () {
                if (closureData.index + 1 < closureData.symbols.length) {
                    loadValuationByIndex(closureData.index + 1, closureData.symbols);
                }
            };

            var onSuccess = function () {
                closureData.controllerData.valuations[closureData.symbol] = _.cloneDeep(closureData.valuation);
                applyValuation(closureData.symbol);

                onComplete();
            };

            var onFailure = function () {
                onComplete();
            };

            if (!closureData.controllerData.valuations[closureData.symbol]) { closureData.controllerData.valuations[closureData.symbol] = {}; }
            closureData.controllerData.valuations[closureData.symbol].isLoading = true;

            dataService.loadData(closureData.valuation, retriever, onSuccess, onFailure);
        };

        var loadValuations = function () {
            var valuationSymbols = _.cloneDeep(controllerData.valuationSymbols);
            loadValuationByIndex(0, valuationSymbols);
        };

        var loadOrderBooksForExchange = function (exchange) {
            var closureData = {
                exchange: exchange,
                orderBooks: {},
                controllerData: controllerData,
                modelData: $scope.model
            };

            var retriever = function () {
                return orderBookService.getCachedOrderBooks(exchange);
            };

            var onSuccess = function () {
                var closureOrderBooks = closureData.orderBooks;
                if (closureData.orderBooks && closureData.orderBooks.data) {
                    for (var i = 0; i < closureData.orderBooks.data.length; i++) {
                        var orderBook = closureData.orderBooks.data[i];
                        applyOrderBook(orderBook, closureData.exchange);
                    }
                }

                console.log("here");
            };

            dataService.loadData(closureData.orderBooks, retriever, onSuccess);
        };

        var applyOrderBook = function (orderBook, exchange) {
            var key = exchange.toUpperCase() + "_" + orderBook.symbol.toUpperCase() + "_" + orderBook.baseSymbol.toUpperCase();
            var clonedOrderBook = _.cloneDeep(orderBook);
            clonedOrderBook.exchange = exchange;
            if (clonedOrderBook.asks && clonedOrderBook.asks.length > 0) {
                sortedAsks = _.orderBy(clonedOrderBook.asks, ["price"], ["asc"]);
                clonedOrderBook.asks = sortedAsks;
                clonedOrderBook.bestAsk = sortedAsks[0];
            }

            if (clonedOrderBook.bids && clonedOrderBook.bids.length > 0) {
                sortedBids = _.orderBy(clonedOrderBook.bids, ["price"], ["desc"]);
                clonedOrderBook.bids = sortedBids;
                clonedOrderBook.bestBid = sortedBids[0];
            }

            controllerData.orderBooks[key] = clonedOrderBook;

            var matchingOpenOrdersForTradingPair = _.find($scope.model.openOrdersContainer.data, function (queryItem) {
                return orderBook.symbol.toUpperCase() === queryItem.symbol.toUpperCase() && orderBook.baseSymbol.toUpperCase() === queryItem.baseSymbol.toUpperCase();
            });

            if (matchingOpenOrdersForTradingPair !== undefined && matchingOpenOrdersForTradingPair !== null) {
                var modelOrderBook = _.cloneDeep(clonedOrderBook);
                if (modelOrderBook.asks && modelOrderBook.asks.length > 0) {
                    modelOrderBook.asks = _.take(modelOrderBook.asks, 3);
                }

                if (modelOrderBook.bids && modelOrderBook.bids.length > 0) {
                    modelOrderBook.bids = _.take(modelOrderBook.bids, 3);
                }

                matchingOpenOrdersForTradingPair.orderBook = modelOrderBook;

                if (matchingOpenOrdersForTradingPair.openOrders) {
                    for (var i = 0; i < matchingOpenOrdersForTradingPair.openOrders.length; i++) {
                        var openOrder = matchingOpenOrdersForTradingPair.openOrders[i];
                        openOrder.statuses = [];

                        // Bid
                        if (openOrder.orderType === 1) {
                            if (clonedOrderBook.bestBid) {
                                var bidStatus;
                                if (openOrder.price >= clonedOrderBook.bestBid.price) {
                                    bidStatus = {
                                        desc: "This is the best bid on " + controllerData.exchangeName,
                                        disposition: "positive"
                                    };

                                    openOrder.statuses.push(status);
                                } else {
                                    bidStatus = {
                                        desc: "This bid is losing.",
                                        disposition: "negative"
                                    };

                                    openOrder.statuses.push(bidStatus);
                                }
                            }
                        }
                        // Ask
                        else if (openOrder.orderType === 2) {
                            if (clonedOrderBook.bestAsk) {
                                var askStatus;
                                if (openOrder.price <= clonedOrderBook.bestAsk.price) {
                                    askStatus = {
                                        desc: "This is the best ask on " + controllerData.exchangeName,
                                        disposition: "positive"
                                    };

                                    openOrder.statuses.push(status);
                                } else {
                                    askStatus = {
                                        desc: "This ask is losing.",
                                        disposition: "negative"
                                    };

                                    openOrder.statuses.push(askStatus);
                                }
                            }
                        }

                        console.log(openOrder);
                    }
                }
            }
        };

        var init = function () {
            loadTradingPairs();
            loadOpenOrders();
            loadValuations();
            loadOrderBooksForExchange(controllerData.exchangeId);
        };

        init();
    });