angular.module('main')
    .directive('exchangeCommodityList', function () {
        var directivesRoot = 'app/directives/';

    return {
        restrict: 'E',
        require: '^exchange',
        scope: {
            exchange: '=exchange'
        },
        templateUrl: directivesRoot + 'exchange-commodity-list/exchange-commodity-list-template.html',
        controller: 'exchangeCommodityListDirectiveController'
    };
});

