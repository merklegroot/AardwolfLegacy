angular.module('main')
    .directive('comparison', function () {
        var directivesRoot = 'app/directives/';

        return {
            restrict: 'E',
            require: '^ngModel',
            scope: {
                model: '=ngModel',
                showIfUnprofitable: '=showIfUnprofitable',
                leftRefresh: '&leftRefresh',
                rightRefresh: '&rightRefresh'
            },
            templateUrl: directivesRoot + 'comparison-directive/comparison-directive-template.html',
            controller: 'comparisonDirectiveController'
        };
    });


