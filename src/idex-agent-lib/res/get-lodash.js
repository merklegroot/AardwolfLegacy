(function () {
    var lodashUrl = "https://cdnjs.cloudflare.com/ajax/libs/lodash.js/4.17.10/lodash.min.js";
    var request = new XMLHttpRequest();
    request.open('GET', lodashUrl, false);  // `false` makes the request synchronous
    request.send(null);

    if (request.status !== 200) {
        console.log('failed!');
        return;
    }

    eval(request.responseText);
    console.log('We have lodash.');
})();