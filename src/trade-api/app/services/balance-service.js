angular.module('main')
.service('balanceService', function ($http) {
    var that = this;

    that.getBalanceForCommodityAndExchange = function (symbol, exchange, forceRefresh) {
        var url = 'api/get-balance-for-commodity-and-exchange';
        var effectiveForceRefresh = forceRefresh !== undefined && forceRefresh !== null
            ? forceRefresh
            : false;

        var serviceModel = { symbol: symbol, exchange: exchange, forceRefresh: forceRefresh };
        return $http.post(url, serviceModel);
    };

    that.getBalanceForExchange = function (exchange, cachePolicy) {
        var url = 'api/get-balance-for-exchange';

        var serviceModel = { exchange: exchange };
        if (cachePolicy !== undefined && cachePolicy !== null) {
            serviceModel.cachePolicy = cachePolicy;
        }

        return $http.post(url, serviceModel);
    };

    // deprecated.
    that.getBalance = that.getBalanceForCommodityAndExchange;
});