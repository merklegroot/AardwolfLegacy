angular.module('main')
    .controller('exchangeHistoryDetailController', function (
        $scope,
        $stateParams,
        dataService,
        alertService,
        exchangeService,
        historyService,
        valuationService,
        exchangeDictionary) {

        // var isForTaxes = true;
        var isForTaxes = false;

        var exchangeId = $stateParams.exchange;
        var exchangeDisplayName = exchangeDictionary[exchangeId].displayName;

        var symbols = [
            'USD', 'USDT', 'TUSD', 'GUSD', 'USDC',            
            'ETH', 'BTC', 'CHX',
            'HGT', 'XLM', 'NPXS', 'IND',
            'COSS', 'TRX',
            'DASH', 'BCHABC',
            'FXT', 'BNT', 'OPQ',
            'WISH', 'KNC', 'WAVES',
            'ARK', 'SUB', 'POE', 'LINK',
            'DAT', 'ZEC', 'ZEN', 'GAT',
            'PRL', 'MRK',
            'OMG', 'WTC', 'ENJ', 'LTC',
            'NEO', 'BLZ', 'BCH', 'FYN',
            'CVC', 'REQ', 'LSK', 'ICN',
            'MLN', 'EOS', 'ZEC', 'XRP',
            'ICN', 'XMR', 'XEM', 'PAY',
            'PIX', 'CAN', 'NOX', 'LA',            
            'UFR', 'SNM', 'CS', 'LALA',
            'STX'];

        if (isForTaxes) {
            symbols = [
                'USD', 'ETH', 'BTC', 'LTC', 'BCH',
                'XRP', 'ZEC'
            ];
        }

        var history = {};
        var valuations = {};
        var _exchanges = {};

        $scope.model = {
            exchangeId: exchangeId,
            exchangeDisplayName: exchangeDisplayName,
            history: {},
            valuations: {}
        };

        $scope.onForceRefreshClicked = function () {
            alertService.info('Refreshing...');
            var onSuccess = function () {
                alertService.info('Done.');
            };

            loadHistory('ForceRefresh', onSuccess);
        };

        $scope.onRefreshValuationClicked = function (symbol) {
            if (symbol === undefined || symbol === null) { throw "Argument \"symbol\" must not be undefind or null."; }
            alertService.info("Refreshing " + symbol + " value.");

            var dataMatch = valuations[symbol];
            if (dataMatch) {dataMatch.isLoading = true; }

            var scopeMatch = $scope.model.valuations[symbol.toUpperCase()];
            if (scopeMatch) { scopeMatch.isLoading = true; }

            var closureData = {
                symbol: symbol
            };

            var onCompleted = function () {
                var scopeMatch = $scope.model.valuations[closureData.symbol.toUpperCase()];
                if (scopeMatch) { scopeMatch.isLoading = false; }
            };

            var onSuccess = function (response) {
                var dataMatch = valuations[closureData.symbol.toUpperCase()];
                if (dataMatch) {
                    dataMatch.isLoading = false;
                    if (response.data && response.data.usdValue && response.data.asOfUtc) {
                        dataMatch.data.usdValue = response.data.usdValue;
                        dataMatch.data.asOfUtc = response.data.asOfUtc;
                    }
                }

                var scopeMatch = $scope.model.valuations[closureData.symbol.toUpperCase()];
                if (scopeMatch && response.data && response.data.usdValue && response.data.asOfUtc) {
                    scopeMatch.data.usdValue = response.data.usdValue;
                    scopeMatch.data.asOfUtc = response.data.asOfUtc;
                }

                if ($scope.model.history.data) {
                    applyValuations($scope.model.history.data);
                }

                alertService.success("Loaded.");
                onCompleted();
            };

            var onFailure = function (err) {
                onCompleted();
            };

            var retriever = function () { return valuationService.getUsdValueV2(symbol, "ForceRefresh"); };
            var valuation = {};
            
            dataService.loadData(valuation, retriever, onSuccess, onFailure);
        };

        var applyValuations = function (historyItems) {
            var effectiveHistoryItems = historyItems !== undefined && historyItems !== null
                ? historyItems
                : $scope.model.history.data.historyItems;

            for (var i = 0; i < effectiveHistoryItems.length; i++) {
                try {
                    var historyItem = effectiveHistoryItems[i];
                    //if (historyItem && historyItem.tradeType && (historyItem.tradeType.toUpperCase() === 'DEPOSIT' || historyItem.tradeType.toUpperCase() === 'WITHDRAW')
                    //    && historyItem.symbol === 'ETH' && valuations && valuations[historyItem.symbol.toUpperCase()] && valuations[historyItem.symbol.toUpperCase()].data) {
                    //  console.log('here');
                    //}

                    if (historyItem === null
                        || 
                        (historyItem.symbol === undefined || historyItem.symbol === null || historyItem.symbol.length === 0)
                        && (historyItem.baseSymbol === undefined || historyItem.baseSymbol === null || historyItem.baseSymbol.length === 0)) {
                        continue;
                    }

                    var symbolValuation = (historyItem.symbol !== undefined && historyItem.symbol !== null && historyItem.symbol.length !== 0)
                        ? valuations[historyItem.symbol.toUpperCase()]
                        : null;

                    var symbolValue = symbolValuation && symbolValuation.data
                        ? symbolValuation.data.usdValue
                        : null;

                    var baseSymbolValuation = (historyItem.baseSymbol !== undefined && historyItem.baseSymbol !== null && historyItem.baseSymbol.length !== 0)
                        ? valuations[historyItem.baseSymbol.toUpperCase()]
                        : null;

                    var baseSymbolValue = baseSymbolValuation && baseSymbolValuation.data
                        ? baseSymbolValuation.data.usdValue
                        : null;

                    var deltaSymbol = null;
                    var deltaBaseSymbol = null;

                    if (historyItem.tradeType.toUpperCase() === 'BUY'
                        || historyItem.tradeType.toUpperCase() === 'SELL') {
                        deltaSymbol = symbolValue !== null && historyItem.quantity !== undefined && historyItem.quantity !== null
                            ? historyItem.quantity * symbolValue
                            : null;

                        deltaBaseSymbol = baseSymbolValue !== null && historyItem.quantity !== undefined && historyItem.quantity !== null
                            ? historyItem.price * historyItem.quantity * baseSymbolValue
                            : null;
                    }
                    else if (historyItem.tradeType.toUpperCase() === 'DEPOSIT' ||
                        historyItem.tradeType.toUpperCase() === 'WITHDRAW') {
                        deltaSymbol = historyItem.feeQuantity;
                    }

                    if (historyItem.tradeType.toUpperCase() === 'BUY') {
                        historyItem.netUsdChange = deltaSymbol - deltaBaseSymbol;
                    } else if (historyItem.tradeType.toUpperCase() === 'SELL') {
                        historyItem.netUsdChange = deltaBaseSymbol - deltaSymbol;
                    } else if (historyItem.tradeType.toUpperCase() === 'DEPOSIT') {
                        // historyItem.netUsdChange = deltaSymbol;
                        historyItem.netUsdChange = 0;
                    } else if (historyItem.tradeType.toUpperCase() === 'WITHDRAW') {
                        historyItem.netUsdChange = -(historyItem.feeQuantity * symbolValue);
                        /* if (deltaSymbol !== undefined && deltaSymbol !== null) {
                            historyItem.netUsdChange = deltaSymbol !== undefined && deltaSymbol !== null
                              ? -deltaSymbol
                                : null;
                        }*/
                    }
                }
                catch (ex) {
                    console.log(ex);
                }
            }
        };

        var loadHistory = function (cachePolicy, onSuccess) {
            var retriever = function () {
                return historyService.getExchangeHistory(exchangeId, cachePolicy);
            };

            var aggregateOnSuccess = function () {

                for (var i = 0; i < history.data.historyItems.length; i++) {
                    var historyItem = history.data.historyItems[i];
                    try {
                        if (historyItem.timeStampUtc) {
                            var year = new Date(historyItem.timeStampUtc).getFullYear();
                            historyItem.year = year;
                        }
                    } catch(ex) {
                      console.log(ex);
                    }
                }

                var clonedHistory = _.cloneDeep(history.data.historyItems);
                var enriched = _.map(clonedHistory, function (item) {
                    if (item && item.exchange && !item.exchangeDisplayName) {
                        var dictionaryItem = exchangeDictionary[item.exchange.toLowerCase()];
                        item.exchangeDisplayName = dictionaryItem ? dictionaryItem.displayName : item.exchange;
                    }

                    return item;
                });

                var sortedHistory = _.orderBy(enriched, 'timeStampUtc', 'desc');

                if (isForTaxes) {
                    sortedHistory = _.filter(sortedHistory, function (item) {
                        var historyDate = new Date(item.timeStampUtc);
                        var compDate = new Date('2018-01-01T14:12:44.844Z');
                        return historyDate <= compDate;
                    });
                }

                applyValuations(sortedHistory);

                $scope.model.history.data = {
                    asOfUtc: history.data.asOfUtc,
                    historyItems: sortedHistory
                };                

                if (onSuccess) { onSuccess(); }
            };

            $scope.model.history.isLoading = true;

            dataService.loadData(history, retriever, aggregateOnSuccess);
        };        

        var loadValuationByIndex = function (index) {            
            var symbol = symbols[index];                
            if (!valuations[symbol]) {
                valuations[symbol] = { symbol: symbol, isLoading: true };
            }

            var valuation = valuations[symbol];

            var closureData = { index: index, symbol: symbol, valuation: valuation };
            var onCompleted = function () {
                $scope.model.valuations[closureData.symbol] = _.cloneDeep(closureData.valuation);
                $scope.model.valuations[closureData.symbol].symbol = closureData.symbol;

                if ($scope.model.history.data
                    && $scope.model.history.data.historyItems) {
                    applyValuations($scope.model.history.data.historyItems);
                }

                if (closureData.index + 1 < symbols.length) {
                    loadValuationByIndex(closureData.index + 1);
                }                
            };

            var retriever = function () {
                // getHistoricUsdValueV2
                if (isForTaxes) { return valuationService.getHistoricUsdValueV2(symbol); }
                return valuationService.getUsdValueV2(symbol);
            };

            dataService.loadData(valuation, retriever, onCompleted, onCompleted);
        };

        var loadExchanges = function () {
            var retriever = function () { return exchangeService.getExchanges(); };

            var closureData = { exchanges: _exchanges };
            var onSuccess = function () {

                $scope.model.exchanges = _.cloneDeep(closureData.exchanges)
            };

            var onError = function (err) {
                alertService.onError(err);
            };
            
            dataService.loadData(_exchanges, retriever, onSuccess);
        };
            
        var init = function () {
            loadExchanges();
            loadValuationByIndex(0);
            loadHistory();
        };

        init();
    });