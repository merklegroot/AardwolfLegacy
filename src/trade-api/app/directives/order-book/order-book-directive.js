angular.module('main')
    .directive('orderBook', function () {
        var directivesRoot = 'app/directives/';

        return {
            restrict: 'E',
            require: '^ngModel',
            scope: {
                model: '=ngModel',
                valuations: '=valuations',
                baseSymbol: '=baseSymbol',
                layout: '@layout',
                minRows: '@minRows',
                hideTerms: '@hideTerms'
            },
            templateUrl: directivesRoot + 'order-book/order-book-directive-template.html',
            controller: 'orderBookDirectiveController'
        };
    });


