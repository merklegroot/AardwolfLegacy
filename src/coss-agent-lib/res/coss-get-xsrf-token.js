//var getXsrfToken = function () {
//    var pieces = document.cookie.split(";");
//    if (!pieces) { return null; }
//    for (var i = 0; i < pieces.length; i++) {
//        var piece = pieces[i];
//        if (!piece) { continue; }
//        var segments = piece.split("=");
//        if (!segments || segments.length != 2) { continue; }
//        var key = segments[0].trim().toUpperCase();
//        if (key === "XSRF-TOKEN") { return segments[1].trim(); }
//    }

//    return null;
//};