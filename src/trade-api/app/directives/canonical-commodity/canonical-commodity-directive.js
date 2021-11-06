angular.module('main')
    .directive('canonicalCommodity', function () {
        var directivesRoot = 'app/directives/';

        return {
            restrict: 'E',
            require: '^commodity',
            scope: {
                data: '=commodity'
            },
            templateUrl: directivesRoot + 'canonical-commodity/canonical-commodity-template.html',
            controller: function ($scope) {
                $scope.getKeys = function () {
                    if (!$scope.data) { return null; }
                    return Object.keys($scope.data);
                };
            }
        };
    });


