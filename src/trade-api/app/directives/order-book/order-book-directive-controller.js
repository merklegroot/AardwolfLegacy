angular.module('main')
    .controller('orderBookDirectiveController', function ($scope) {
        var defaultMinRows = 5;
        var effectiveMinRows = $scope.minRows ? $scope.minRows : defaultMinRows;
        $scope.effectiveMinRows = effectiveMinRows;

        $scope.bidTerm = $scope.hideTerms ? '' : 'Bid';
        $scope.askTerm = $scope.hideTerms ? '' : 'Ask';

        $scope.minBidRows = effectiveMinRows;
        $scope.minAskRows = effectiveMinRows;
        $scope.areBidsExpanded = false;
        $scope.areAsksExpanded = false;

        $scope.getSortedAsks = function () {
            return _.reverse(                
                _.take(_.orderBy($scope.model.data.asks, ['price'], ['asc']), $scope.minAskRows));
        };

        $scope.getSortedBids = function () {
            return _.orderBy($scope.model.data.bids, ['price'], ['desc']);
        };

        $scope.getUsdValue = function (symbol) {
            if (!$scope.valuations) { return null; }

            var match = _.find($scope.valuations, function (item) { return item.symbol.toUpperCase() === symbol.toUpperCase(); })
            if (!match) { return null; }
            return match.data;
        };

        $scope.onExpandAsksClicked = function () {
            if ($scope.areAsksExpanded) {
                $scope.minAskRows = effectiveMinRows;
                $scope.areAsksExpanded = false;
            } else {
                $scope.minAskRows = 15;
                $scope.areAsksExpanded = true;
            }
        };

        $scope.onExpandBidsClicked = function () {
            if ($scope.areBidsExpanded) {
                $scope.minBidRows = effectiveMinRows;
                $scope.areBidsExpanded = false;
            } else {
                $scope.minBidRows = 15;
                $scope.areBidsExpanded = true;
            }
        };
});


