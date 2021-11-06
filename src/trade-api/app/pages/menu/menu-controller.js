
angular.module('main')
.controller('menuController', function ($scope, $http) {
    $scope.topRow = [
        { icon: 'house', link: "home" },        

        { icon: 'money-bag', link: "holdings" },
        { commodity: 'eth', link: "eth" },

        { divider: true },

        { exchange: "Binance", link: "exchange-detail({id:'binance'})" },
        { exchange: "Coss", link: "exchange-detail({id:'coss'})" },
        { exchange: "KuCoin", link: "exchange-detail({id:'kucoin'})" },
        { exchange: "Livecoin", link: "exchange-detail({id:'livecoin'})" },
        { exchange: "HitBtc", link: "exchange-detail({id:'hitbtc'})" },
        { exchange: "Bit-Z", link: "exchange-detail({id:'bit-z'})" },
        { exchange: "Cryptopia", link: "exchange-detail({id:'cryptopia'})" },
        { exchange: "Idex", link: "exchange-detail({id:'idex'})" },
        { exchange: "Kraken", link: "exchange-detail({id:'kraken'})" },
        { exchange: "Qryptos", link: "exchange-detail({id:'qryptos'})" },
        { exchange: "Yobit", link: "exchange-detail({id:'yobit'})" },
        { exchange: "Blocktrade", link: "exchange-detail({id:'blocktrade'})" },
        { exchange: "Gemini", link: "exchange-detail({id:'gemini'})" },
        { exchange: "Mew", link: "exchange-detail({id:'mew'})" },    

        { divider: true },

        { icon: "clock-history", link: "history" },
        { icon: "exchange", link: "exchange-list" },
        { icon: "gear", link: "config" },

        { icon: "status", link: "status" },
        { icon: "log", link: "log" }
    ];

    $scope.bottomRow = [       
        { title: "Comp", link: "comparison" },
        { title: "Arb", link: "arb" },
        { title: "Commodities", link: "commodity-list" },
        { title: "Canon", link: "canon-list" },
        { title: "Valuation", link: "valuation" },
        { title: "New Listings", link: "listing" },
        { title: "Test", link: "test" },
        { title: "Order Book", link: "order-book-list" },
        { title: "Exch Open Orders", link: "exchange-open-orders-list" },
        { title: "Exch Pairs", link: "exchange-trading-pairs-list" },
        { title: "Exch Commodities", link: "exchange-commodities-list" },
        { title: "Exch Order Books", link: "exchange-order-books-list" },
        { title: "Exch History", link: "exchange-history-list" },
        { title: "Place Order", link: "place-order" }
    ];
});

