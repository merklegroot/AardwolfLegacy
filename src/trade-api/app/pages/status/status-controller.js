angular.module('main').controller("statusController", function (
    $scope, $http, $timeout, dataService, alertService) {
    var that = this;
    that.keepRunning = true;

    var _services = null;
    $scope.model = {};

    $scope.angularVersion = 'Version ' + angular.version.full;

    var getDatabaseStatus = function () {
        if (!$scope.model.database) {
            $scope.model.database = { isLoading: true };
        }        

        $http.post('api/get-database-status')
            .then(function (response) {
                $scope.model.database.isLoading = false;
                $scope.model.database.value = response.data;
                if (that.keepRunning) {
                    // $timeout(getDatabaseStatus, 2500);
                }
            }, function (err) {
                $scope.model.database.isLoading = false;
                $scope.model.database.value = 'Failed to load';
                alertService.onError(err);

                if (that.keepRunning) {
                    // $timeout(getDatabaseStatus, 2500);
                }
            });
    };

    var getServiceStatus = function (agent) {
        $http.post('api/get-' + agent.id + '-service-status')
            .then(function (response) {
                agent.isLoading = false;
                agent.value = response.data;
                if (that.keepRunning) {
                    $timeout(function () { getServiceStatus(agent); }, 2500);
                }
            }, function (err) {
                agent.isLoading = false;
                agent.value = 'Failed to load';
                if (that.keepRunning) {
                    $timeout(function () { getServiceStatus(agent); }, 2500);
                }
            });
    };

    var loadServices = function () {
        var dataClosure = { services: {} };
        var retriever = function () { return $http.post("api/get-services"); };

        var onSuccess = function () {
            _services = _.cloneDeep(dataClosure.services.data);
            $scope.model.services = _.cloneDeep(dataClosure.services);

            refreshServiceStatusByIndex(0);
        };

        dataService.loadData(dataClosure.services, retriever, onSuccess);
    };

    $scope.onPingButtonClicked = function (service) {
        alertService.info("Ping...");
        var serviceModel = { service: service.id };
        $http.post("api/ping-service", serviceModel)
            .then(function (response) {
                // alertService.success(response);
                alertService.success("Pong!");
            }, function (err) {
                alertService.onError(err);
            });
    };

    var refreshServiceStatusByIndex = function (i) {
        var match = _.find($scope.model.services.data, function (item) { return item.id === _services[i].id; });
        var closureData = {
            service: _services[i], result: {}, match: match
        };

        var retriever = function () {
            var serviceModel = { service: closureData.service.id };
            alertService.info("Pinging the " + closureData.service.name + " service.")
            return $http.post("api/ping-service", serviceModel);
        };

        var onCompleted = function () {
            if (i + 1 < _services.length) {
                refreshServiceStatusByIndex(i + 1);
            }
        };

        var onFailure = function () {
            alertService.onError("Oh nose!");
            onCompleted();
        };

        var onSuccess = function () {         
            alertService.success("Success!");
            var clonedResults = _.cloneDeep(closureData.result);
            closureData.match.status = clonedResults.data;
            closureData.match.statusKvps = [];
            var keys = Object.keys(clonedResults.data);
            for (var keyIndex = 0; keyIndex < keys.length; keyIndex++) {
                var key = keys[keyIndex];
                var value = clonedResults.data[key];
                closureData.match.statusKvps.push({ key: key, value: value });
            }

            onCompleted();
        };

        dataService.loadData(closureData.result, retriever, onSuccess, onFailure);
    };

    var init = function () {
        loadServices();

        $http.post('api/get-server-name')
            .then(function (response) {
                $scope.model.serverName = response.data;
            }, function (err) { alertService.onError(err); });

        $http.post('api/get-app-server-build-date')
            .then(function (response) {
                $scope.model.appServerBuildDate = response.data;
            }, function (err) { alertService.onError(err); });

        getDatabaseStatus();
    };

    init();

    $scope.getStyle = function (item) {
        try {
            if (item.value.wasSuccessful === true) {
                return { 'background-color': 'lightgreen' };
            }

            if (item.value.wasSuccessful === false) {
                return { 'background-color': 'red', 'color': 'white' };
            }

            return {};
        }
        catch (ex) {
            return {};
        }
    };

    // each time a scope is removed this event receiver will be called
    $scope.$on('$destroy', function dismiss() {
        // ActiveScopesServices.remove(...);
        that.keepRunning = false;
    });

    $scope.onCossTestClicked = function () {
        alertService.info("Requesting coss cookies...");
        $http.post("api/status-coss-cookie-test")
            .then(function (response) {
                $scope.model.cossResponse = response.data;
                var sessionToken = response.data.sessionToken;
                var xsrfToken = response.data.xsrfToken;

                if (!sessionToken) {
                    alertService.onError("Missing session token.");
                }
                else if (!xsrfToken) {
                    alertService.onError("Missing XSRF token.");
                } else {
                    alertService.success("All tokens included!");
                }
            }, function (err) { alertService.onError(err); });
    };
});