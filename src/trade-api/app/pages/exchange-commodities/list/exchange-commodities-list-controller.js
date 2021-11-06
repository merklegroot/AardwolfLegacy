angular.module('main')
    .controller('exchangeCommoditiesListController', function (
        $scope,
        dataService,
        exchangeService) {

        $scope.model = {
            exchanges: {}
        };

        dataService.loadData($scope.model.exchanges, function () { return exchangeService.getExchanges(); });
    });