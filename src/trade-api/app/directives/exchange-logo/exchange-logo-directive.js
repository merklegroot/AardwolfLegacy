angular.module('main')
    .directive('exchangeLogo', function () {
        var directivesRoot = 'app/directives/';

        return {
            restrict: 'E',
            require: '^exchange',
            scope: {
                data: '=exchange'
            },
            templateUrl: directivesRoot + 'exchange-logo/exchange-logo-template.html',
            controller: function ($scope) {
                $scope.getLogo = function () {
                    var resRoot = 'res/img/exchanges/';
                    if ($scope.data === undefined || $scope.data === null) {
                        return null;
                    }

                    if ($scope.data.toUpperCase() === 'Binance'.toUpperCase()) { return resRoot + 'binance-logo.svg'; }
                    if ($scope.data.toUpperCase() === 'Kraken'.toUpperCase()) { return resRoot + 'kraken-logo.png'; }
                    if ($scope.data.toUpperCase() === 'Coss'.toUpperCase()) { return resRoot + 'coss-logo.png'; }
                    if ($scope.data.toUpperCase() === 'Tidex'.toUpperCase()) { return resRoot + 'tidex-logo.png'; }
                    if ($scope.data.toUpperCase() === 'Idex'.toUpperCase()) { return resRoot + 'idex-logo.png'; }
                    if ($scope.data.toUpperCase() === 'HitBTC'.toUpperCase()) { return resRoot + 'hitbtc-logo.png'; }
                    if ($scope.data.toUpperCase() === 'Bit-Z'.toUpperCase()) { return resRoot + 'bit-z-logo.png'; }
                    if ($scope.data.toUpperCase() === 'BitZ'.toUpperCase()) { return resRoot + 'bit-z-logo.png'; }
                    if ($scope.data.toUpperCase() === 'KuCoin'.toUpperCase()) { return resRoot + 'kucoin-logo.png'; }
                    if ($scope.data.toUpperCase() === 'Livecoin'.toUpperCase()) { return resRoot + 'livecoin-logo.png'; }
                    if ($scope.data.toUpperCase() === 'Coinbase'.toUpperCase()) { return resRoot + 'coinbase-logo.png'; }
                    if ($scope.data.toUpperCase() === 'Cryptopia'.toUpperCase()) { return resRoot + 'cryptopia-logo.png'; }
                    if ($scope.data.toUpperCase() === 'Mew'.toUpperCase()) { return resRoot + 'mew.png'; }
                    if ($scope.data.toUpperCase() === 'Yobit'.toUpperCase()) { return resRoot + $scope.data.toLowerCase() + '-logo.png'; }
                    if ($scope.data.toUpperCase() === 'Qryptos'.toUpperCase()) { return resRoot + 'qryptos-logo.png'; }
                    if ($scope.data.toUpperCase() === 'Minergate'.toUpperCase()) { return resRoot + 'minergate-logo.png'; }
                    if ($scope.data.toUpperCase() === 'Aggregate'.toUpperCase()) { return resRoot + 'aggregate-logo.png'; }
                    if ($scope.data.toUpperCase() === 'Oex'.toUpperCase()) { return resRoot + 'oex-logo.png'; }
                    if ($scope.data.toUpperCase() === 'Gemini'.toUpperCase()) { return resRoot + 'gemini-logo.png'; }
                    if ($scope.data.toUpperCase() === 'Blocktrade'.toUpperCase()) { return resRoot + 'blocktrade-logo.png'; }

                    if ($scope.data.toUpperCase() === 'Me'.toUpperCase()) { return resRoot + 'invader.png'; }

                    return null;
                };
            }
        };
    });


