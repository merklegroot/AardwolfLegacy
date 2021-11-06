(function () {
    var relayEndpoint = "http://localhost/coss-ws-api/api/message-received";
    var cossSocketUrl = "wss://exchange.coss.io/api/ws";

    console.log("Beginning Page-Sock script");
    try {
        var closureData = {};

        closureData.relayMessage = function (message) {
            console.log("Relaying it!");

            var payload = {                
                timeStampUtc: new Date(),
                contract: "coss-ws-relay-v1",
                messageContents: message
            };

            var req = new XMLHttpRequest();
            req.open("POST", relayEndpoint, true);
            req.setRequestHeader("Content-Type", "application/json;charset=UTF-8");
            req.send(JSON.stringify(payload));
        };

        var connection = new WebSocket(cossSocketUrl, ['soap', 'xmpp']);

        // When the connection is open, send some data to the server
        connection.onopen = function () {
            connection.send('Ping'); // Send the message 'Ping' to the server
        };

        // Log errors
        connection.onerror = function (error) {
            console.log('WebSocket Error ' + error);
        };

        // Log messages from the server
        connection.onmessage = function (e) {
            console.log('Server: ' + e.data);
            try {
                closureData.relayMessage(e.data);
            } catch (ex) {
                console.log("Failed to relay message.");
            }
        };

    } catch (ex) {
        console.log("Page-Sock script exception: " + ex);
    }
})();