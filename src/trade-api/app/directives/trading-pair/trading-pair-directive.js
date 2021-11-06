angular.module('main')
    .directive('tradingPair', function () {
        var directivesRoot = 'app/directives/';

        return {
            restrict: 'E',
            require: '^ngModel',
            scope: {
                model: '=ngModel'
            },
            templateUrl: directivesRoot + 'trading-pair/trading-pair-template.html',
            controller: function ($scope) {
                $scope.test = 'stuff';
                // $scope.exchange = $
            }
        };
    });


