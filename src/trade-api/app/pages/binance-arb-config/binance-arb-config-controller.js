(function () {
    var controller = function ($scope, $http, alertService) {
        var controllerData = {

        };

        $scope.model = {
            binanceArbConfig: {}
        };

        $scope.onSaveClicked = function () {
            alertService.info("Saving...");

            var data = $scope.model.binanceArbConfig.data;
            var onError = function(err) { alertService.onError(err); };
            var onSuccess = function (response) {
                alertService.info("Saved!");
                loadData();
            };

            $http.post("api/set-binance-arb-config", data)
                .then(onSuccess, onError);
        };


        var loadData = function () {
            var closureData = {
                controllerData: controllerData,
                model: $scope.model
            };

            var onSuccess = function (response) {
                alertService.info(response.data);

                closureData.controllerData.binanceArbConfig = _.cloneDeep(response.data);
                closureData.model.binanceArbConfig.data = _.cloneDeep(response.data);
                closureData.model.binanceArbConfig.isLoading = false;
            };

            var onError = function (err) {
                closureData.model.binanceArbConfig.isLoading = false;
                alertService.onError(err);
            };
            
            var retriever = function () { return $http.post("api/get-binance-arb-config"); };

            retriever().then(onSuccess, onError);
        };

        loadData();
    };

    angular.module("main").controller("binanceArbConfigController", controller);
})();