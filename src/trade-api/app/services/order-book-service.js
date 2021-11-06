angular.module('main')
.service('orderBookService', function ($http) {
    var that = this;

    that.getOrderBook = function (symbol, baseSymbol, exchange, forceRefresh, cachePolicy) {
        var url = 'api/get-order-book';
        var serviceModel = { symbol: symbol, baseSymbol: baseSymbol, exchange: exchange};

        if (forceRefresh !== undefined && forceRefresh !== null) {
            serviceModel.forceRefresh = forceRefresh;
        }

        if (cachePolicy !== undefined && cachePolicy !== null) {
            serviceModel.cachePolicy = cachePolicy;
        }

        return $http.post(url, serviceModel);
    };

    that.getCachedOrderBooks = function (exchange) {
        var url = 'api/get-cached-order-books';
        var serviceModel = { exchange: exchange };
        return $http.post(url, serviceModel);
    };
});