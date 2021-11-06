angular.module('main')
    .controller('exchangeOpenOrdersListController', function (
        $scope,
        dataService,
        exchangeService) {

        $scope.model = {
            exchanges: {}
        };

        dataService.loadData($scope.model.exchanges,
            function () { return exchangeService.getExchanges(); });
    });