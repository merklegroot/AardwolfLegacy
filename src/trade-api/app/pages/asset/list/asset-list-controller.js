angular.module('main')
    .controller("assetListController", function ($scope, $http, alertService) {
        $http.get('api/assets')
            .then(function (response) {
                $scope.assets = response.data;
            },
            function (err) { alertService.error(err); });
    });