angular.module('main')
    .directive('balance', function () {
        var directivesRoot = 'app/directives/';

        return {
            restrict: 'E',
            require: '^exchange',
            scope: {
                exchange: '=exchange'
            },
            templateUrl: directivesRoot + 'balance/balance-template.html',
            controller: function ($scope) {
                $scope.test = 'stuff';
                // $scope.exchange = $
            }
        };
    });


