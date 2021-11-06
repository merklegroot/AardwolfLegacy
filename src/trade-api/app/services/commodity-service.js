angular.module('main')
.service('commodityService', function ($http) {
    var that = this;

    that.getCommodities = function (forceRefresh) {
        var url = 'api/get-commodities';
        var effectiveForceRefresh = forceRefresh !== undefined && forceRefresh !== null
            ? forceRefresh
            : false;

        var serviceModel = { forceRefresh: effectiveForceRefresh };
        return $http.post(url, serviceModel);
    };

    that.getExchangeCommodityByNativeSymbol = function (exchange, nativeSymbol, forceRefresh) {
        var effectiveForceRefresh = forceRefresh !== undefined && forceRefresh !== null ? forceRefresh : false;
        var serviceModel = { exchange: exchange, nativeSymbol: nativeSymbol, forceRefresh: effectiveForceRefresh };
        return $http.post('api/get-commodity-for-exchange', serviceModel);
    };

    that.getExchangeCommodityBySymbol = function (exchange, symbol, forceRefresh) {
        var effectiveForceRefresh = forceRefresh !== undefined && forceRefresh !== null ? forceRefresh : false;
        var serviceModel = { exchange: exchange, symbol: symbol, forceRefresh: effectiveForceRefresh };
        return $http.post('api/get-commodity-for-exchange', serviceModel);
    };

    that.getExchangeCommodityBySymbolExcludeDepositAddress = function (exchange, symbol, forceRefresh) {
        var effectiveForceRefresh = forceRefresh !== undefined && forceRefresh !== null ? forceRefresh : false;
        var serviceModel = { exchange: exchange, symbol: symbol, forceRefresh: effectiveForceRefresh, excludeDepositAddress: true };
        return $http.post('api/get-commodity-for-exchange', serviceModel);
    };

    that.getCommodityDetails = function (symbol, forceRefresh) {
        var effectiveForceRefresh = forceRefresh !== undefined && forceRefresh !== null ? forceRefresh : false;
        var serviceModel = { symbol: symbol, forceRefresh: effectiveForceRefresh, excludeDepositAddress: true };
        return $http.post('api/get-commodity-details', serviceModel);
    };
});