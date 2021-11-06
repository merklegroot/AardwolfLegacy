angular.module('main')
    .directive('checkButton', function () {
        var directivesRoot = 'app/directives/';

        return {
            restrict: 'E',
            scope: {
                data: '&ngModel',
                text: '@text'
            },
            templateUrl: directivesRoot + 'check-button-directive/check-button-template.html',
            controller: function ($scope) {
                $scope.checked = false;

                $scope.onClicked = function () {
                    $scope.checked = !$scope.checked;
                };
            }
        };
    });


