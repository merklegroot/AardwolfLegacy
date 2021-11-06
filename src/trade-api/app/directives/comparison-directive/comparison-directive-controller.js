angular.module('main')
    .controller('comparisonDirectiveController', function (
        $scope,
        exchangeDictionary) {

        var refreshes = [
            function () { if ($scope.leftRefresh) { $scope.leftRefresh(); } },
            function () { if ($scope.rightRefresh) { $scope.rightRefresh(); } }
        ];

        $scope.onExpandClicked = function () {
            $scope.expand = true;
        };

        $scope.onCollapseClicked = function () {
            $scope.expand = false;
        };

        $scope.exchangeDictionary = exchangeDictionary;

        $scope.onRefreshClicked = function (index) {
            refreshes[index]();
        };

        $scope.getCustomValues = function (index) {
            // return "testing";
            if (!$scope.model.exchangeCommodities[index].customValues) { return null; }
            
            var keys = Object.keys($scope.model.exchangeCommodities[index].customValues);
            var kvps = [];
            for (var i = 0; i < keys.length; i++) {
                var key = keys[i];
                var value = $scope.model.exchangeCommodities[index].customValues[key];
                kvps.push({key: key, value: value});
            }

            return kvps;
        };

        $scope.getProfitText = function () {
            for (var i = 0; i < 2; i++) {
                var a = i;
                var b = 1 - i;

                var profits = $scope.getProfitPercentage(a, b);
                if (profits && profits > 0) {
                    return exchangeDictionary[$scope.model.exchanges[a]].displayName + ' to ' + exchangeDictionary[$scope.model.exchanges[b]].displayName
                        + ' Profits:' +
                        profits.toFixed(4) + " %";
                }
            }

            return "No profits";
        };

        $scope.hasDirectionalProfits = function (a, b) {
            var profitPercentage = $scope.getProfitPercentage(a, b);
            return profitPercentage !== null && profitPercentage > 0;
        };

        $scope.hasProfits = function () {
            return $scope.hasDirectionalProfits(0, 1) || $scope.hasDirectionalProfits(1, 0);
        };

        $scope.hasSufficientProfits = function () {
            var waiveCanTransferCheck = false; //;true;

            var canTransferLeftToRight = ($scope.model.exchangeCommodities[0].canWithdraw !== false
                && $scope.model.exchangeCommodities[1].canDeposit !== false) || waiveCanTransferCheck;

            var leftToRightProfits = $scope.getProfitPercentage(0, 1);
            if (leftToRightProfits !== undefined
                && leftToRightProfits !== null
                && leftToRightProfits >= 1
                && canTransferLeftToRight) { return true; }

            var canTransferRightToLeft = ($scope.model.exchangeCommodities[1].canWithdraw !== false
                && $scope.model.exchangeCommodities[0].canDeposit !== false) || waiveCanTransferCheck;

            var rightToLeftProfits = $scope.getProfitPercentage(1, 0);
            if (rightToLeftProfits !== undefined && rightToLeftProfits !== null
                && rightToLeftProfits >= 1
                && canTransferRightToLeft) { return true; }

            return false;
        };

        $scope.getProfitPercentage = function (a, b) {
            try {
                if (!$scope.model.books[b]
                    || !$scope.model.books[b]
                    || !$scope.model.books[b].data
                    || !$scope.model.books[b].data.bids
                    || $scope.model.books[b].data.bids.length < 1
                    || !$scope.model.books[a]
                    || !$scope.model.books[a].data
                    || !$scope.model.books[a].data.asks
                    || $scope.model.books[a].data.asks.length < 1
                    || $scope.model.books[a].data.asks[0] === 0
                ) {
                    return null;
                }

                var diff = $scope.model.books[b].data.bids[0].price - $scope.model.books[a].data.asks[0].price;
                var ratio = diff / $scope.model.books[a].data.asks[0].price;
                var percentage = 100.0 * ratio;

                return percentage;
            } catch (ex) {
                log('getProfitPercentage failed.');
                log(ex);

                return null;
            }
        }

        $scope.getStyle = function (index) {
            try {
                if (!$scope.model
                    || !$scope.model.books
                    || $scope.model.books.length <= index
                    || !$scope.model.books[index]
                    || !$scope.model.books[index].data
                    || !$scope.model.books[index].data.asOf
                ) {
                    return null;
                }

                var asOf = $scope.model.books[index].data.asOf;
                var currentTime = new Date();
                var diff = currentTime - new Date(asOf);
                var diffSeconds = diff / 1000;
                var diffMinutes = diffSeconds / 60;
                if (diffMinutes <= 10) {
                    return { "color": "green" };
                }

                if (diffMinutes <= 60) {
                    // return { color: "orange" };
                    return { "background-color": "#ffde9e" };
                    // e2e281
                }

                return { "background-color": "#FF9696" };
            } catch (ex) {
                log('getStyle failed');
                log(ex);
                return null;
            }
        };
});


