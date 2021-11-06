angular.module('main')
.controller('intersectionController', function ($scope, $http, dataService, alertService) {
    $scope.model = {};
    dataService.loadData($scope.model, function () { return $http.post('api/get-intersections'); });

    $scope.hasBinance = function (intersection) {
        for (var i = 0; i < intersection.exchanges.length; i++) {
            if (intersection.exchanges[i] === 'Binance') { return true; }
        }

        return false;
    }
});
