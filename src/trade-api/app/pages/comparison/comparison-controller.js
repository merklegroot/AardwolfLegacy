angular.module('main')
    .controller('comparisonController', function (
        $scope,
        $stateParams,
        orderBookService,
        exchangeDictionary,    
        exchangeService,
        commodityService,
        dataService,
        alertService,
        $timeout) {

        var exchangeA = $stateParams.exchangeA;
        var exchangeB = $stateParams.exchangeB;
        var symbol = $stateParams.symbol;
        var baseSymbol = $stateParams.baseSymbol;

        $scope.exchangeA = exchangeA;
        $scope.exchangeB = exchangeB;
        $scope.symbol = symbol;
        $scope.baseSymbol = baseSymbol;

        var maxRows = 3;
        var compExchanges = [
            'hitbtc', 
            'bitz',
            'binance',
            'coss',
            'qryptos',
            'kucoin',
            'livecoin'
        ];

        if (exchangeA && exchangeB) {
            compExchanges = [exchangeA, exchangeB];
        }

        $scope.compExchanges = compExchanges;
        $scope.exchangeDictionary = exchangeDictionary;

        $scope.comps = [];

        var exchangeCommodityDictionary = {};
        var exchangeTradingPairDictionary = {};
        var bookDictionary = {};
        $scope.bookDictionary = bookDictionary;

        var getExchangeCommodityKey = function (exchange, symbol) {
            return exchange.toUpperCase() + "_" + symbol.toUpperCase();
        };

        var getOrderBookKey = function (exchange, symbol, baseSymbol) {
            return exchange.toUpperCase() + "_" + symbol.toUpperCase() + "_" + baseSymbol.toUpperCase();
        };

        $scope.onLeftRefreshClicked = function (comp) {
            var key = getOrderBookKey(comp.exchanges[0], comp.symbol, comp.baseSymbol);
            loadBook(bookDictionary[key], 'forceRefresh');
        };

        $scope.onRightRefreshClicked = function (comp) {
            var key = getOrderBookKey(comp.exchanges[1], comp.symbol, comp.baseSymbol);
            loadBook(bookDictionary[key], 'forceRefresh');
        };

        var onLoadBookSuccess = function (bookInfo) {
            updateBookIndexes(bookInfo);
        };

        var updateBookIndexes = function (bookInfo) {
            if (!bookInfo.book || !bookInfo.book.data || !bookInfo.book.data.bids) { return; }
            bookInfo.book.data.bidIndexes = [];
            var i;
            for (i = 0; i < maxRows; i++) {
                bookInfo.book.data.bidIndexes.push(i);
            }

            bookInfo.book.data.reverseBidIndexes = [];
            for (i = maxRows - 1; i >= 0; i--) {
                bookInfo.book.data.reverseBidIndexes.push(i);
            }

            bookInfo.book.data.askIndexes = [];
            for (i = 0; i < maxRows; i++) {
                bookInfo.book.data.askIndexes.push(i);
            }

            bookInfo.book.data.reverseAskIndexes = [];
            for (i = maxRows - 1; i >= 0; i--) {
                bookInfo.book.data.reverseAskIndexes.push(i);
            }                
        };

        var loadBook = function (bookInfo, cachePolicy, onSuccess, onFailure) {
            if (bookInfo === undefined || bookInfo === null) {
                throw 'Argument "bookInfo" must not be undefined or null.';
            }

            var retriever = function () {
                return orderBookService.getOrderBook(
                    bookInfo.symbol,
                    bookInfo.baseSymbol,
                    bookInfo.exchange,
                    null,
                    cachePolicy);
            };

            var internalOnSuccess = function () {
                onLoadBookSuccess(bookInfo);
                onSuccess();
            };

            dataService.loadData(bookInfo.book, retriever, internalOnSuccess, onFailure);
        };

        var loadExchangeCommodity = function (exchangeCommodityInfo, onSuccess, onFailure) {
            var retriever = function () {
                return commodityService.getExchangeCommodityBySymbolExcludeDepositAddress(
                    exchangeCommodityInfo.exchange,
                    exchangeCommodityInfo.symbol,
                    false);
            };

            dataService.loadData(exchangeCommodityInfo.commodity, retriever, onSuccess, onFailure);
        };

        // TODO: need to continue on when an order book fails.
        var initCompDef = function (comp, onCompleted) {
            try {
                var hasBeenPushed = false;

                var onFailure = function () {
                    console.log("initCompDef.onFailure");
                    alertService.info("initCompDef.onFailure");

                    onCompleted();
                };

                var onSuccess = function () {
                    console.log('success');
                    try {
                        var anyNotReady = false;
                        for (var i = 0; i < 2; i++) {
                            var exchangeCommodityKey = getExchangeCommodityKey(comp.exchanges[i], comp.symbol);
                            var exchangeCommodityInfo = exchangeCommodityDictionary[exchangeCommodityKey];
                            if (exchangeCommodityInfo === undefined
                                || exchangeCommodityInfo === null
                                || exchangeCommodityInfo.commodity === undefined
                                || exchangeCommodityInfo.commodity === null
                                || exchangeCommodityInfo.commodity.isLoading !== false) {
                                anyNotReady = true;
                                break;
                            }

                            var orderBookKey = getOrderBookKey(comp.exchanges[i], comp.symbol, comp.baseSymbol);
                            var bookInfo = bookDictionary[orderBookKey];
                            if (bookInfo === undefined
                                || bookInfo === null
                                || bookInfo.book === undefined
                                || bookInfo.book === null
                                || bookInfo.book.isLoading !== false) {
                                anyNotReady = true;
                                break;
                            }
                        }

                        if (!anyNotReady) {
                            comp.exchangeCommodities = [
                                exchangeCommodityDictionary[getExchangeCommodityKey(comp.exchanges[0], comp.symbol)].commodity,
                                exchangeCommodityDictionary[getExchangeCommodityKey(comp.exchanges[1], comp.symbol)].commodity
                            ];

                            comp.books = [
                                bookDictionary[getOrderBookKey(comp.exchanges[0], comp.symbol, comp.baseSymbol)].book,
                                bookDictionary[getOrderBookKey(comp.exchanges[1], comp.symbol, comp.baseSymbol)].book
                            ];

                            if (!hasBeenPushed) {
                                $scope.comps.push(comp);
                                hasBeenPushed = true;

                                console.log('everything is ready!');

                                if (onCompleted) {
                                    onCompleted();
                                }
                            }
                        }
                    } catch (ex) {
                        console.log(ex);
                    }
                };

                for (var i = 0; i < 2; i++) {
                    var exchangeCommodityKey = getExchangeCommodityKey(comp.exchanges[i], comp.symbol);
                    var exchangeCommodityInfo = exchangeCommodityDictionary[exchangeCommodityKey];

                    if (!exchangeCommodityInfo) {
                        exchangeCommodityInfo = {
                            exchange: comp.exchanges[i],
                            symbol: comp.symbol,
                            commodity: {}
                        };

                        exchangeCommodityDictionary[exchangeCommodityKey] = exchangeCommodityInfo;

                        console.log('Loading exchange commodity ' + exchangeCommodityInfo.exchange + ' - ' + exchangeCommodityInfo.symbol);
                        loadExchangeCommodity(exchangeCommodityInfo, onSuccess, onFailure);
                    } else {
                        onSuccess();
                    }

                    var orderBookKey = getOrderBookKey(comp.exchanges[i], comp.symbol, comp.baseSymbol);

                    var bookInfo = bookDictionary[orderBookKey];
                    if (bookInfo === undefined || bookInfo === null) {
                        bookInfo = {
                            exchange: comp.exchanges[i],
                            symbol: comp.symbol,
                            baseSymbol: comp.baseSymbol,
                            book: {}
                        };

                        bookDictionary[orderBookKey] = bookInfo;

                        console.log('Loading order book ' + bookInfo.exchange + ' - ' + bookInfo.symbol + ' - ' + bookInfo.baseSymbol);
                        var cachePolicy = 'AllowCache';
                        loadBook(bookInfo, cachePolicy, onSuccess, onFailure);
                    } else {
                        onSuccess();
                    }
                }
            } catch (exception) {
                console.log(exception);
                alertService.onError("initCompDef exception. " + exception);
            }
        };

        var compDefs = [];

        $scope.compDefs = _.cloneDeep(compDefs);
        for(var i = 0; i < $scope.compDefs.length; i++) {
            $scope.compDefs[i].index = i;
        }

        var initCompDefByIndex = function (i) {
            var compDef = compDefs[i];
            var onCompleted = function () {
                console.log("completed");
                if (i + 1 < compDefs.length) {
                    $timeout(function () {
                        initCompDefByIndex(i + 1);
                    }, 10);
                }
            };

            initCompDef(compDef, onCompleted);
        };

        var getAgeInMinutes = function (bookInfo) {
            return (new Date() - new Date(bookInfo.book.data.asOf)) / 1000 / 60;
        };

        $scope.refreshIndex = null;

        var refreshIfOld = function (index) {
            $scope.refreshIndex = index;
            var booksCompleted = 0;
            var onBookCompleted = function () {
                booksCompleted++;

                if (booksCompleted === 2) {
                    if (index + 1 < compDefs.length) {
                        refreshIfOld(index + 1);
                    } else {
                        $scope.refreshIndex = null;
                    }
                }
            };

            var onFailure = function () {
                onBookCompleted();
            };

            var onSuccess = function () {
                onBookCompleted();
            };

            var comp = compDefs[index];

            for (var i = 0; i < 2; i++) {
                var key = getOrderBookKey(comp.exchanges[i], comp.symbol, comp.baseSymbol);
                if (getAgeInMinutes(bookDictionary[key]) > 30) {
                    loadBook(bookDictionary[key], 'allowCache', onSuccess, onFailure);
                } else {
                    $timeout(onBookCompleted, 1);
                }
            }
        };

        $scope.onRefreshTheOldStuffClicked = function () {
            refreshIfOld(0);
        };

        var onAllExchangeTradingPairsLoaded = function () {
            alertService.info("All trading pairs loaded!");

            var matches = [];
            for (var i = 0; i < compExchanges.length; i++) {
                var exchangeA = compExchanges[i];
                var containerA = exchangeTradingPairDictionary[exchangeA];

                for (var j = i + 1; j < compExchanges.length; j++) {
                    var exchangeB = compExchanges[j];
                    if (exchangeA.toUpperCase() === 'hitbtc'.toUpperCase() && exchangeB.toUpperCase() === 'binance'.toUpperCase()) { continue; }
                    if (exchangeB.toUpperCase() === 'hitbtc'.toUpperCase() && exchangeA.toUpperCase() === 'binance'.toUpperCase()) { continue; }
                    var containerB = exchangeTradingPairDictionary[exchangeB];
                    console.log("Comparing " + exchangeA + " to " + exchangeB + ".");

                    for (k = 0; k < containerA.data.length; k++) {
                        var pairA = containerA.data[k];

                        for (var l = 0; l < containerB.data.length; l++) {
                            var pairB = containerB.data[l];
                            if (!doTradingPairsMatch(pairA, pairB)) { continue; }
                            if (symbol && pairA.symbol.toUpperCase() !== symbol.toUpperCase()) { continue; }
                            if (baseSymbol && pairA.baseSymbol.toUpperCase() !== baseSymbol.toUpperCase()) { continue; }
                            matches.push({ exchangeA: exchangeA, exchangeB: exchangeB, symbol: pairA.symbol, baseSymbol: pairA.baseSymbol });
                        }
                    }
                }
            }

            processTradingPairMatches(matches);
        };

        var processTradingPairMatches = function (matches) {
            $scope.matches = matches;

            console.log("There are " + matches.length + " matches.");
            for (var i = 0; i < matches.length; i++) {
                var match = matches[i];
                var compDef = {
                    exchanges: [match.exchangeA, match.exchangeB],
                    symbol: match.symbol,
                    baseSymbol: match.baseSymbol
                };

                compDefs.push(compDef);
            }

            initCompDefByIndex(0);
        };

        var doTradingPairsMatch = function (tradingPairA, tradingPairB) {
            if (tradingPairA.canonicalCommodityId !== undefined 
            && tradingPairA.canonicalCommodityId !== null
            && tradingPairA.canonicalCommodityId.length > 0
            && tradingPairB.canonicalCommodityId !== undefined 
            && tradingPairB.canonicalCommodityId !== null
            && tradingPairB.canonicalCommodityId.length > 0
            && tradingPairA.canonicalCommodityId.toUpperCase() !== tradingPairB.canonicalCommodityId.toUpperCase()) {
                    return false;
            }

            if (tradingPairA.canonicalBaseCommodityId !== undefined 
                && tradingPairA.canonicalBaseCommodityId !== null
                && tradingPairA.canonicalBaseCommodityId.length > 0
                && tradingPairB.canonicalBaseCommodityId !== undefined 
                && tradingPairB.canonicalBaseCommodityId !== null
                && tradingPairB.canonicalBaseCommodityId.length > 0
                && tradingPairA.canonicalBaseCommodityId.toUpperCase() !== tradingPairB.canonicalBaseCommodityId.toUpperCase()) {
                    return false;
            }

            return tradingPairA.symbol.toUpperCase() === tradingPairB.symbol.toUpperCase()
                && tradingPairA.baseSymbol.toUpperCase() === tradingPairB.baseSymbol.toUpperCase();
        };

        var onExchangeTradingPairsLoaded = function () {
            var wereAllLoaded = true;
            for (var i = 0; i < compExchanges.length; i++) {
                var exchange = compExchanges[i];

                if (_.some(exchangesWithLoadedCommodities, function (item) { return item === exchange; })) {
                    console.log('woot!');
                } else {
                    wereAllLoaded = false;
                    break;
                }
                
                var tradingPairContainer = exchangeTradingPairDictionary[exchange];
                if (tradingPairContainer === undefined
                    || tradingPairContainer === null
                    || !tradingPairContainer.data) {
                    wereAllLoaded = false;
                    break;
                }
            }

            if (wereAllLoaded) {
                onAllExchangeTradingPairsLoaded();
            }
        };

        var initCompExchanges = function () {
            for (var i = 0; i < compExchanges.length; i++) {
                var exchange = compExchanges[i];
                initCompExchange(exchange);
            }
        };

        var exchangesWithLoadedCommodities = [];

        var onCommoditiesForExchangeLoaded = function (exchange, commoditiesForExchange) {           
            if (commoditiesForExchange) {
                for (var i = 0; i < commoditiesForExchange.length; i++) {
                    var item = commoditiesForExchange[i];
                    item.isLoading = false;

                    var key = getExchangeCommodityKey(exchange, item.symbol);
                    exchangeCommodityDictionary[key] = {
                        isLoading: false,
                        exchange: exchange,
                        symbol: item.symbol,
                        commodity: item
                    };
                }
            }

            exchangesWithLoadedCommodities.push(exchange);
            onExchangeTradingPairsLoaded();
        };

        var initCompExchange = function (exchange) {
            var tradingPairContainer = exchangeTradingPairDictionary[exchange];
            if (tradingPairContainer === undefined || tradingPairContainer === null) {
                tradingPairContainer = {};
                exchangeTradingPairDictionary[exchange] = tradingPairContainer;

                var tradingPairsRetriever = function () { return exchangeService.getTradingPairsForExchange(exchange, null, 'OnlyUseCacheUnlessEmpty'); };
                dataService.loadData(tradingPairContainer, tradingPairsRetriever, onExchangeTradingPairsLoaded, onExchangeTradingPairsLoaded);
            }

            var commoditiesForExchangeContainer = {};
            var commoditiesRetriever = function () { return exchangeService.getCommoditiesForExchange(exchange); };

            var onCommoditiesSuccess = function (commoditiesResponse) {
                onCommoditiesForExchangeLoaded(exchange, commoditiesResponse.data);
            };

            var onCommoditiesFailure = function () {
                onCommoditiesForExchangeLoaded(exchange, null);
            };

            dataService.loadData(commoditiesForExchangeContainer, commoditiesRetriever, onCommoditiesSuccess, onCommoditiesFailure);
        };
        
        var initCacheableExchange = function (
            exchange,
            onCompleted) {
            var onSuccess = function () {
                for (var i = 0; i < exchangeCache.data.length; i++) {
                    var item = exchangeCache.data[i];
                    var key = getOrderBookKey(exchange, item.symbol, item.baseSymbol);
                    var bookInfo = {
                        exchange: exchange,
                        symbol: item.symbol,
                        baseSymbol: item.baseSymbol,
                        book: {
                            isLoading: false,
                            data: item
                        }
                    };

                    bookDictionary[key] = bookInfo;

                    updateBookIndexes(bookInfo);
                }

                onCompleted();
            };

            var onFailure = function () {
                onCompleted();
            };

            var retriever = function () { return orderBookService.getCachedOrderBooks(exchange); };

            var exchangeCache = {};
            dataService.loadData(exchangeCache, retriever, onSuccess, onFailure);
        };

        var initCacheableExchangeByIndex = function (index) {
            var onCompleted = function () {
                if (index + 1 < compExchanges.length) {
                    initCacheableExchangeByIndex(index + 1);
                } else {
                    initCompExchanges();
                }
            };

            initCacheableExchange(compExchanges[index], onCompleted);
        };

        var init = function () {
            initCacheableExchangeByIndex(0);
        };

        init();

        $scope.debug = exchangeTradingPairDictionary;
});

