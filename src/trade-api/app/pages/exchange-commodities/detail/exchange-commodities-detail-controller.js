angular.module('main')
    .controller("exchangeCommoditiesDetailController", function (
        $scope,
        $stateParams,
        exchangeDictionary,
        dataService,
        alertService,
        commodityService,
        exchangeService) {

        var exchange = $stateParams.exchange;

        $scope.model = {
            exchange: exchange,
            exchangeDisplayName: exchangeDictionary[exchange].displayName,
            commodities: {},
            filteredCommodities: {},
        };

        $scope.onRefreshButtonClicked = function () {
            alertService.info('Refreshing...');
            load(true);
        };

        var updateFilteredData = function () {
            var filterText = $scope.filterText;
            var commodities = $scope.model.commodities;

            if (!commodities) {
                $scope.model.filteredCommodities = {};
                return;
            }

            if (filterText === undefined || filterText === null) {
                $scope.model.filteredCommodities = commodities.data;
                return;
            }

            filterText = filterText.trim().toUpperCase();
            if (filterText.length === 0) {
                $scope.model.filteredCommodities = commodities.data;
                return;
            }

            $scope.model.filteredCommodities = _.filter(commodities.data, function (item) {
                if (item.symbol !== undefined && item.symbol !== null && item.symbol.indexOf(filterText) >= 0) {
                    return true;
                }

                if (item.nativeSymbol !== undefined && item.nativeSymbol !== null && item.nativeSymbol.indexOf(filterText) >= 0) {
                    return true;
                }

                if (item.name !== undefined && item.name !== null && item.name.indexOf(filterText) >= 0) {
                    return true;
                }

                if (item.nativeName !== undefined && item.nativeName !== null && item.nativeName.indexOf(filterText) >= 0) {
                    return true;
                }


                return false;
            });
        };

        var load = function (forceRefresh) {
            dataService.loadData($scope.model.commodities, function () {
                return exchangeService.getCommoditiesForExchange(exchange, forceRefresh);
            }, updateFilteredData);
        }

        $scope.onFilterTextChanged = function () {
            updateFilteredData();
        };

        load(false);
    });
