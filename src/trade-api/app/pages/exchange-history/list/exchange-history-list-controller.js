angular.module('main')
    .controller('exchangeHistoryListController', function (
        $scope,
        dataService,
        exchangeService) {

        $scope.model = {
            exchanges: {}
        };

        dataService.loadData($scope.model.exchanges,
            function () { return exchangeService.getExchanges(); });
    });