angular.module('main')
.service('valuationService', function ($http) {
    var that = this;

    that.getUsdValue = function (symbol, forceRefresh) {
        var url = 'api/get-usdvalue';
        var effectiveForceRefresh = forceRefresh !== undefined && forceRefresh !== null
            ? forceRefresh
            : false;

        var serviceModel = { symbol: symbol, forceRefresh: effectiveForceRefresh };
        return $http.post(url, serviceModel);
    };

    that.getUsdValueV2 = function (symbol, cachePolicy) {
        var url = 'api/get-usdvalue-v2';

        var serviceModel = { symbol: symbol };
        if (cachePolicy !== undefined && cachePolicy !== null) {
            serviceModel.cachePolicy = cachePolicy;
        }

        return $http.post(url, serviceModel);
    };

    that.getHistoricUsdValueV2 = function (symbol, cachePolicy) {
        var url = 'api/get-historic-usdvalue-v2';

        var serviceModel = { symbol: symbol };
        if (cachePolicy !== undefined && cachePolicy !== null) {
            serviceModel.cachePolicy = cachePolicy;
        }

        return $http.post(url, serviceModel);
    };
});