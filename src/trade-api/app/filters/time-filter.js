(function () {
    var generateText = function (timeStamp, showTimeZone) {
        if (timeStamp === undefined || timeStamp === null) {
            return null;
        }

        var getTimeZoneText = function () {
            var offset = -(new Date().getTimezoneOffset() / 60);
            var offsetAbs = Math.abs(offset);
            var plusMinus = offset < 0 ? '-' : '+';
            return '(UTC' + plusMinus + offsetAbs + ')';
        };

        var tz = getTimeZoneText();
        text = moment.utc(timeStamp).local().format("MMMM DD YYYY hh:mm:ss A");
        if (showTimeZone) {
            return text + " " + tz;
        }

        return text;
    };

    var localTimeFilter = function () {
        return function (timeStamp) {
            return generateText(timeStamp, true);
        };
    };

    var localTimeFilterNoTimeZone = function () {
        return function (timeStamp) {
            return generateText(timeStamp, false);
        };
    };

    angular.module('main').filter('localTime', localTimeFilter);
    angular.module('main').filter('localTimeNoTimeZone', localTimeFilterNoTimeZone);
})();