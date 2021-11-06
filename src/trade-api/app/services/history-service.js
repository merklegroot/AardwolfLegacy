angular.module('main')
.service('historyService', function ($http) {
    var that = this;

    that.getExchangeHistory = function (exchange, cachePolicy) {
        var url = 'api/get-history-for-exchange';
        var serviceModel = { exchange: exchange };

        if (cachePolicy !== undefined && cachePolicy !== null) {
            serviceModel.cachePolicy = cachePolicy;
        }

        return $http.post(url, serviceModel);
    };
});