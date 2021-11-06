//(function () {
//    var getXsrfToken = function () {
//        var pieces = document.cookie.split(";");
//        if (!pieces) { return null; }
//        for (var i = 0; i < pieces.length; i++) {
//            var piece = pieces[i];
//            if (!piece) { continue; }
//            var segments = piece.split("=");
//            if (!segments || segments.length != 2) { continue; }
//            var key = segments[0].trim().toUpperCase();
//            if (key === "XSRF-TOKEN") { return segments[1].trim(); }
//        }

//        return null;
//    };

//    var cancelOrder = function (orderId, symbol, baseSymbol, debugId)  {
//        var xsrfToken = getXsrf();
//        var url = "https://exchange.coss.io/api/limit-order/buy/" + symbol.toLowerCase() + "-" + baseSymbol.toLowerCase() + "/" + orderId;

//        var ajax = new XMLHttpRequest();
//        ajax.onreadystatechange = function () {
//            if (ajax.readyState === 4 && ajax.status === 200) {
//                var responseDiv = document.createElement('div');
//                responseDiv.id = debugId;
//                responseDiv.innerText = ajax.responseText;
//                document.body.appendChild(responseDiv);
//            }
//        };

//        ajax.open("DELETE", url);
//        ajax.setRequestHeader("x-xsrf-token", xsrfToken);
//        ajax.send();
//    };

//    cancelOrder('{orderId}', '{symbol}', '{baseSymbol}', '{debugId}');
//})();