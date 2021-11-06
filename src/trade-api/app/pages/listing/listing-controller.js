angular.module('main')
    .controller('listingController', function ($scope, $http, dataService, alertService) {
        $scope.listings = {};

        $scope.listings.isLoading = true;
        var onSuccess = function (response) { $scope.listings.isLoading = false; $scope.listings.data = response.data; };
        var onError = function (err) { $scope.listings.isLoading = false; alertService.onError(err); };
        $http.post('api/get-binance-listings').then(onSuccess, onError);
    });