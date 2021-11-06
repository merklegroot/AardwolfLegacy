(function () {
    var controller = function ($scope, $http, $uibModalInstance, alertService, data) {
        $scope.data = {};
        angular.copy(data, $scope.data);

        $scope.ok = function () {
            var result = {
                wasConfirmed: true,
                data: {
                    symbol: $scope.data.symbol,
                    quantity: $scope.data.quantityToSend,
                    source: $scope.data.source,
                    destination: $scope.data.destination
                }
            };

            $uibModalInstance.close(result);
        };

        $scope.cancel = function () {
            $uibModalInstance.dismiss('cancel');
        };

        $scope.onDestinationClicked = function (exchange) {
            $scope.data.destination.name = exchange.name;
            $scope.data.destination.id = exchange.id;
        };
    };

    angular.module('main').controller("transferFundsDialogController", controller);
}) ();