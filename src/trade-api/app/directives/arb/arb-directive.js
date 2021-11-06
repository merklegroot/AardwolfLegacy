angular.module('main')
.directive('arb', function () {
    var directivesRoot = 'app/directives/';

    return {
        restrict: 'E',
        require: '^ngModel',
        scope: {
            arb: '=ngModel'
        },
        templateUrl: directivesRoot + 'arb/arb-directive-template.html',
        controller: 'arbDirectiveController'
    };
});


