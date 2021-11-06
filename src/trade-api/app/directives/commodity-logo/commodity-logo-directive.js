angular.module('main')
    .directive('commodityLogo', function () {
        var directivesRoot = 'app/directives/';

        return {
            restrict: 'E',
            require: '^symbol',
            scope: {
                data: '=symbol'
            },
            templateUrl: directivesRoot + 'commodity-logo/commodity-logo-template.html',
            controller: 'commodityLogoDirectiveController'
        };
    });


