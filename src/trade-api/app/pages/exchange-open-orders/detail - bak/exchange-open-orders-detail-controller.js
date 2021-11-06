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
        orderBookService) {

        var _exchange = $stateParams.exchange;
        var _symbol = $stateParams.symbol;
        var _baseSymbol = $stateParams.baseSymbol;

        var _exchangeName = exchangeDictionary[_exchange].displayName;
        var _tradingPairs = {};
        var _openOrders = {};
        var _orderBooks = {};
        var _exchanges = [ _exchange, 'binance', 'hitbtc' ];

        $scope.model = {
            exchangeName: _exchangeName,
            exchangeId: _exchange,
            symbol: _symbol,
            baseSymbol: _baseSymbol,
            tradingPairs: {},

            // deprecated. TODO: change the references to point to $scope.model.exchangeName
            exchange: _exchangeName,
            exchanges: _.map(_exchanges, function (queryExchangeId) {
                return { id: queryExchangeId, name: exchangeDictionary[queryExchangeId].displayName };
            }),

            // problably not used anymore. verify and get rid of.
            openOrders: {},
            orderBooks: {}
        };

        $scope.onRefreshTheOldStuffClicked = function () {
            refreshTheOldStuff();
        };

        $scope.onRefreshTradingPairClicked = function (tradingPair) {
            refreshTradingPair(tradingPair);
        };

        $scope.onRefreshOrderBookClicked = function (exchange, symbol, baseSymbol) {
            loadOrderBook(exchange, symbol, baseSymbol);            
        };

        var refreshTradingPair = function (tradingPair, onCompleted) {
            alertService.info(tradingPair);
            tradingPair.openOrderData.isLoading = true;

            var retriever = function () {
                return openOrderService.getOpenOrdersForTradingPairV2(_exchange, tradingPair.symbol, tradingPair.baseSymbol, 'ForceRefresh');
            };

            var completed = function () {
                tradingPair.openOrderData.isLoading = false;
                if (onCompleted) { onCompleted(); }
            };

            var dataClosure = { tradingPair: tradingPair, result: {}, onCompleted: completed };

            var onSuccess = function () {
                alertService.success(dataClosure.result);

                dataClosure.tradingPair.openOrderData.openOrders = dataClosure.result.data.openOrders;
                dataClosure.tradingPair.openOrderData.asOfUtc = dataClosure.result.data.asOfUtc;

                dataClosure.onCompleted();
            };
            var onFailure = function () {
                dataClosure.onCompleted();
            };

            dataService.loadData(dataClosure.result, retriever, onSuccess, onFailure);
        };

        var refreshTheOldStuff = function () {
            var keys = Object.keys(_openOrders.data);
            var sortedKeys = _.orderBy(keys, function (queryKey) {
                var item = _openOrders.data[queryKey];
                return item.asOfUtc;
            });

            var asOfs = _.map(sortedKeys, function (queryKey) {
                var item = _openOrders.data[queryKey];
                return item.asOfUtc;
            });

            refreshTheOldStuffByIndex(sortedKeys, 0);
        };

        var refreshTheOldStuffByIndex = function (keys, index) {
            var currentTimeUtc = new Date();

            var key = keys[index];
            var item = _openOrders.data[key];
            var asOfUtc = new Date(item.asOfUtc);
            var ageMilliseconds = currentTimeUtc - asOfUtc;
            var ageSeconds = ageMilliseconds / 1000;
            var ageMinutes = ageSeconds / 60;
            var ageHours = ageMinutes / 60;

            if (ageMinutes > 30) {
                alertService.info(key + " is old. ( " + ageMinutes.toFixed(0) + " min)");
                var pieces = key.split('_');
                var symbol = pieces[0];
                var baseSymbol = pieces[1];

                var tradingPair = _.find(_tradingPairs.data, function (queryTradingPair) {
                    return queryTradingPair.symbol.toUpperCase() === symbol.toUpperCase()
                        && queryTradingPair.baseSymbol.toUpperCase() === baseSymbol.toUpperCase();
                });
                
                if (tradingPair) {
                    alertService.info(tradingPair);

                    var scopeTradingPair = _.find($scope.model.tradingPairs.data,
                        function (queryTradingPair) {
                            return queryTradingPair.symbol.toUpperCase() === symbol.toUpperCase()
                                && queryTradingPair.baseSymbol.toUpperCase() === baseSymbol.toUpperCase();
                        });

                    var closureData = {
                        tradingPair: tradingPair,
                        keys: keys,
                        index: index,
                        key: key,
                        openOrders: {},
                        orderGroup: _openOrders.data[key],
                        scopeTradingPair: scopeTradingPair
                    };

                    var retriever = function () {
                        return openOrderService.getOpenOrdersForTradingPairV2(
                            _exchange,
                            tradingPair.symbol,
                            tradingPair.baseSymbol,                            
                            'ForceRefresh');
                    };

                    var onSuccess = function () {                                               
                        closureData.orderGroup.asOfUtc = closureData.openOrders.data.asOfUtc;
                        closureData.orderGroup.openOrders = _.cloneDeep(closureData.openOrders.data.openOrders);

                        closureData.scopeTradingPair.openOrderData = _.cloneDeep(closureData.openOrders.data);
                        closureData.scopeTradingPair.asOfUtc = closureData.openOrders.data.asOfUtc;
                        closureData.scopeTradingPair.isLoading = false;

                        alertService.info("Finished setting values...");

                        if (closureData.index + 1 < closureData.keys.length) {
                            $timeout(function () {
                                refreshTheOldStuffByIndex(closureData.keys, closureData.index + 1);
                            }, 25);                            
                        }                        
                    };

                    var onFailure = function () {
                        alertService.onError("oh nose!");
                    };

                    closureData.scopeTradingPair.isLoading = true;
                    dataService.loadData(closureData.openOrders, retriever, onSuccess, onFailure);
                }
            }
            else {
                if (index + 1 < keys.length) {
                    refreshTheOldStuffByIndex(keys, index + 1);
                }
            }
        };

        var getMnemonic = function (tradingPair) {
            return tradingPair.symbol.toUpperCase()
                + "_"
                + tradingPair.baseSymbol.toUpperCase();
        };

        var loadOrderBook = function (exchange, symbol, baseSymbol) {
            alertService.info("Loading " + exchange + "-" + symbol + "-" + baseSymbol);

            var orderBook = {};
            var closureData = { exchange: exchange, symbol: symbol, baseSymbol: baseSymbol, orderBook: orderBook };

            var retriever = function () { return orderBookService.getOrderBook(symbol, baseSymbol, exchange, null, 'ForceRefresh'); };

            var onSuccess = function () {
                alertService.success("Loaded " + closureData.exchange + "-" + closureData.symbol + "-" + closureData.baseSymbol);
                var orderBooksForExchange = _orderBooks[closureData.exchange].data;
                var matchingOrderBook = _.find(orderBooksForExchange, function (queryOrderBook) {
                    return queryOrderBook.symbol.toUpperCase() === closureData.symbol.toUpperCase()
                        && queryOrderBook.baseSymbol.toUpperCase() === closureData.baseSymbol.toUpperCase();
                });

                matchingOrderBook.asOf = closureData.orderBook.data.asOf;
                matchingOrderBook.asks = _.cloneDeep(closureData.orderBook.data.asks);
                matchingOrderBook.bids = _.cloneDeep(closureData.orderBook.data.bids);

                var matchingScopeTradingPair = _.find($scope.model.tradingPairs.data, function (queryTradingPair) {
                    return queryTradingPair.symbol.toUpperCase() === closureData.symbol.toUpperCase()
                        && queryTradingPair.baseSymbol.toUpperCase() === closureData.baseSymbol.toUpperCase();
                });

                var scopeOrderBook = matchingScopeTradingPair.orderBooks[closureData.exchange];

                scopeOrderBook.data.asOf = closureData.orderBook.data.asOf;
                scopeOrderBook.data.asks = _.cloneDeep(closureData.orderBook.data.asks);
                scopeOrderBook.data.bids = _.cloneDeep(closureData.orderBook.data.bids);

                applyNotableOrdersForTradingPair(matchingScopeTradingPair);
            };

            dataService.loadData(orderBook, retriever, onSuccess);
        };

        var loadOrderBookByIndex = function (exchange, tradingPairs, index, cachePolicy, onAllCompleted) {
            var tradingPair = tradingPairs[index];
            var mnemonic = getMnemonic(tradingPair);
            var orderBook = _orderBooks[mnemonic];
            if (orderBook === undefined || orderBook === null) {
                _orderBooks[mnemonic] = orderBook = {
                    symbol: tradingPair.symbol,
                    baseSymbol: tradingPair.baseSymbol,
                    exchanges: {}
                };
            }

            if (!orderBook.exchanges[exchange]) {
                orderBook.exchanges[exchange] = {
                    exchange: exchange
                };
            }

            var retrieverClosureData = {
                symbol: tradingPair.symbol,
                baseSymbol: tradingPair.baseSymbol,
                exchange: exchange,
                cachePolicy: cachePolicy,
                index: index,
                onAllCompleted: onAllCompleted,
                tradingPairs: tradingPairs
            };

            var onSuccessClosureData = {
                symbol: tradingPair.symbol,
                baseSymbol: tradingPair.baseSymbol,
                exchange: exchange,
                cachePolicy: cachePolicy,
                index: index,
                onAllCompleted: onAllCompleted,
                tradingPairs: tradingPairs
            };

            var retriever = function () {
                return orderBookService.getOrderBook(
                    retrieverClosureData.symbol,
                    retrieverClosureData.baseSymbol,
                    retrieverClosureData.exchange,
                    null,
                    retrieverClosureData.cachePolicy);
            };

            var onSuccess = function () {
                onOrderBookLoaded(
                    onSuccessClosureData.exchange,
                    onSuccessClosureData.tradingPairs,
                    onSuccessClosureData.index,
                    onSuccessClosureData.cachePolicy,
                    onSuccessClosureData.onAllCompleted);
            };

            var onError = function (err) {
                alertService.error('oh nose! ' + err);
            };


            if (exchange === 'coss') {
                console.log('here');
            }

            dataService.loadData(orderBook.exchanges[exchange], retriever, onSuccess, onError);
        };

        var onOrderBookLoaded = function (exchange, tradingPairs, index, cachePolicy, onAllCompleted) {
            var tradingPair = tradingPairs[index];
            var exchanges = _orderBooks[getMnemonic(tradingPair)].exchanges;
            if (!exchanges[exchange]) {
                exchanges[exchange] = { exchange: exchange };
            }

            var orderBook = _orderBooks[getMnemonic(tradingPair)].exchanges[exchange];
            var bestAskPrice = orderBook.data.asks
                ? _.orderBy(orderBook.data.asks, 'price', 'asc')[0].price
                : null;

            var bestBidPrice = orderBook.data.bids
                ? _.orderBy(orderBook.data.bids, 'price', 'desc')[0].price
                : null;

            var openOrdersForTradingPair = _.filter($scope.model.openOrders.data, function (queryOpenOrder) {
                return queryOpenOrder.symbol.toUpperCase() === tradingPair.symbol.toUpperCase()
                    && queryOpenOrder.baseSymbol.toUpperCase() === tradingPair.baseSymbol.toUpperCase();
            });

            for (var i = 0; i < openOrdersForTradingPair.length; i++) {
                var openOrder = openOrdersForTradingPair[i];
                if (_exchange.toUpperCase() === exchange.toUpperCase()) {
                    openOrder.bestAskPrice = bestAskPrice;
                    openOrder.bestBidPrice = bestBidPrice;
                } else {
                    openOrder.bestCompAskPrice = bestAskPrice;
                    openOrder.bestCompBidPrice = bestBidPrice;
                }
            }

            if (index + 1 < tradingPairs.length) {
                $timeout(function () {
                    loadOrderBookByIndex(exchange, tradingPairs, index + 1, cachePolicy, onAllCompleted);
                }, 10);
            } else {
                if (onAllCompleted) {
                    onAllCompleted();
                }
            }
            console.log('here');
        };

        var onOpenOrdersLoaded = function () {
            combineData();
        };

        var combineData = function () {
            var tradingPairs = [];

            for (var i = 0; i < _tradingPairs.data.length; i++) {
                var tradingPair = _.cloneDeep(_tradingPairs.data[i]);
                var key = tradingPair.symbol.toUpperCase() + "_" + tradingPair.baseSymbol.toUpperCase();
                var matchingOrderGroup = _openOrders.data[key];
                if (matchingOrderGroup !== undefined && matchingOrderGroup !== null) {
                    tradingPair.openOrderData = matchingOrderGroup;
                    tradingPair.devoidOfOpenOrders = matchingOrderGroup && matchingOrderGroup.openOrders && matchingOrderGroup.openOrders.length > 0 ? 0 : 1;
                } else {
                    tradingPair.devoidOfOpenOrders = 1;
                }

                tradingPair.isLoading = false;
                tradingPairs.push(tradingPair);
            }

            var sortedTradingPairs = _.orderBy(tradingPairs, ['devoidOfOpenOrders', 'symbol', 'baseSymbol']);
            $scope.model.tradingPairs.data = sortedTradingPairs;
            $scope.model.tradingPairs.isLoading = false;
        };

        var loadOpenOrders = function (callback) {
            var retriever = function () {
                return openOrderService.getOpenOrdersV2(_exchange);
            };

            var closureData = {};
            var onSuccess = function () {
                alertService.success('v2 loaded!');
                var openOrdersDictionary = {};

                for (var i = 0; i < closureData.openOrders.data.length; i++) {
                    var item = closureData.openOrders.data[i];

                    var symbol = item.symbol.toUpperCase();
                    var baseSymbol = item.baseSymbol.toUpperCase();
                    var key = symbol + "_" + baseSymbol;
                    openOrdersDictionary[key] = item;
                }

                _openOrders.isLoading = false;
                _openOrders.data = openOrdersDictionary;

                onOpenOrdersLoaded();

                if (callback) {
                    callback();
                }
            };

            alertService.info('Loading Open Orders...');
            closureData.openOrders = {};
            _openOrders.isLoading = true;
            dataService.loadData(closureData.openOrders, retriever, onSuccess);
        };

        var onTradingPairsLoaded = function () {
            $scope.model.tradingPairs = _.cloneDeep(_tradingPairs);
            loadOpenOrders(function () {
                loadOrderBooks();
            });
        };

        var loadTradingPairs = function () {
            var retriever = function () {
                return exchangeService.getTradingPairsForExchange(_exchange, null, 'OnlyUseCacheUnlessEmpty');
            };

            if (_symbol && _baseSymbol) {
                _tradingPairs = {
                    isLoading: false,
                    data: [{ symbol: _symbol, baseSymbol: _baseSymbol }]
                };

                onTradingPairsLoaded();
            } else {
                $scope.model.tradingPairs = { isLoading: true };
                dataService.loadData(_tradingPairs, retriever, onTradingPairsLoaded());
            }
        };

        var loadOrderBooksForExchange = function (exchange) {
            var orderBooks = {};
            var closureData = { orderBooks: orderBooks, exchange: exchange };

            var retriever = function () {
                return orderBookService.getCachedOrderBooks(exchange);
            };

            var onSuccess = function () {
                _orderBooks[closureData.exchange] = closureData.orderBooks;
                applyOrderBooksForExchange(closureData.exchange);
                applyNotableOrders();
            };

            $scope.model.orderBooks.isLoading = true;
            dataService.loadData(orderBooks, retriever, onSuccess);
        };

        var loadOrderBooks = function () {
            for (var i = 0; i < _exchanges.length; i++) {
                loadOrderBooksForExchange(_exchanges[i]);
            }
        };

        var applyOrderBooksForExchange = function (exchange) {
            var tpDict = {};

            for (var tradingPairIndex = 0; tradingPairIndex < $scope.model.tradingPairs.data.length; tradingPairIndex++) {
                var tradingPair = $scope.model.tradingPairs.data[tradingPairIndex];
                tpDict[tradingPair.symbol.toUpperCase() + "_" + tradingPair.baseSymbol] = tradingPair;
            }

            var orderBooks = _orderBooks[exchange];

            for (var orderBookIndex = 0; orderBookIndex < orderBooks.data.length; orderBookIndex++) {
                var orderBook = orderBooks.data[orderBookIndex];
                var mnemonic = orderBook.symbol.toUpperCase() + "_" + orderBook.baseSymbol.toUpperCase();

                var matchingTradingPair = tpDict[mnemonic];
                if (!matchingTradingPair) { continue; }

                if (matchingTradingPair.orderBooks === undefined || matchingTradingPair.orderBooks === null) {
                    matchingTradingPair.orderBooks = {};
                }

                var exchangeName = exchangeDictionary[exchange].displayName;

                matchingTradingPair.orderBooks[exchange] = { isLoading: false, data: _.cloneDeep(orderBook) };
                matchingTradingPair.orderBooks[exchange].exchangeId = exchange;
                matchingTradingPair.orderBooks[exchange].exchangeName = exchangeName;
            }
        };

        var applyNotableOrders = function () {
            for (var tradingPairIndex = 0; tradingPairIndex < $scope.model.tradingPairs.data.length; tradingPairIndex++) {
                var tradingPair = $scope.model.tradingPairs.data[tradingPairIndex];
                applyNotableOrdersForTradingPair(tradingPair);
            }
        };

        var applyNotableOrdersForTradingPair = function (tradingPair) {
            try {
                applyNotableOrdersForTradingPairInner(tradingPair);
            } catch (ex) {
                alertService.onError(ex);
            }
        };

        var applyNotableOrdersForTradingPairInner = function (tradingPair) {
            var symbol = tradingPair.symbol;
            var baseSymbol = tradingPair.baseSymbol;
            var mnemonic = symbol.toUpperCase() + "_" + baseSymbol.toUpperCase();

            var notableOrders = [];

            var exchangeKeys = Object.keys(_orderBooks);
            for (var exchangeKeyIndex = 0; exchangeKeyIndex < exchangeKeys.length; exchangeKeyIndex++) {
                var exchangeKey = exchangeKeys[exchangeKeyIndex];
                var exchangeName = exchangeDictionary[exchangeKey].displayName;
                var orderBooks = _orderBooks[exchangeKey];
                if (!orderBooks || !orderBooks.data) { continue; }

                var orderBookForTradingPair = _.find(orderBooks.data, function (queryOrderBook) {
                    return queryOrderBook
                        && queryOrderBook.symbol.toUpperCase() === symbol.toUpperCase()
                        && queryOrderBook.baseSymbol.toUpperCase() === baseSymbol.toUpperCase();
                });

                if (!orderBookForTradingPair) { continue; }

                var bestAsk = null;
                if (orderBookForTradingPair.asks) {
                    var orderedAsks = _.orderBy(orderBookForTradingPair.asks, 'price');
                    bestAsk = _.cloneDeep(orderedAsks[0]);
                    bestAsk.title = exchangeName + ' Best Ask';
                    bestAsk.party = exchangeName;
                    bestAsk.orderType = 2;
                    bestAsk.orderTypeText = 'Ask';
                    notableOrders.push(bestAsk);
                }

                var bestBid = null;
                if (orderBookForTradingPair.bids) {
                    var orderedBids = _.orderBy(orderBookForTradingPair.bids, 'price', 'desc');
                    bestBid = _.cloneDeep(orderedBids[0]);
                    bestBid.title = exchangeName + ' Best Bid';
                    bestBid.party = exchangeName;
                    bestBid.orderType = 1;
                    bestBid.orderTypeText = 'Bid';
                    notableOrders.push(bestBid);
                }
            }

            var orderBookForThisExchange = _.find(_orderBooks[_exchange].data, function (queryOrderBook) {
                return queryOrderBook
                    && queryOrderBook.symbol.toUpperCase() === symbol.toUpperCase()
                    && queryOrderBook.baseSymbol.toUpperCase() === baseSymbol.toUpperCase();
            });

            var exchangeKeysForOtherExchanges = _.filter(exchangeKeys, function (queryExchangeKey) {
                return queryExchangeKey.toUpperCase() !== _exchange.toUpperCase();
            });

            var orderBookGroupsForOtherExchanges = _.map(exchangeKeysForOtherExchanges, function (queryExchangeKey) {
                return _orderBooks[queryExchangeKey];
            });

            var orderBooksForOtherExchanges = _.map(orderBookGroupsForOtherExchanges, function (queryOrderBookGroup) {
                return _.find(queryOrderBookGroup.data, function (queryOrderBook) {
                    return queryOrderBook
                        && queryOrderBook.symbol.toUpperCase() === symbol.toUpperCase()
                        && queryOrderBook.baseSymbol.toUpperCase() === baseSymbol.toUpperCase();
                });
            });

            var statuses = [];
            var openOrderGroup = _openOrders.data[mnemonic];
            statuses.push({ disposition: "neutral", text: "Testing" });            

            if (_orderBooks && _orderBooks[_exchange] && _orderBooks[_exchange].data && _orderBooks[_exchange].data.length > 0) {
                var primaryOrderBook = _.find(_orderBooks[_exchange].data,
                    function (queryOrderBook) {
                        return queryOrderBook.symbol.toUpperCase() === symbol.toUpperCase()
                            && queryOrderBook.baseSymbol.toUpperCase() === baseSymbol.toUpperCase()
                    });

                if (primaryOrderBook.asks && primaryOrderBook.asks.length > 0) {
                    var bestPrimaryAsk = _.orderBy(primaryOrderBook.asks, 'price', 'asc')[0];

                    statuses.push({ disposition: "neutral", text: "Best Primary Ask: " + JSON.stringify(bestPrimaryAsk) });
                }

                if (primaryOrderBook.bids && primaryOrderBook.bids.length > 0) {
                    var bestPrimaryBid = _.orderBy(primaryOrderBook.bids, 'price', 'desc')[0];

                    statuses.push({ disposition: "neutral", text: "Best Primary Bid: " + JSON.stringify(bestPrimaryBid) });
                }

                try {
                    if (exchangeKeysForOtherExchanges && exchangeKeysForOtherExchanges.length > 0) {
                        var compKey = exchangeKeysForOtherExchanges[0];
                        var compOrderBooksForSecondaryExchange = _orderBooks[compKey];
                        console.log(compOrderBooksForSecondaryExchange);

                        if (compOrderBooksForSecondaryExchange
                            && compOrderBooksForSecondaryExchange.data
                            && compOrderBooksForSecondaryExchange.data.length > 0) {
                            var compOrderBook = _.find(compOrderBooksForSecondaryExchange.data, function (queryOrderBook) {
                                return queryOrderBook.symbol.toUpperCase() === symbol.toUpperCase()
                                    && queryOrderBook.baseSymbol.toUpperCase() === baseSymbol.toUpperCase();
                            });

                            console.log(compOrderBook);

                            if (compOrderBook) {
                                if (compOrderBook.bids && compOrderBook.bids.length) {
                                    var bestCompBid = _.orderBy(compOrderBook.bids, 'price', 'desc')[0];
                                    statuses.push({ disposition: "neutral", text: "Best Comp Bid: " + JSON.stringify(bestCompBid) });

                                    if (bestCompBid && bestPrimaryBid) {
                                        var bidGap = bestPrimaryBid.price - bestCompBid.price;
                                    }
                                }

                                if (compOrderBook.asks && compOrderBook.asks.length) {
                                    var bestCompAsk = _.orderBy(compOrderBook.asks, 'price', 'asc')[0];
                                    statuses.push({ disposition: "neutral", text: "Best Comp Ask: " + JSON.stringify(bestCompAsk) });
                                }
                            }
                        }
                    }
                } catch (ex) {
                    console.log(ex);
                    alertService.onError(ex);
                }
            }

            if (openOrderGroup && openOrderGroup.openOrders) {
                var openOrders = openOrderGroup.openOrders;
                for (var openOrderIndex = 0; openOrderIndex < openOrders.length; openOrderIndex++) {
                    var openOrder = openOrders[openOrderIndex];
                    var clone = _.cloneDeep(openOrder);
                    clone.title = "My " + clone.orderTypeText;
                    clone.party = "Me";
                    notableOrders.push(clone);
                    
                    if (clone.orderType === 1) {
                        if (orderBookForThisExchange.bids && orderBookForThisExchange.bids.length > 0) {
                            var bestBidOnThisExchange = _.orderBy(orderBookForThisExchange.bids, 'price', 'desc')[0];
                            if (clone.price >= bestBidOnThisExchange.price) {
                                if (clone.price === bestBidOnThisExchange.price && clone.quantity === bestBidOnThisExchange.quantity) {
                                    statuses.push({ disposition: 'good', text: "You have the highest bid on " + _exchangeName + "!" });
                                } else {
                                    statuses.push({ disposition: 'neutral', text: "You are tied for the highest bid on " + _exchangeName + "." });
                                }
                            } else {
                                statuses.push({ disposition: 'bad', text: "Your bid on " + _exchangeName + " is losing!" });
                            }
                        }

                        for (var i = 0; i < orderBooksForOtherExchanges.length; i++) {
                            var orderBookForOtherExchange = orderBooksForOtherExchanges[i];
                            if (orderBookForOtherExchange.bids && orderBookForOtherExchange.bids.length > 0) {
                              var bestBidForOtherExchange = _.orderBy(orderBookForOtherExchange.bids, 'price', 'desc')[0];
                              if (bestBidForOtherExchange.price < clone.price) {
                                statuses.push({ disposition: 'bad', text: "Your bid on " + _exchangeName + " is too high! Binance's bid is only " + bestBidForOtherExchange.price + "."});
                              }
                            }

                            console.log('asdf');
                        }

                    } else if (clone.orderType === 2) {
                        if (orderBookForThisExchange.bids && orderBookForThisExchange.bids.length > 0) {
                            var bestAskOnThisExchange = _.orderBy(orderBookForThisExchange.asks, 'price', 'asc')[0];
                            if (clone.price === bestAskOnThisExchange.price) {
                                statuses.push({ disposition: 'good', text: "You have the best ask on " + _exchangeName + "!" });
                            } else {
                                statuses.push({ disposition: 'bad', text: "Your ask on " + _exchangeName + " is losing!" });
                            }
                        }
                    }

                    console.log(openOrder);
                }
            }

            tradingPair.notableOrders = _.orderBy(notableOrders, 'price', 'desc');
            tradingPair.statuses = statuses;
        };

        var init = function () {
            loadTradingPairs();            
        };

        init();
    });