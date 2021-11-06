angular.module('main')
.service('logService', function ($http) {
    var that = this;

    that.getLogs = function () {
        return $http.post('api/get-logs');
    };
});