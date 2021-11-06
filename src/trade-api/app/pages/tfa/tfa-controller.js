angular.module('main')
    .controller('tfaController', function ($scope, $http, dataService, alertService) {

        $scope.model = {};
        dataService.loadData($scope.model, function () { return $http.post('api/get-tfa'); });
    });