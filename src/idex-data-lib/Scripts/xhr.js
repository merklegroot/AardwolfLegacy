
(function () {
    var clientMachineName = 'test_run';
	var socket = new WebSocket("wss://idex.market");

	socket.onmessage = function (evt) {
	  var frameContents = JSON.stringify(evt.data);
	  
	  var serviceModel = { 
		frameContents: evt.data, // frameContents,
		clientTimeStampLocal: new Date(),
		clientMachineName: clientMachineName
      };
	  
	  var xhr = new XMLHttpRequest();
	  xhr.open('POST', 'http://localhost/trade-data-relay/api/collector');
	  xhr.setRequestHeader('Content-Type', 'application/json');
	  xhr.send(JSON.stringify(serviceModel));
	};
})();
