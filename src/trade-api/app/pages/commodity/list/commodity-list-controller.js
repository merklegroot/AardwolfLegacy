angular.module('main')
    .controller('commodityListController', function ($scope, $http, dataService, alertService) {

        var itemsPerPage = 10;

        $scope.commodities = {};
        $scope.pageIndex = 0;

        dataService.loadData($scope.commodities, function () { return $http.post('api/get-commodities'); }, function () {
            $scope.pageIndex = 0;
            $scope.totalPages = Math.ceil($scope.commodities.data.length / itemsPerPage);
            refreshPage();
        });

        $scope.onNextPageClicked = function () {
            if ($scope.pageIndex + 1 < $scope.totalPages) {
                $scope.pageIndex++;
                refreshPage();
            }
        };

        $scope.onFirstPageClicked = function () {
            if ($scope.pageindex !== 0) {
                $scope.pageIndex = 0;
                refreshPage();
            }
        };

        $scope.onPreviousPageClicked = function () {
            if ($scope.pageIndex > 0) {
                $scope.pageIndex--;
                refreshPage();
            }
        }

        $scope.onLastPageClicked = function () {
            if ($scope.pageIndex !== $scope.totalPages - 1) {
                $scope.pageIndex = $scope.totalPages - 1
                refreshPage();
            }
        };

        var refreshPage = function () {
            var filteredData = _.filter($scope.commodities.data, function (item) {
                if ($scope.filter === undefined || $scope.filter === null) { return true; }
                var effectiveFilter = $scope.filter.trim();
                if (effectiveFilter.length === 0) { return true; }

                if (item.symbol !== undefined
                    && item.symbol !== null
                    && item.symbol.toUpperCase().indexOf(effectiveFilter.toUpperCase()) !== -1) { return true; }

                if (item.name !== undefined
                    && item.name !== null
                    && item.name.toUpperCase().indexOf(effectiveFilter.toUpperCase()) !== -1) { return true; }

                return false;
            });

            $scope.effectiveData = _.take(_.drop(filteredData, $scope.pageIndex * itemsPerPage), itemsPerPage);
        };

        $scope.onFilterChanged = function () {
            refreshPage();
        };

        $scope.clearFilter = function () {
            $scope.filter = '';
            $scope.onFilterChanged();
        };

    });