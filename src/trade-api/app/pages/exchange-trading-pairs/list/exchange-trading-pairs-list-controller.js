angular.module('main')
    .controller("exchangeTradingPairsListController", function (
        $stateParams, $scope, $http, $uibModal, $timeout,
        dataService, alertService,
        orderBookService,
        balanceService,
        valuationService,
        commodityService,
        exchangeService) {

        $scope.model = {
            exchanges: {}
        };

        dataService.loadData($scope.model.exchanges, function () { return exchangeService.getExchanges(); });
    });
