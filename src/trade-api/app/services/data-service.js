angular.module('main')
    .service('dataService', function ($http, alertService) {
        this.loadData = function (model, promiseMethod, afterSuccess, afterFailure) {            
            var closureData = {
                model: model,
                promiseMethod: promiseMethod,
                afterSuccess: afterSuccess,
                afterFailure: afterFailure
            };

            closureData.model.isLoading = true;
            try {
                closureData.promiseMethod().then(
                    function (response) {
                        closureData.model.isLoading = false;
                        closureData.model.data = response.data;

                        if (closureData.afterSuccess !== undefined && closureData.afterSuccess !== null) {
                            closureData.afterSuccess(response);
                        }
                    },
                    function (err) {
                        closureData.model.isLoading = false;
                        alertService.error(err);

                        if (closureData.afterFailure !== undefined && closureData.afterFailure !== null) {
                            closureData.afterFailure(err);
                        }
                    }
                );
            } catch (ex) {
                closureData.model.isLoading = false;
                alertService.error(ex);
            }
        };
});
