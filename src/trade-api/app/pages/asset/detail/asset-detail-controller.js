angular.module('main')
    .controller("assetDetailController", function ($stateParams, $scope, $http, alertService) {
        var symbol = $stateParams.id;
        $scope.asset = { symbol: symbol };

        $http.get('api/get-asset-detail?symbol=' + symbol)
            .then(function (response) {
                $scope.asset = response.data;
            },
            function (err) { alertService.error(JSON.stringify(err)); });        
    });
