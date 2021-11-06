angular.module('main')
    .directive('icon', function () {
        var directivesRoot = 'app/directives/';

        return {
            restrict: 'E',
            require: '^image',
            scope: {
                data: '@image'
            },
            templateUrl: directivesRoot + 'icon-directive/icon-template.html',
            controller: function ($scope) {
                var resRoot = 'res/img/icons/';
                var fileName = $scope.data.toLowerCase() + '.png';
                $scope.source = resRoot + fileName;
            }
        };
    });


