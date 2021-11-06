angular.module('main')
.controller('configController', function ($scope, $http, dataService, alertService) {    

    $scope.model = {
        stuff: "blah",
        exchanges: [
            { 'id': 'coss', 'name': 'Coss' },
            { 'id': 'tidex', 'name': 'Tidex' },
            { 'id': 'blocktrade', 'name': 'Blocktrade' },
            { 'id': 'kucoin', 'name': 'KuCoin' },
            { 'id': 'binance', 'name': 'Binance' },
            { 'id': 'hit-btc', 'name': 'HitBTC' },
            { 'id': 'kraken', 'name': 'Kraken' },
            { 'id': 'livecoin', 'name': 'Livecoin' },
            { 'id': 'bitz', 'name': 'Bit-Z' },
            { 'id': 'cryptopia', 'name': 'Cryptopia' },
            { 'id': 'qryptos', 'name': 'Qryptos' },
            { 'id': 'twitter', 'name': 'Twitter' },
            { 'id': 'infura', 'name': 'Infura' },
        ]
    };
    
    var loadApiKey = function (exchange) {
        exchange.apiKey = {};
        exchange.name = exchange.name || exchange.id;
        exchange.isLoading = true;

        var url = 'api/get-' + exchange.id + '-api-key';
        $http.post(url).then(
            function (response) {
                exchange.isLoading = false;
                exchange.apiKey = response.data.apiKey;
            },
            function (err) {
                exchange.isLoading = false;
                alertService.errorHandler(err);
            }
        );
    };

    var loadExchanges = function () {
        angular.forEach($scope.model.exchanges, function (exchange) {
            loadApiKey(exchange);
        });
    };

    var loadData = function () {
        loadExchanges();

        $scope.isLoading = true;
        $http.get('api/get-mew-wallet-address').then(
            function (response) {
                $scope.isLoading = false;
                $scope.model.mew = response.data; // .myEtherWalletAddress;
            },
            function (err) { $scope.isLoading = false; alertService.errorHandler(err); }
        );

        $scope.isLoadingConnectionString = true;
        $http.post('api/get-connection-string').then(
            function (response) {
                $scope.isLoadingConnectionString = false;
                $scope.model.connectionString = response.data.connectionString;
            },
            function (err) { $scope.isLoadingConnectionString = false; alertService.errorHandler(err); }
        );


        $scope.isLoadingEtherscanApiKey = true;
        $http.post('api/get-etherscan-api-key').then(
            function (response) {
                $scope.isLoadingEtherscanApiKey = false;
                $scope.model.etherscanApiKey = response.data.apiKey;
            },
            function (err) { $scope.isLoadingEtherscanApiKey = false; alertService.errorHandler(err); }
        );

        $scope.isLoadingCossCredentials = true;
        $http.post('api/get-coss-credentials').then(
            function (response) {
                $scope.isLoadingCossCredentials = false;
                $scope.model.cossCredentials = response.data;
            },
            function (err) { $scope.isLoadingCossCredentials = false; alertService.errorHandler(err); }
        );

        $scope.isLoadingCossEmailCredentials = true;
        $http.post('api/get-coss-email-credentials').then(
            function (response) {
                $scope.isLoadingCossEmailCredentials = false;
                $scope.model.cossEmailCredentials = response.data;
            },
            function (err) { $scope.isLoadingCossEmailCredentials = false; alertService.errorHandler(err); }
        );

        $scope.isLoadingBitzLoginCredentials = true;
        $http.post('api/get-bitz-login-credentials').then(
            function (response) {
                $scope.isLoadingBitzLoginCredentials = false;
                $scope.model.bitzLoginCredentials = response.data;
            },
            function (err) { $scope.isLoadingBitzLoginCredentials = false; alertService.errorHandler(err); }
        );

        $scope.isLoadingKucoinEmailCredentials = true;
        $http.post('api/get-kucoin-email-credentials').then(
            function (response) {
                $scope.isLoadingKucoinEmailCredentials = false;
                $scope.model.kucoinEmailCredentials = response.data;
            },
            function (err) { $scope.isLoadingKucoinEmailCredentials = false; alertService.errorHandler(err); }
        );

        $scope.isLoadingBitzTradePassword = true;
        $http.post('api/get-bitz-trade-password').then(
            function (response) {
                $scope.isLoadingBitzTradePassword = false;
                $scope.model.bitzTradePassword = response.data;
            },
            function (err) { $scope.isLoadingBitzTradePassword = false; alertService.errorHandler(err); }
        );

        $scope.isLoadingKucoinTradePassword = true;
        $http.post('api/get-kucoin-trade-password').then(
            function (response) {
                $scope.isLoadingKucoinTradePassword = false;
                $scope.model.kucoinTradePassword = response.data;
            },
            function (err) { $scope.isLoadingKucoinTradePassword = false; alertService.errorHandler(err); }
        );

        $scope.isLoadingKucoinApiPassphrase = true;
        $http.post('api/get-kucoin-api-passphrase').then(
            function (response) {
                $scope.isLoadingKucoinApiPassphrase = false;
                $scope.model.kucoinApiPassphrase = response.data;
            },
            function (err) { $scope.isLoadingKucoinApiPassphrase = false; alertService.errorHandler(err); }
        );

        $scope.isLoadingMewPassword = true;
        $http.post('api/get-mew-password').then(
            function (response) {
                $scope.isLoadingMewPassword = false;
                $scope.model.mewPassword = response.data;
            },
            function (err) { $scope.isLoadingMewPassword = false; alertService.errorHandler(err); }
        );

        $scope.isLoadingMewWalletFileName = true;
        $http.post('api/get-mew-wallet-filename').then(
            function (response) {
                $scope.isLoadingMewWalletFileName = false;
                $scope.model.mewWalletFileName = response.data;
            },
            function (err) { $scope.isLoadingMewWalletFileName = false; alertService.errorHandler(err); }
        );

        $scope.isLoadingCossAgentConfig = true;
        $http.post('api/get-coss-agent-config').then(
            function (response) {
                $scope.isLoadingCossAgentConfig = false;

                $scope.cossAgentConfig = response.data ? response.data : {};
            },
            function (err) { $scope.isLoadingCossAgentConfig = false; alertService.errorHandler(err); }
        );

        $scope.isLoadingBitzAgentConfig = true;
        $http.post('api/get-bitz-agent-config').then(
            function (response) {
                $scope.isLoadingBitzAgentConfig = false;
                $scope.bitzAgentConfig = response.data ? response.data : {};
            },
            function (err) { $scope.isLoadingBitzAgentConfig = false; alertService.errorHandler(err); }
        );

        $scope.ccxtConfig = {};
        $scope.isLoadingCcxtUrl = true;
        $http.post('api/get-ccxt-url').then(
            function (response) {
                $scope.isLoadingCcxtUrl = false;
                $scope.ccxtConfig.url = response.data ? response.data : '';
            },
            function (err) { $scope.isLoadingCcxtUrl = false; alertService.errorHandler(err); }
        );
    };

    $scope.onUpdateMyEtherWalletAddressClicked = function () {
        $scope.isUpdating = true;
        var onSuccess = function (response) {
            $scope.isUpdating = false;
            $scope.model.mew = response.data;

            alertService.success('Updated.');
        };

        var onError = function (err) { $scope.isUpdating = false; alertService.errorHandler(err); };

        var data = { myEtherWalletAddress: $scope.model.mew };
        try {
            $http.post('api/set-mew-wallet-address', data)
                .then(onSuccess, onError);
        } catch (ex) { alert(JSON.stringify(ex)); }
    };

    $scope.onUpdateMongoConnectionStringClick = function () {
        var onSuccess = function (response) {
            $scope.isUpdating = false;
            $scope.connectionString = response.data.connectionString;

            alertService.success('Updated.');
        };

        var onError = function (err) { alertService.errorHandler(err); };

        var data = { connectionString: $scope.model.connectionString };
        try {
            $http.post('api/set-connection-string', data)
                .then(onSuccess, onError);
        } catch (ex) { alert(JSON.stringify(ex)); }        
    }

    $scope.onUpdateEtherscanApiKeyClick = function () {
        var onSuccess = function (response) {
            $scope.isUpdating = false;
            $scope.etherscanApiKey = response.data.apiKey;

            alertService.success('Updated.');
        };

        var onError = function (err) { alertService.errorHandler(err); };

        alertService.info('Updating...');
        var data = { apiKey: { key: $scope.model.etherscanApiKey } };
        try {
            $http.post('api/set-etherscan-api-key', data)
                .then(onSuccess, onError);
        } catch (ex) { alert(JSON.stringify(ex)); }
    };

    $scope.onUpdateCossCredentialsClicked = function () {
        var onSuccess = function (response) {
            $scope.isUpdating = false;
            $scope.cossCredentials = response.data;

            alertService.success('Updated.');
        };

        var onError = function (err) { alertService.errorHandler(err); };

        alertService.info('Updating...');
        var data = $scope.model.cossCredentials;
        try {
            $http.post('api/set-coss-credentials', data)
                .then(onSuccess, onError);
        } catch (ex) { alert(JSON.stringify(ex)); }
    };

    $scope.onUpdateCossEmailCredentialsClicked = function () {
        var onSuccess = function (response) {
            $scope.isUpdating = false;
            $scope.cossEmailCredentials = response.data;

            alertService.success('Updated.');
        };

        var onError = function (err) { alertService.errorHandler(err); };

        alertService.info('Updating...');
        var data = $scope.model.cossEmailCredentials;
        try {
            $http.post('api/set-coss-email-credentials', data)
                .then(onSuccess, onError);
        } catch (ex) { alert(JSON.stringify(ex)); }
    };

    $scope.onUpdateBitzLoginCredentialsClicked = function () {
        var onSuccess = function (response) {
            $scope.isUpdating = false;
            $scope.bitzLoginCredentials = response.data;

            alertService.success('Updated.');
        };

        var onError = function (err) { alertService.errorHandler(err); };

        alertService.info('Updating...');
        var data = $scope.model.bitzLoginCredentials;
        try {
            $http.post('api/set-bitz-login-credentials', data)
                .then(onSuccess, onError);
        } catch (ex) { alert(JSON.stringify(ex)); }
    };

    $scope.onUpdateKucoinEmailCredentialsClicked = function () {
        var onSuccess = function (response) {
            $scope.isUpdating = false;
            $scope.kucoinEmailCredentials = response.data;

            alertService.success('Updated.');
        };

        var onError = function (err) { alertService.errorHandler(err); };

        alertService.info('Updating...');
        var data = $scope.model.kucoinEmailCredentials;
        try {
            $http.post('api/set-kucoin-email-credentials', data)
                .then(onSuccess, onError);
        } catch (ex) { alert(JSON.stringify(ex)); }
    };
    
    $scope.onUpdateExchangeClicked = function (exchange) {
        alertService.info(JSON.stringify(exchange));

        var onSuccess = function (response) {
            exchange.isUpdating = false;
            exchange.apiKey = response.data.apiKey;

            alertService.success('Updated.');
        };

        var onError = function (err) { alertService.errorHandler(err); };

        alertService.info('Updating...');
        var data = { apiKey: exchange.apiKey };
        try {
            $http.post('api/set-' + exchange.id + '-api-key', data)
                .then(onSuccess, onError);
        } catch (ex) { alert(JSON.stringify(ex)); }
    };

    $scope.onUpdateBitzTradePasswordClicked = function () {
        var onSuccess = function (response) {
            $scope.isUpdating = false;
            $scope.bitzTradePassword = response.data;

            alertService.success('Updated.');
        };

        var onError = function (err) { alertService.errorHandler(err); };

        alertService.info('Updating Bit-Z Trade Password...');
        var data = { password: $scope.model.bitzTradePassword };
        try {
            $http.post('api/set-bitz-trade-password', data)
                .then(onSuccess, onError);
        } catch (ex) { alert(JSON.stringify(ex)); }
    };

    $scope.onUpdateKucoinTradePasswordClicked = function () {
        var onSuccess = function (response) {
            $scope.isUpdating = false;
            $scope.kucoinTradePassword = response.data;

            alertService.success('Updated.');
        };

        var onError = function (err) { alertService.errorHandler(err); };

        alertService.info('Updating Kucoin Trade Password...');
        var data = { password: $scope.model.kucoinTradePassword };
        try {
            $http.post('api/set-kucoin-trade-password', data)
                .then(onSuccess, onError);
        } catch (ex) { alert(JSON.stringify(ex)); }
    };

    $scope.onUpdateKucoinApiPassphraseClicked = function () {
        var onSuccess = function (response) {
            $scope.isUpdating = false;
            $scope.kucoinApiPassphrase = response.data;

            alertService.success('Updated.');
        };

        var onError = function (err) { alertService.errorHandler(err); };

        alertService.info('Updating Kucoin Trade Password...');
        var data = { password: $scope.model.kucoinApiPassphrase };
        try {
            $http.post('api/set-kucoin-api-passphrase', data)
                .then(onSuccess, onError);
        } catch (ex) { alert(JSON.stringify(ex)); }
    };

    $scope.onUpdateMewPasswordClicked = function () {
        var onSuccess = function (response) {
            $scope.isUpdating = false;
            $scope.mewPassword = response.data;

            alertService.success('Updated.');
        };

        var onError = function (err) { alertService.errorHandler(err); };

        alertService.info('Updating Mew Password...');
        var data = { password: $scope.model.mewPassword };
        try {
            $http.post('api/set-mew-password', data)
                .then(onSuccess, onError);
        } catch (ex) { alert(JSON.stringify(ex)); }
    };

    $scope.onUpdateMewWalletFileNameClicked = function () {
        var onSuccess = function (response) {
            $scope.isUpdating = false;
            $scope.mewFileName = response.data;

            alertService.success('Updated.');
        };

        var onError = function (err) { alertService.errorHandler(err); };

        alertService.info('Updating Mew Password...');
        var data = { fileName: $scope.model.mewWalletFileName };
        try {
            $http.post('api/set-mew-wallet-filename', data)
                .then(onSuccess, onError);
        } catch (ex) { alert(JSON.stringify(ex)); }
    };

    $scope.onUpdateCossAgentConfigClicked = function () {
        var onSuccess = function (response) {
            $scope.isUpdating = false;
            $scope.cossAgentConfig = response.data;

            alertService.success('Updated.');
        };

        var onError = function (err) { alertService.errorHandler(err); };

        alertService.info('Updating Coss Agent Config...');
        var data = $scope.cossAgentConfig;
        try {
            $http.post('api/set-coss-agent-config', data)
                .then(onSuccess, onError);
        } catch (ex) { alert(JSON.stringify(ex)); }

    };

    $scope.onUpdateBitzAgentConfigClicked = function () {
        var onSuccess = function (response) {
            $scope.isUpdating = false;
            $scope.bitzAgentConfig = response.data;

            alertService.success('Updated.');
        };

        var onError = function (err) { alertService.errorHandler(err); };

        alertService.info('Updating Bit-Z Agent Config...');
        var data = $scope.bitzAgentConfig;
        try {
            $http.post('api/set-bitz-agent-config', data)
                .then(onSuccess, onError);
        } catch (ex) { alert(JSON.stringify(ex)); }

    };

    $scope.onUpdateCcxtUrlClick = function () {
        var onSuccess = function (response) {
            $scope.isUpdating = false;
            $scope.ccxtConfig.url = response.data;

            alertService.success('Updated.');
        };

        var onError = function (err) { alertService.errorHandler(err); };

        alertService.info('Updating Ccxt Config...');
        var data = { url: $scope.ccxtConfig.url };
        try {
            $http.post('api/set-ccxt-url', data)
                .then(onSuccess, onError);
        } catch (ex) { alert(JSON.stringify(ex)); }
    };

    loadData();
});
