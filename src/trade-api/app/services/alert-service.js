angular.module('main')
.service('alertService', function (toastr) {
    var that = this;
    that.error = function (message, title) {
        console.log(message);
        toastr.error(err, title || 'Error');
    };

    that.onError = that.error;

    that.exception = function (exception, title) {
        console.log(exception);
        toastr.error(exception ? JSON.stringify(exception) : exception, title || 'Exception');
    };

    that.info = function (message, title) {
        console.log(message);
        toastr.info(message, title);
    };

    that.success = function (message, title) {
        console.log(message);
        toastr.success(message, title);
    };
});