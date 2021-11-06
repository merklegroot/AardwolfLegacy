angular.module('main')
    .controller('canonDialogController', function ($scope, $http, $uibModalInstance, alertService, dataService, data) {
        $scope.data = _.clone(data);
        $scope.test = 'asdf';

        $scope.cancel = function () {
            $uibModalInstance.dismiss('cancel');
        };

        $scope.saveChanges = function () {
            var result = {
                wasConfirmed: true,
                data: _.clone($scope.data)
            };

            $uibModalInstance.close(result);
        };
    });