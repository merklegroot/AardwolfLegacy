angular.module('main')
    .controller("exchangeOrderBooksListController", function (
        $stateParams,
        $scope,
        dataService,
        exchangeService) {

        $scope.model = {
            exchanges: {}
        };

        dataService.loadData($scope.model.exchanges, function () {
            return exchangeService.getExchanges();
        });
    });
