
angular.module('main')
    .config(function ($stateProvider, $urlRouterProvider) {

        var toCamel = function (text) {
            return text.replace(/-([a-z])/g, function (g) { return g[1].toUpperCase(); });
        };

        var milliseconds = new Date().getMilliseconds();

        var preventCache = function (url) {
            return url + "?t=" + milliseconds;
        };

        $urlRouterProvider.otherwise('/home');

        var getTemplateUrl = function (name) {
            return preventCache('app/pages/' + name + '/' + name + '-template.html');
        };

        var getListTemplateUrl = function (name) {
            return preventCache('app/pages/' + name + '/' + 'list' + '/' + name + '-list-template.html');
        };

        var getDetailTemplateUrl = function (name) {
            return preventCache('app/pages/' + name + '/' + 'detail' + '/' + name + '-detail-template.html');
        };

        var menuView = { templateUrl: getTemplateUrl('menu'), controller: 'menuController' };

        var getView = function (name) {
            var view = {};
            view.main = { templateUrl: getTemplateUrl(name), controller: toCamel(name) + 'Controller' };
            view.menu = menuView;

            return view;
        };

        var getListView = function (name) {
            var view = {};
            view.main = { templateUrl: getListTemplateUrl(name), controller: toCamel(name) + 'ListController' };
            view.menu = menuView;

            return view;
        };

        var getDetailView = function (name) {
            var view = {};
            view.main = { templateUrl: getDetailTemplateUrl(name), controller: toCamel(name) + 'DetailController' };
            view.menu = menuView;

            return view;
        };

        var addPage = function (name) {
            $stateProvider.state(name, { url: '/' + name, views: getView(name) });
        };

        var addListPage = function (name) {
            $stateProvider.state(name + '-list', { url: '/' + name + '-list', views: getListView(name) });
        };

        var addDetailPage = function (name) {
            $stateProvider.state(name + '-detail', { url: '/' + name + '-detail/:id', views: getDetailView(name) });
        };

        addPage('home');
        addPage('arb');
        $stateProvider.state('arb-detail', {
            url: '/arb/:exchangeA/:exchangeB/:symbol',
            views: getView('arb'),
        });

        addListPage('asset');
        addDetailPage('asset');
        addListPage('exchange');
        addDetailPage('exchange');
        addPage('status');
        addPage('eth');
        addPage('holdings');
        addPage('binance');
        addPage('kucoin');
        addPage('hitbtc');
        addPage('valuation');
        addPage('order');
        addPage('log');
        addPage('config');
        addPage('agent');
        addPage('notes');
        addPage('intersection');
        addPage('history');
        addPage('tfa');
        addPage('coss');
        addPage('comparison');
        addPage('place-order');

        $stateProvider.state('comparison-detail', {
            url: '/comparison/:exchangeA/:exchangeB/:symbol/:baseSymbol',
            views: getView('comparison')
        });

        addPage('test');
        addListPage('commodity');
        addListPage('order-book');
        addListPage('exchange-trading-pairs');
        addDetailPage('exchange-trading-pairs');
        addListPage('exchange-commodities');
        addListPage('exchange-order-books');

        $stateProvider.state('order-book-detail', {
            url: '/order-book-detail/:exchange/:symbol/:baseSymbol',
            views: getDetailView('order-book')
        });

        $stateProvider.state('exchange-commodities-detail', {
            url: '/exchange-commodities-detail/:exchange', views: getDetailView('exchange-commodities')
        });

        $stateProvider.state('exchange-order-books-detail', {
            url: '/exchange-order-books-detail/:exchange', views: getDetailView('exchange-order-books')
        });

        $stateProvider.state('commodity-detail', {
            url: '/commodity-detail/:symbol', views: getDetailView('commodity')
        });

        $stateProvider.state('exchange-commodity-detail', {
            url: '/exchange-commodity-detail/:exchange/:nativeSymbol', views: {
                menu: menuView,
                main: { templateUrl: getDetailTemplateUrl('exchange-commodity'), controller: 'exchangeCommodityDetailController' }
            }
        });

        addListPage('exchange-open-orders');

        $stateProvider.state('exchange-open-orders-detail', {
            url: '/exchange-open-orders-detail/:exchange', views: getDetailView('exchange-open-orders')
        });

        $stateProvider.state('exchange-open-orders-detail-trading-pair', {
            url: '/exchange-open-orders-detail/:exchange/:symbol/:baseSymbol',
            views: getDetailView('exchange-open-orders')
        });

        addPage('listing');
        addListPage('canon');
        addDetailPage('canon');

        addListPage('exchange-history');
        $stateProvider.state('exchange-history-detail', {
            url: '/exchange-history/:exchange', views: getDetailView('exchange-history')
        });

        addPage('binance-arb-config');
    });
