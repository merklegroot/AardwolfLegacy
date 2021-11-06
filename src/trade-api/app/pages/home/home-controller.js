(function () {
    var controller = function (
        $scope, $http, $timeout,
        alertService, dataService, exchangeDictionary) {

        var profitThreshold = 0.75;

        var _controllerData = {
            compDefs: {},
            comps: {},
            expansions: {}
        };

        var getKey = function (compDef) {
            return compDef.symbol.toUpperCase() + "_"
                + compDef.baseSymbol.toUpperCase() + "_"
                + compDef.exchanges[0].toUpperCase() + "_"
                + compDef.exchanges[1].toUpperCase();
        };

        $scope.model = {
            comps: [],
            totalCompDefs: null
        };

        $scope.onLoadCompDataClicked = function (compDef) {
            loadCompData(compDef, "OnlyUseCacheUnlessEmpty");
        };

        $scope.onRefreshCompClicked = function (compData) {
            compData.isRefreshing = true;
            alertService.info("Refreshing...");
            loadCompData(compData.compDef, "ForceRefresh");
        };

        $scope.onToggleExpandClicked = function (comp) {
            var isCurrentlyExpanded = comp.isExpanded;
            var compKey = getKey(comp.compDef);

            _controllerData.expansions[compKey] = !isCurrentlyExpanded;
            comp.isExpanded = !isCurrentlyExpanded;
        };

        var applyAllCompData = function () {
            var compKeys = Object.keys(_controllerData.comps);

            var compArray = [];

            for (var i = 0; i < compKeys.length; i++) {
                var compKey = compKeys[i];
                var compData = _.cloneDeep(_controllerData.comps[compKey]);
                var isExpanded = _controllerData.expansions[compKey];
                if (isExpanded !== undefined && isExpanded !== null) {
                    compData.isExpanded = isExpanded;
                }

                if (compData.profitPercentage !== undefined
                    && compData.profitPercentage !== null
                    && compData.profitPercentage >= profitThreshold) {
                    compArray.push(compData);
                }
            }

            var sorted = _.orderBy(compArray, ['profitPercentage'], ['desc']);
            $scope.model.comps = sorted;
        };

        var loadCompData = function (compDef, cachePolicy, callback) {
            var closureData = {
                compDef: compDef,
                results: {},
                exchangeDictionary: exchangeDictionary,
                controllerData: _controllerData,
                scopeComps: $scope.model.comps
            };

            var retriever = function () {
                var serviceModel = {
                    symbol: closureData.compDef.symbol,
                    baseSymbol: closureData.compDef.baseSymbol,
                    exchangeA: closureData.compDef.exchanges[0],
                    exchangeB: closureData.compDef.exchanges[1],
                    cachePolicy: cachePolicy
                };

                return $http.post("api/get-orders", serviceModel);
            };

            var onCompleted = function () {
                if (callback) { callback(); }
            };

            var onFailure = function () {
                onCompleted();
            };

            var onSuccess = function () {
                var compKey = getKey(closureData.compDef);
                closureData.controllerData.comps[compKey] = _.cloneDeep(closureData.results.data);
                closureData.controllerData.comps[compKey].compDef = _.cloneDeep(closureData.compDef);

                var profitPercentage = null;
                if (closureData.results.data.exchanges[0].profitPercentage !== undefined
                    && closureData.results.data.exchanges[0].profitPercentage !== null
                    && closureData.results.data.exchanges[1].profitPercentage !== undefined
                    && closureData.results.data.exchanges[1].profitPercentage !== null) {
                    profitPercentage = closureData.results.data.exchanges[0].profitPercentage > closureData.results.data.exchanges[1].profitPercentage
                        ? closureData.results.data.exchanges[0].profitPercentage
                        : closureData.results.data.exchanges[1].profitPercentage;
                }

                closureData.controllerData.comps[compKey].profitPercentage = profitPercentage;

                var compComparator = function (queryComp) {
                    var scopeCompKey = getKey(queryComp.compDef);
                    return scopeCompKey === compKey;
                };

                var matchIndex = _.findIndex(closureData.scopeComps, compComparator);

                var match = matchIndex >= 0 ? closureData.scopeComps[matchIndex] : null;

                var clone = _.cloneDeep(closureData.controllerData.comps[compKey]);                
                if (match) {
                    if (match.isRefreshing) {
                        match.isRefreshing = false;
                        alertService.success("Refreshed!");                        
                    }
                    if (clone.profitPercentage >= profitThreshold) {
                        var cloneKeys = Object.keys(clone);
                        for (var i = 0; i < cloneKeys.length; i++) {
                            var cloneKey = cloneKeys[i];
                            match[cloneKey] = clone[cloneKey];
                        }
                    } else {
                        // remove 1 item from the array at index "matchIndex"
                        array.splice(closureData.scopeComps, 1, matchIndex);
                    }
                } else {
                    if (clone.profitPercentage >= profitThreshold) {
                        closureData.scopeComps.push(clone);
                    }
                }

                onCompleted();
            };

            dataService.loadData(closureData.results, retriever, onSuccess, onFailure);
        };

        var loadCompDataByIndex = function (index, cachePolicy) {
            $scope.isLoadingCompData = true;
            var compDef = _controllerData.compDefs[index];

            var closureData = { index: index, controllerData: _controllerData };
            var callback = function () {
                var timeoutMs = cachePolicy === "OnlyUseCacheUnlessEmpty" ? 25 : 250;
                if (closureData.index + 1 < closureData.controllerData.compDefs.length
                    // && closureData.index < 5
                ) {
                    $timeout(function () { loadCompDataByIndex(closureData.index + 1, cachePolicy); }, timeoutMs);
                } else {
                    $scope.model.status = "Iteration complete.";
                    alertService.success("Done!");
                    applyAllCompData();
                    $scope.isLoadingCompData = false;
                    // $timeout(function () { loadCompDataByIndex(0, "AllowCache"); }, timeoutMs);
                }
            };

            $scope.model.status = "Loading " + (index + 1) + " of " + _controllerData.compDefs.length + ". (" + cachePolicy + ")";
            loadCompData(compDef, cachePolicy, callback);
        };

        var loadCompDefs = function (callback) {
            var closureData = {
                controllerData: _controllerData,
                compDefs: {}
            };

            var onCompleted = function () {
                if (callback) { callback(); }
            };

            var onFailure = function () {
                onCompleted();
            };

            var onSuccess = function () {
                closureData.controllerData.compDefs = _.cloneDeep(closureData.compDefs.data);
                $scope.model.compDefs = _.cloneDeep(closureData.compDefs);
                $scope.model.totalCompDefs = closureData.controllerData.compDefs.length;
                onCompleted();
            };

            var retriever = function () {
                return $http.post("api/coin/get-comps");
            };

            dataService.loadData(closureData.compDefs, retriever, onSuccess, onFailure);
        };

        var afterCompDefsLoaded = function () {
            // var cachePolicy = "OnlyUseCacheUnlessEmpty";
            var cachePolicy = "AllowCache";
            loadCompDataByIndex(0, cachePolicy);
        };

        var init = function () {
            loadCompDefs(afterCompDefsLoaded);
        };

        $scope.shouldHideLetsDoThis = false;
        $scope.onLetsDoThisClicked = function () {
            $scope.shouldHideLetsDoThis = true;
            init();
        };

        $scope.onRefreshOldClicked = function () {            
            loadCompDataByIndex(0, "AllowCache");
        };

    };

    angular.module('main').controller('homeController', controller);
}
)();
