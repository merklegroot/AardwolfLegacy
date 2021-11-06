angular.module('main')
    .filter('ago', function () {
        return function (timeStamp) {
            if (timeStamp === undefined || timeStamp === null) {
                return null;
            }

            var currentTime = moment();
            var totalMilliseconds = (currentTime - moment(timeStamp));

            var days = 0;
            var hours = 0;
            var minutes = 0;
            var seconds = 0;
            var milliseconds = totalMilliseconds;

            if (milliseconds >= 1000) {
                seconds = parseInt(milliseconds / 1000);
                milliseconds -= 1000 * seconds;
            }

            var totalSeconds = totalMilliseconds / 1000;

            if (seconds >= 60) {
                minutes = parseInt(seconds / 60);
                seconds -= 60 * minutes;
            }

            var totalMinutes = totalSeconds / 60;

            if (minutes >= 60) {
                hours = parseInt(minutes / 60);
                minutes -= 60 * hours;
            }

            var totalHours = totalMinutes / 60;
            if (hours >= 24) {
                days = parseInt(hours / 24);
                hours -= 24 * days;
            }

            var totalDays = totalHours / 60;
            if (days >= 24) {
                weeks = parseInt(days / 7);
                days -= 7 * days;
            }

            var totalWeeks = totalDays / 7;

            if (totalDays >= 2) { return Math.round(totalDays) + " days ago"; }
            if (totalDays > 1) { return Math.round(totalDays) + " day ago"; }

            if (totalHours >= 2) { return Math.round(totalHours) + " hrs ago"; }
            if (totalHours > 1) { return Math.round(totalHours) + " hr ago"; }

            if (totalMinutes >= 2) { return Math.round(totalMinutes) + " mins ago"; }
            if (totalMinutes > 1) { return Math.round(totalMinutes) + " min ago"; }

            if (totalSeconds > 1) { return Math.round(totalSeconds) + " sec ago"; }

            return "Just now";
        };
});