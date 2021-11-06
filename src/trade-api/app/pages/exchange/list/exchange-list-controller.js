angular.module('main')
    .controller("exchangeListController", function ($scope, $http, alertService) {
        $http.get('api/get-exchanges')
            .then(function (response) {
                $scope.exchanges = response.data;
            },
            function (err) { alertService.error(err); });
    });