(function () {
    var controller = function (
        $scope,
        $http,
        alertService) {

        $scope.model = {
            exchange: "kucoin",
            symbol: "CS",
            baseSymbol: "ETH",
            quantity: 447.93111030,
            price: 0.0007775,
            orderType: "buy"
        };

        $scope.onPlaceOrderClicked = function () {
            var orderDesc = "Exchange: " + $scope.model.exchange + "\r" +
                "Type: " + $scope.model.orderType + "\r" +
                "Symbol: " + $scope.model.symbol + "\r" +
                "BaseSymbol: " + $scope.model.baseSymbol + "\r" +
                "Price: " + $scope.model.price + "\r" + 
                "Quantity: " + $scope.model.quantity;

            if (!confirm("Are you sure you want to place this order?\r\r" + orderDesc)) {
                alertService.info("Not placing.");
                return;
            }

            alertService.info("Placing order...");

            var onSuccess = function (response) { };
            var onError = function (err) {
                alertService.onError(err);
            };

            var serviceModel = {
                orderType: $scope.model.orderType,
                exchange: $scope.model.exchange,
                symbol: $scope.model.symbol,
                baseSymbol: $scope.model.baseSymbol,
                price: $scope.model.price,
                quantity: $scope.model.quantity
            };

            $http.post('api/place-order', serviceModel).then(onSuccess, onError);
        };
    };

    angular.module("main").controller("placeOrderController", controller);
})();