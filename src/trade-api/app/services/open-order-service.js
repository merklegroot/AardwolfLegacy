angular.module('main')
.service('openOrderService', function ($http) {
    var that = this;

    that.getOpenOrders = function (exchange, forceRefresh, cachePolicy) {
        var url = 'api/get-open-orders';
        var serviceModel = { exchange: exchange };

        if (forceRefresh !== undefined && forceRefresh !== null) {
            serviceModel.forceRefresh = forceRefresh;
        }

        if (cachePolicy !== undefined && cachePolicy !== null) {
            serviceModel.cachePolicy = cachePolicy;
        }

        return $http.post(url, serviceModel);
    };

    that.getOpenOrdersV2 = function (exchange) {
        var url = 'api/get-open-orders-v2';
        var serviceModel = { exchange: exchange };

        return $http.post(url, serviceModel);
    };

    that.getOpenOrdersForTradingPairV2 = function (exchange, symbol, baseSymbol, cachePolicy) {
        var url = "api/get-open-orders-for-trading-pair-v2";
        var serviceModel = { exchange: exchange, symbol: symbol, baseSymbol: baseSymbol };
        if (cachePolicy !== undefined && cachePolicy !== null) { serviceModel.cachePolicy = cachePolicy; }

        return $http.post(url, serviceModel);
    };
});