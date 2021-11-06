(function () {
    var runSock = function () {
        var clientMachineName = '[CLIENT_MACHINE_NAME]';
        var socket = new WebSocket("wss://idex.market/");

        socket.onmessage = function (evt) {
            var frameContents = JSON.stringify(evt.data);

            var serviceModel = {
                frameContents: evt.data,
                clientTimeStampLocal: new Date(),
                clientMachineName: clientMachineName
            };

            var xhr = new XMLHttpRequest();
            xhr.open('POST', 'http://localhost/trade-data-relay/api/collector');
            xhr.setRequestHeader('Content-Type', 'application/json');
            xhr.send(JSON.stringify(serviceModel));
        };
    };

    document.writeln('Starting sock script.<br />');
    try {
        runSock();
        document.writeln('Sock script is running.<br />');
    }
    catch(ex) {
        document.writeln('An error has occurred.<br />');
        document.writeln(ex);
    }
})();
