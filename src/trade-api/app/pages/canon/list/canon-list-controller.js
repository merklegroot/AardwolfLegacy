angular.module('main')
    .controller('canonListController', function ($scope, $http, $uibModal, alertService, dataService) {
        var itemsPerPage = 10;

        $scope.commodities = {};
        $scope.pageIndex = 0;

        dataService.loadData($scope.commodities, function () { return $http.post('api/get-canon'); }, function () {
            $scope.pageIndex = 0;
            $scope.totalPages = Math.ceil($scope.commodities.data.length / itemsPerPage);
            refreshPage();
        });

        $scope.onCommodityClicked = function (commodity) {
            // alertService.info(commodity);

            var data = commodity ? _.clone(commodity) : {};
           
            var dialog = $uibModal.open({
                templateUrl: 'app/pages/canon/dialog/canon-dialog-template.html',
                controller: 'canonDialogController',
                resolve: { data: function () { return data; } },
                size: 'lg',
                windowClass: 'canon-dialog'
            });

            dialog.result.then(function (result) {
                console.log(result);

                if (!(result && result.wasConfirmed)) { console.log('nope'); return; }

                var serviceModel = result.data;

                $http.post('api/save-canon', serviceModel)
                    .then(function (response) {
                        alertService.success('Success!' + '\r\n' + JSON.stringify(response.data));
                        $scope.commodities.data = response.data;
                    }, function (err) {
                        alertService.error('Error!' + '\r\n' + JSON.stringify(err));
                    });
            });
        };

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

        $scope.clearFilter = function() {
            $scope.filter = '';
            $scope.onFilterChanged();
        };

        $scope.onNewClicked = function () {
            alertService.info("new!");
            $scope.onCommodityClicked(null);
        };
});