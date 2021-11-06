angular.module('main', ['ui.router', 'ui.bootstrap', 'ngAnimate', 'toastr', 'ngclipboard']);

// https://github.com/angular-ui/ui-router/issues/2889
angular.module('main')
    .config(['$qProvider', function ($qProvider) {
        $qProvider.errorOnUnhandledRejections(false);
    }]);