angular.module('main')
.service('exchangeService', function ($http) {
    var that = this;

    that.getCommoditiesForExchange = function (exchange, forceRefresh) {
        var url = 'api/get-commodities-for-exchange';
        var effectiveForceRefresh = forceRefresh !== undefined && forceRefresh !== null
            ? forceRefresh
            : false;

        var serviceModel = { exchange: exchange, forceRefresh: effectiveForceRefresh };
        return $http.post(url, serviceModel);
    };

    that.getTradingPairsForExchange = function (exchange, forceRefresh, cachePolicy) {
        var url = 'api/get-trading-pairs-for-exchange';
        var serviceModel = { exchange: exchange };

        if (forceRefresh !== undefined && forceRefresh !== null) {
            serviceModel.forceRefresh = forceRefresh;
        }

        if (cachePolicy !== undefined && cachePolicy !== null) {
            serviceModel.cachePolicy = cachePolicy;
        }

        return $http.post(url, serviceModel);
    };

    that.getExchanges = function () {
        var url = 'api/get-exchanges';
        return $http.post(url);
    };
});