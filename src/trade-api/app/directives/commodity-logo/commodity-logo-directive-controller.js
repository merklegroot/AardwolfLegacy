angular.module('main')
    .controller('commodityLogoDirectiveController', function ($scope) {
        $scope.getLogo = function () {
            var resRoot = 'res/img/commodities/';
            var logoPortion = 'yoshi-coin.png';

            if ($scope.data === undefined || $scope.data === null) { logoPortion = 'yoshi-coin.png'; }
            else if ($scope.data.toUpperCase() === 'XEM'.toUpperCase()) { logoPortion = 'xem-logo.svg'; }
            else if ($scope.data.toUpperCase() === 'WAVES'.toUpperCase()) { logoPortion = 'waves-logo.svg'; }
            else if ($scope.data.toUpperCase() === 'XRP'.toUpperCase()) { logoPortion = 'ripple-logo.png'; }
            else if ($scope.data.toUpperCase() === 'BTC_GHs'.toUpperCase()) { logoPortion = 'minecraft-pickaxe.png'; }

            else { logoPortion = 'yoshi-coin.png'; }

            var standardSymbols =
                [
                    'KAYA', 'BAT', 'BTT', 'CHX', 'GXS',
                    'TRX', 'XLM', 'NPXS',
                    'LALA', 'SNM', 'GAT', 'AXP', 'KNC',
                    'IOST', 'ARN', 'PAY', 'DASH', 'WTC',
                    'MCO', 'PPT', 'AMB', 'SUB', 'POE',
                    'LTC', 'ICN', 'ICX', 'ATM', 'CAN',
                    'USD', 'VOISE', 'HGT', 'REQ', 'BLZ',
                    'CHSB', 'ZEN', 'IHT', 'ACAT', 'VZT',
                    'LINK', 'LEND', 'LA', 'ADA', 'TIME',
                    'PPC', 'REP', 'BPL', 'ADX', '1ST',
                    'AE', 'AGI', 'AMM', 'BDG', 'BEZ',
                    'ADI', 'BTC', 'ETH', 'ARK', 'EOS',
                    'LSK', 'OMG', 'FYN', 'BCH', 'IDH',
                    'VEN', 'ENJ', 'PIX', 'ADH', 'COV',
                    'ENG', 'BNB', 'AION',
                    'CVC', 'VET', 'OXY', 'USDT', 'QASH',
                    'HAV', 'MITX', 'MRK', 'NEO', 'GBP',
                    'EUR', 'JPY', 'TUSD',
                    'SGD', 'RUB', 'PGT', 'XDCE', 'KIN',
                    'BCD', 'IXT', 'DOGE', 'GUSD',
                    'WISH', 'DAT', 'USDC',
                    'OCN', 'BNTY', 'CS', 'STX', 'ZEC', 'ETC',
                    "COSS", "BCHABC", "BNT", "FXT",
                    "GRS"
                ];

            for (var i = 0; i < standardSymbols.length; i++) {
                var standardSymbol = standardSymbols[i];
                if ($scope.data && $scope.data.toUpperCase() === standardSymbol.toUpperCase()) {                    
                        logoPortion = $scope.data.toLowerCase() + '-logo.png';
                    break;
                }
            }

            return resRoot + logoPortion;
        };
    });