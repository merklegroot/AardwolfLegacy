angular.module('main')
    .controller('canonDetailController', function (
        $scope, $http, $stateParams,
        alertService, dataService) {

        var id = $stateParams.id;

        $scope.test = 'asdf';
        $scope.model = {
            canon: {}
        };

        var serviceModel = { id: id };
        dataService.loadData($scope.model.canon, function () { return $http.post('api/get-canon-item', serviceModel); });

        $scope.getKeys = function () {
            if (!$scope.model || !$scope.model.canon || !$scope.model.canon.data) { return null; }
            return Object.keys($scope.model.canon.data);
        };
});